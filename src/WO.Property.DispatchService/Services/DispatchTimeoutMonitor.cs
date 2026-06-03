using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using WO.Property.DispatchService.Data;
using WO.Property.DispatchService.Tenant;
using WO.Property.DispatchService.Models;

namespace WO.Property.DispatchService.Services;

/// <summary>
/// 超时监控后台服务
/// 从 timeout_rules 表读取超时配置，根据角色和工单类型动态计算超时时间
/// </summary>
public class DispatchTimeoutMonitor : BackgroundService
{
    private readonly ILogger<DispatchTimeoutMonitor> _logger;
    private readonly IServiceProvider _serviceProvider;

    // 角色升级顺序
    private static readonly string[] EscalationRoles = {
        "operator", "supervisor", "manager", "department_head", "company_head"
    };

    public DispatchTimeoutMonitor(
        ILogger<DispatchTimeoutMonitor> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DispatchTimeoutMonitor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckPendingTimeouts();
                await CheckReceivedTimeouts();
                await CheckCompletedTimeouts();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DispatchTimeoutMonitor");
            }

            // 每分钟检查一次
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    /// <summary>
    /// 获取角色的超时小时数（从 timeout_rules 表读取，支持按 color 筛选）
    /// </summary>
    private async Task<int> GetTimeoutHoursAsync(TenantDbContext db, string role, string color = "green")
    {
        // 优先按 color + role 查找
        var rule = await db.TimeoutRules
            .Where(r => r.Role == role && r.Color == color && r.Enabled)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (rule != null)
        {
            return rule.Hours;
        }

        // 回退：按 role 查找（不限 color）
        rule = await db.TimeoutRules
            .Where(r => r.Role == role && r.Enabled)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (rule != null)
        {
            return rule.Hours;
        }

        // 默认值
        return 24;
    }

    /// <summary>
    /// 获取下一个升级角色
    /// </summary>
    private string? GetNextEscalationRole(string currentRole)
    {
        var index = Array.IndexOf(EscalationRoles, currentRole);
        if (index >= 0 && index < EscalationRoles.Length - 1)
        {
            return EscalationRoles[index + 1];
        }
        return null;
    }

    /// <summary>
    /// 查找指定角色的上级人员
    /// </summary>
    private async Task<PersonInfo?> FindPersonByRoleAsync(TenantDbContext db, string role, int excludePersonId, string tenantCode, int projectId)
    {
        // 通过 HTTP 调用 PersonService 获取该角色的人员
        // 这里简化处理，实际应该通过服务间通信获取
        try
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5018");
            client.Timeout = TimeSpan.FromSeconds(5);

            var personsResponse = await client.GetAsync($"/api/persons?role={role}");
            if (!personsResponse.IsSuccessStatusCode) return null;

            var personsJson = await personsResponse.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(personsJson);
            var dataRoot = doc.RootElement.GetProperty("data");
            var dataElement = dataRoot.TryGetProperty("items", out var ipe) ? ipe : dataRoot;

            foreach (var item in dataElement.EnumerateArray())
            {
                var id = item.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
                var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var status = item.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";

                if (id > 0 && id != excludePersonId && status == "active")
                {
                    return new PersonInfo { Id = id, Name = name, Role = role };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch person by role {Role}", role);
        }

        return null;
    }

    /// <summary>
    /// 检查 Pending 状态超时（派单后未接单）
    /// </summary>
    private async Task CheckPendingTimeouts()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TenantDbContext>>();
        var tenantDbFactory = scope.ServiceProvider.GetRequiredService<ITenantDbFactory>();

        tenantDbFactory.SetCurrentTenantCode("wo_property");

        using var db = await dbFactory.CreateDbContextAsync();

        // 查询所有 Pending 状态的派单记录
        var pendingRecords = await db.DispatchRecords
            .Where(d => d.Status == "Pending")
            .ToListAsync();

        foreach (var record in pendingRecords)
        {
            // 获取接单人的超时配置
            var toPersonRole = await GetPersonRoleAsync(db, record.ToPersonId);
            var timeoutHours = await GetTimeoutHoursAsync(db, toPersonRole);
            var timeoutTime = record.DispatchTime.AddHours(timeoutHours);

            if (DateTime.Now > timeoutTime)
            {
                _logger.LogWarning("派单超时: DispatchId={Id}, TicketCode={TicketCode}, 已超时 {Minutes} 分钟",
                    record.Id, record.TicketCode, (int)(DateTime.Now - record.DispatchTime).TotalMinutes);

                // 按 TicketId 查找已有告警（一张工单只对应一条 Pending 告警）
                var existingAlert = await db.TimeoutAlerts
                    .Where(a => a.TicketId == record.TicketId && a.AlertType == "PendingTimeout" && a.Status == "Pending")
                    .FirstOrDefaultAsync();

                if (existingAlert == null)
                {
                    // 首次超时 → 创建新告警，同时触发升级（只升级一次）
                    var alert = new TimeoutAlert
                    {
                        TicketId = record.TicketId,
                        TicketCode = record.TicketCode,
                        DispatchRecordId = record.Id,
                        AlertType = "PendingTimeout",
                        ExpectedTime = DateTime.Now.AddMinutes(30), // 30分钟后再次提醒
                        ActualTime = DateTime.Now,
                        TimeoutMinutes = (int)(DateTime.Now - record.DispatchTime).TotalMinutes,
                        Level = 1,
                        NotifyTargetId = record.ToPersonId,
                        NotifyTargetName = record.ToPersonName,
                        Status = "Pending",
                        TenantCode = record.TenantCode,
                        ProjectId = record.ProjectId,
                        CreatedAt = DateTime.Now
                    };
                    db.TimeoutAlerts.Add(alert);

                    // 首次超时才触发升级，且只升级一次
                    var existingEscalation = await db.TimeoutEscalations
                        .Where(e => e.TicketId == record.TicketId && e.Status == "Pending")
                        .FirstOrDefaultAsync();
                    if (existingEscalation == null)
                    {
                        await EscalateToHigherRole(db, record, toPersonRole);
                    }
                }
                else
                {
                    // 已有告警 → 只更新 ExpectedTime（推后30分钟再次提醒），不重复升级
                    existingAlert.ExpectedTime = DateTime.Now.AddMinutes(30);
                    existingAlert.ActualTime = DateTime.Now;
                    existingAlert.TimeoutMinutes = (int)(DateTime.Now - record.DispatchTime).TotalMinutes;
                }

                await db.SaveChangesAsync();
            }
        }
    }

    /// <summary>
    /// 检查 Received 状态超时（已接单但未开始处理）
    /// </summary>
    private async Task CheckReceivedTimeouts()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TenantDbContext>>();
        var tenantDbFactory = scope.ServiceProvider.GetRequiredService<ITenantDbFactory>();

        tenantDbFactory.SetCurrentTenantCode("wo_property");

        using var db = await dbFactory.CreateDbContextAsync();

        var receivedRecords = await db.DispatchRecords
            .Where(d => d.Status == "Received")
            .ToListAsync();

        foreach (var record in receivedRecords)
        {
            var toPersonRole = await GetPersonRoleAsync(db, record.ToPersonId);
            var timeoutHours = await GetTimeoutHoursAsync(db, toPersonRole);
            var timeoutTime = record.DispatchTime.AddHours(timeoutHours);

            if (DateTime.Now > timeoutTime)
            {
                _logger.LogWarning("接单超时: DispatchId={Id}, TicketCode={TicketCode}, 已超时 {Minutes} 分钟",
                    record.Id, record.TicketCode, (int)(DateTime.Now - record.DispatchTime).TotalMinutes);

                var existingAlert = await db.TimeoutAlerts
                    .Where(a => a.TicketId == record.TicketId && a.AlertType == "ReceivedTimeout" && a.Status == "Pending")
                    .FirstOrDefaultAsync();

                if (existingAlert == null)
                {
                    var alert = new TimeoutAlert
                    {
                        TicketId = record.TicketId,
                        TicketCode = record.TicketCode,
                        DispatchRecordId = record.Id,
                        AlertType = "ReceivedTimeout",
                        ExpectedTime = DateTime.Now.AddMinutes(30),
                        ActualTime = DateTime.Now,
                        TimeoutMinutes = (int)(DateTime.Now - record.DispatchTime).TotalMinutes,
                        Level = 2,
                        NotifyTargetId = record.ProjectId,
                        NotifyTargetName = "管理员",
                        Status = "Pending",
                        TenantCode = record.TenantCode,
                        ProjectId = record.ProjectId,
                        CreatedAt = DateTime.Now
                    };
                    db.TimeoutAlerts.Add(alert);
                }
                else
                {
                    existingAlert.ExpectedTime = DateTime.Now.AddMinutes(30);
                    existingAlert.ActualTime = DateTime.Now;
                    existingAlert.TimeoutMinutes = (int)(DateTime.Now - record.DispatchTime).TotalMinutes;
                }
            }
        }
    }

    /// <summary>
    /// 检查 Completed 状态超时（已完成但未确认）
    /// </summary>
    private async Task CheckCompletedTimeouts()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TenantDbContext>>();
        var tenantDbFactory = scope.ServiceProvider.GetRequiredService<ITenantDbFactory>();

        tenantDbFactory.SetCurrentTenantCode("wo_property");

        using var db = await dbFactory.CreateDbContextAsync();

        var completedRecords = await db.DispatchRecords
            .Where(d => d.Status == "Completed" && d.CompletedAt.HasValue)
            .ToListAsync();

        foreach (var record in completedRecords)
        {
            // 默认72小时未确认则自动确认
            var timeoutTime = record.CompletedAt.Value.AddHours(72);

            if (DateTime.Now > timeoutTime)
            {
                _logger.LogWarning("完工未确认，自动确认: DispatchId={Id}, TicketCode={TicketCode}",
                    record.Id, record.TicketCode);

                // 自动确认，5星默认评价
                var rating = new SatisfactionRating
                {
                    TicketId = record.TicketId,
                    TicketCode = record.TicketCode,
                    DispatchRecordId = record.Id,
                    RaterId = 0,
                    RaterName = "系统自动评价",
                    RateeId = record.ToPersonId,
                    RateeName = record.ToPersonName,
                    QualityScore = 5,
                    AttitudeScore = 5,
                    TimelinessScore = 5,
                    OverallScore = 5,
                    Comment = "超时未确认，系统自动五星评价",
                    RatedAt = DateTime.Now,
                    IsAutoRated = true,
                    TenantCode = record.TenantCode,
                    ProjectId = record.ProjectId,
                    CreatedAt = DateTime.Now
                };
                db.SatisfactionRatings.Add(rating);

                record.Status = "Confirmed";
                record.ConfirmedAt = DateTime.Now;
                record.ConfirmedBy = 0;
                record.ConfirmedByName = "系统自动确认";
                record.RatingId = rating.Id;

                var alert = new TimeoutAlert
                {
                    TicketId = record.TicketId,
                    TicketCode = record.TicketCode,
                    DispatchRecordId = record.Id,
                    AlertType = "CompletedTimeout",
                    ExpectedTime = timeoutTime,
                    ActualTime = DateTime.Now,
                    TimeoutMinutes = (int)(DateTime.Now - record.CompletedAt.Value).TotalMinutes,
                    Level = 1,
                    NotifyTargetId = record.ToPersonId,
                    NotifyTargetName = record.ToPersonName,
                    Status = "Processed",
                    TenantCode = record.TenantCode,
                    ProjectId = record.ProjectId,
                    CreatedAt = DateTime.Now
                };
                db.TimeoutAlerts.Add(alert);

                await db.SaveChangesAsync();

                // 自动将工单状态改为 Closed（Owner超时未评价）
                try {
                    var ticketHttpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5102") };
                    ticketHttpClient.DefaultRequestHeaders.Remove("X-Project");
                    ticketHttpClient.DefaultRequestHeaders.Add("X-Project", record.TenantCode ?? "wo_property");
                    var payload = new { status = "Closed", rating = 5.0 };
                    await ticketHttpClient.PutAsJsonAsync($"/api/tenant/tickets/{record.TicketId}", payload);
                    _logger.LogInformation("自动关闭超时未评价工单: TicketId={TicketId}", record.TicketId);
                } catch (Exception ex) {
                    _logger.LogWarning(ex, "自动关闭工单失败: TicketId={TicketId}", record.TicketId);
                }

                _logger.LogInformation("自动确认并评价: DispatchId={Id}", record.Id);
            }
        }
    }

    /// <summary>
    /// 升级给更高级别角色的人员
    /// </summary>
    private async Task EscalateToHigherRole(TenantDbContext db, DispatchRecord record, string currentRole)
    {
        var nextRole = GetNextEscalationRole(currentRole);
        if (nextRole == null)
        {
            _logger.LogWarning("已到达最高升级级别，无法继续升级: DispatchId={Id}", record.Id);
            return;
        }

        var higherPerson = await FindPersonByRoleAsync(db, nextRole, record.ToPersonId, record.TenantCode, record.ProjectId);
        if (higherPerson == null)
        {
            _logger.LogWarning("找不到升级目标角色 {Role} 的人员: DispatchId={Id}", nextRole, record.Id);
            return;
        }

        // 1. 更新原派单记录状态为 Escalated
        record.Status = "Escalated";
        record.UpdatedAt = DateTime.Now;

        // 2. 创建新的派单记录（升级派单）
        var escalationLevel = Array.IndexOf(EscalationRoles, nextRole) + 1;
        var newDispatch = new DispatchRecord
        {
            TicketId = record.TicketId,
            TicketCode = record.TicketCode,
            DispatchTime = DateTime.Now,
            FromPersonId = record.ToPersonId,
            FromPersonName = record.ToPersonName,
            ToPersonId = higherPerson.Id,
            ToPersonName = higherPerson.Name,
            Status = "Pending",
            Source = "Escalation",
            ParentDispatchId = record.Id,
            EscalationLevel = $"L{escalationLevel}",
            TenantCode = record.TenantCode,
            ProjectId = record.ProjectId,
            CreatedAt = DateTime.Now
        };
        db.DispatchRecords.Add(newDispatch);

        // 3. 记录升级信息
        var escalation = new TimeoutEscalation
        {
            TicketId = record.TicketId,
            TicketCode = record.TicketCode,
            DispatchRecordId = record.Id,
            FromPersonId = record.ToPersonId,
            FromPersonName = record.ToPersonName,
            ToPersonId = higherPerson.Id,
            ToPersonName = higherPerson.Name,
            ToRole = nextRole,
            Level = escalationLevel,
            EscalatedAt = DateTime.Now,
            Status = "Pending",
            TenantCode = record.TenantCode,
            ProjectId = record.ProjectId,
            CreatedAt = DateTime.Now
        };
        db.TimeoutEscalations.Add(escalation);

        // 4. 记录超时告警
        var alert = new TimeoutAlert
        {
            TicketId = record.TicketId,
            TicketCode = record.TicketCode,
            DispatchRecordId = record.Id,
            AlertType = "EscalationTimeout",
            ExpectedTime = DateTime.Now,
            ActualTime = DateTime.Now,
            TimeoutMinutes = 0,
            Level = escalationLevel,
            NotifyTargetId = higherPerson.Id,
            NotifyTargetName = higherPerson.Name,
            Status = "Pending",
            TenantCode = record.TenantCode,
            ProjectId = record.ProjectId,
            CreatedAt = DateTime.Now
        };
        db.TimeoutAlerts.Add(alert);

        // 5. 发送通知（这里调用通知服务，后续完善）
        await SendEscalationNotificationAsync(higherPerson.Id, record.TicketCode, escalationLevel, nextRole);

        _logger.LogInformation("升级派单: 原DispatchId={Id}, 新派单人={Name}({Role}), Level={Level}",
            record.Id, higherPerson.Name, nextRole, escalationLevel);
    }

    /// <summary>
    /// 发送升级通知
    /// </summary>
    private async Task SendEscalationNotificationAsync(int personId, string ticketCode, int level, string role)
    {
        try
        {
            // 调用 NotificationService 发送通知
            using var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5505");  // NotificationService
            client.Timeout = TimeSpan.FromSeconds(5);

            var payload = new
            {
                title = $"工单升级通知 (L{level})",
                content = $"工单 {ticketCode} 已升级到您，级别: {role}，请及时处理。",
                type = "Escalation",
                targetId = personId,
                projectCode = "YGHY001"
            };


            var response = await client.PostAsJsonAsync("/api/tenant/notification", payload);
            _logger.LogInformation("升级通知已发送: PersonId={PersonId}, TicketCode={TicketCode}, Level={Level}",
                personId, ticketCode, level);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "发送升级通知失败: PersonId={PersonId}", personId);
        }
    }

    /// <summary>
    /// 获取人员的角色（简化版，实际应该从数据库或缓存获取）
    /// </summary>
    private async Task<string> GetPersonRoleAsync(TenantDbContext db, int personId)
    {
        try
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5018");
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync($"/api/persons/{personId}");
            if (!response.IsSuccessStatusCode) return "operator";

            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var role = doc.RootElement.TryGetProperty("data", out var d) 
                && d.TryGetProperty("role", out var r) 
                ? r.GetString() ?? "operator" 
                : "operator";

            return role;
        }
        catch
        {
            return "operator";
        }
    }
}

/// <summary>
/// 人员信息
/// </summary>
public class PersonInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
}