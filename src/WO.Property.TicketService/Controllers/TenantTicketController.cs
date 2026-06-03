using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.TicketService.Data;
using WO.Property.TicketService.Tenant;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;

namespace WO.Property.TicketService.Controllers;

/// <summary>
/// Phase 0 多租户工单控制器
/// 使用 TenantDbContextFactory 动态切换租户库
/// </summary>
[ApiController]
[Route("api/tenant/tickets")]
public class TenantTicketController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TenantTicketController> _logger;

    public TenantTicketController(
        IDbContextFactory<TenantDbContext> dbFactory,
        ITenantDbFactory tenantDbFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<TenantTicketController> logger)
    {
        _dbFactory = dbFactory;
        _tenantDbFactory = tenantDbFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    /// <summary>
    /// 批量获取工种名称（直接从 MySQL JobTypes 表查询，避免 HTTP 认证问题）
    /// </summary>
    private async Task<Dictionary<int, string>> GetJobTypeNames(List<int> jobTypeIds, TenantDbContext db)
    {
        if (jobTypeIds == null || !jobTypeIds.Any())
            return new Dictionary<int, string>();

        var result = new Dictionary<int, string>();
        try
        {
            var ids = jobTypeIds.Distinct().ToList();
            var idsStr = string.Join(",", ids);
            var conn = db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT Id, Name FROM JobTypes WHERE Id IN ({idsStr})";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result[reader.GetInt32(0)] = reader.GetString(1);
            }
            await reader.CloseAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetJobTypeNames ERROR] {ex.Message}");
        }
        return result;
    }

    /// <summary>
    /// 批量获取工单类型名称
    /// </summary>
    private async Task<Dictionary<int, string>> GetTicketTypeNames(List<int> ticketTypeIds, TenantDbContext db)
    {
        if (ticketTypeIds == null || !ticketTypeIds.Any())
            return new Dictionary<int, string>();
        var result = new Dictionary<int, string>();
        try
        {
            var ids = ticketTypeIds.Distinct().ToList();
            var idsStr = string.Join(",", ids);
            var conn = db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT id, name FROM ticket_types WHERE id IN ({idsStr})";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result[reader.GetInt32(0)] = reader.GetString(1);
            await reader.CloseAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetTicketTypeNames ERROR] {ex.Message}");
        }
        return result;
    }

    /// <summary>
    /// 构建工单标题：ticketTypeName + jobTypeName（顺序不能换）
    /// </summary>
    private string BuildTitle(string? ticketTypeName, string? jobTypeName)
    {
        var parts = new[] { ticketTypeName, jobTypeName }.Where(p => !string.IsNullOrEmpty(p)).ToList();
        return parts.Count > 0 ? string.Join("", parts) : "工单";
    }

    /// <summary>
    /// 批量获取人员姓名
    /// </summary>
    private async Task<Dictionary<int, string>> FetchPersonNames(List<int> personIds)
    {
        var result = new Dictionary<int, string>();
        if (personIds == null || personIds.Count == 0)
            return result;
        try
        {
            var client = _httpClientFactory.CreateClient("PersonService");
            foreach (var id in personIds.Distinct())
            {
                try
                {
                    var response = await client.GetAsync($"/api/persons/{id}");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("data", out var data) &&
                            data.TryGetProperty("name", out var nameProp))
                            result[id] = nameProp.GetString() ?? id.ToString();
                        else if (doc.RootElement.TryGetProperty("name", out var nameProp2))
                            result[id] = nameProp2.GetString() ?? id.ToString();
                    }
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FetchPersonNames] Error: {ex.Message}");
        }
        return result;
    }

    /// <summary>
    /// 解析 JobTypeId 字段
    /// </summary>
    private int? ParseFirstJobTypeId(int? jobTypeId) => jobTypeId;

    private string? GetTenantCode()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return null;
        var token = authHeader.Substring("Bearer ".Length).Trim();
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == "tenant_code")?.Value;
        }
        catch { return null; }
    }

    private int? GetUserIdFromJwt()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return null;
        var token = authHeader.Substring("Bearer ".Length).Trim();
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var uidStr = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
              ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "personId")?.Value;
            return int.TryParse(uidStr, out var uid) ? uid : null;
        }
        catch { return null; }
    }

    // GET /api/tenant/tickets
    [HttpGet]
    public async Task<IActionResult> GetTickets([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, [FromQuery] string? dispatchStatus = null, [FromQuery] int? projectId = null, [FromQuery] int? creatorPersonId = null, [FromQuery] int? assigneePersonId = null)
    {
        using var db = CreateDbContext();
        var query = db.Tickets.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        var dispatchStatusValues = Request.Query["dispatchStatus"].ToArray();
        if (dispatchStatusValues.Length > 0)
            query = query.Where(t => dispatchStatusValues.Contains(t.DispatchStatus));

        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);

        // 从 X-Project header 读取项目代码过滤（方案A）
        var projectCodeHeader = Request.Headers["X-Project"].FirstOrDefault();
        if (!string.IsNullOrEmpty(projectCodeHeader))
            query = query.Where(t => t.ProjectCode == projectCodeHeader);

        if (creatorPersonId.HasValue)
            query = query.Where(t => t.CreatorPersonId == creatorPersonId.Value);

        if (assigneePersonId.HasValue)
            query = query.Where(t => t.AssigneePersonId == assigneePersonId.Value);

        var total = await query.CountAsync();
        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Debug: 直接用 ADO.NET 查 jobTypeIds 原始值
        // 工种 enrichment：使用原始 SQL 读取 jobTypeIds（避免 EF Core json 列读取问题）
        var ticketIds = tickets.Select(t => t.Id).ToList();
        var jobTypeIdMap = new Dictionary<int, int?>();
        if (ticketIds.Count > 0)
        {
            var idsStr = string.Join(",", ticketIds);
            var conn = db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT id, jobTypeId FROM tickets WHERE id IN ({idsStr}) AND jobTypeId IS NOT NULL";
using var reader = await cmd.ExecuteReaderAsync();
            var debugRows = new List<string>();
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(0);
                int? jt = reader.IsDBNull(1) ? null : reader.GetInt32(1);
                jobTypeIdMap[id] = jt;
            }
            await reader.CloseAsync();
}

        var firstJobTypeIds = jobTypeIdMap.Values.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        var jobTypeNames = await GetJobTypeNames(firstJobTypeIds, db);

        // 批量获取人员姓名（Assignee + Creator）
        var personIds = tickets
            .SelectMany(t => new[] { t.CreatorPersonId, t.AssigneePersonId })
            .Where(id => id.HasValue).Distinct().Select(id => id!.Value).ToList();
        var personNames = await FetchPersonNames(personIds);

        var result = tickets.Select(t => {
            var firstId = jobTypeIdMap.TryGetValue(t.Id, out var fid) ? fid : null;
            return new {
                t.Id,
                t.TicketCode,
                t.Title,
                t.Description,
                t.Category,
                t.Priority,
                t.Status,
                t.DispatchStatus,
                t.CreatorPersonId,
                CreatorName = t.CreatorPersonId.HasValue
                    ? personNames.GetValueOrDefault(t.CreatorPersonId!.Value)
                    : null,
                t.ProjectId,
                t.ProjectCode,
                t.TicketTypeId,
                t.AreaId,
                t.BuildingId,
                t.RoomId,
                t.ContactPersonName,
                t.ContactPhone,
                t.Location,
                t.AssignedAt,
                t.StartedAt,
                t.FinishedAt,
                t.CreatedAt,
                t.UpdatedAt,
                t.AssigneePersonId,
                AssigneeName = t.AssigneePersonId.HasValue
                    ? personNames.GetValueOrDefault(t.AssigneePersonId!.Value)
                    : null,
                JobTypeId = t.JobTypeId,
                jobTypeName = firstId.HasValue && jobTypeNames.ContainsKey(firstId.Value)
                    ? jobTypeNames[firstId.Value]
                    : null
            };
        }).ToList();

        return Ok(new
        {
            success = true,
            data = result,
            total,
            page,
            pageSize
        });
    }

    // GET /api/tenant/tickets/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicket(int id)
    {
        using var db = CreateDbContext();

        // 使用原始 SQL 直接读取 dispatch_status 列，避免 EF Core 映射问题
        string? dispatchStatus = null;
        string? jobTypeName = null;
        string? ticketCode = null;
        string? title = null;
        string? description = null;
        string? category = null;
        string? priority = null;
        string? status = null;
        int? creatorPersonId = null;
        int? projectId = null;
        string? projectCode = null;
        int? ticketTypeId = null;
        long? areaId = null;
        int? buildingId = null;
        int? roomId = null;
        string? contactPersonName = null;
        string? contactPhone = null;
        string? location = null;
        DateTime? assignedAt = null;
        DateTime? startedAt = null;
        DateTime? finishedAt = null;
        DateTime? createdAt = null;
        DateTime? updatedAt = null;
        int? assigneePersonId = null;
        int? jobTypeId = null;

        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"SELECT id, TicketNumber, Title, Description, category, Priority, Status,
                dispatch_status, creator_id, project_id, project_code, ticket_type_id,
                area_id, BuildingId, RoomId, ContactPersonName, ContactPhone, Location,
                assigned_at, started_at, finished_at, created_at, UpdatedAt, assignee_id, jobTypeId
                FROM tickets WHERE id = @id";
            var p = cmd.CreateParameter();
            p.ParameterName = "@id";
            p.Value = id;
            cmd.Parameters.Add(p);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                id = reader.GetInt32(0);
                ticketCode = reader.IsDBNull(1) ? null : reader.GetString(1);
                title = reader.IsDBNull(2) ? null : reader.GetString(2);
                description = reader.IsDBNull(3) ? null : reader.GetString(3);
                category = reader.IsDBNull(4) ? null : reader.GetString(4);
                priority = reader.IsDBNull(5) ? null : reader.GetString(5);
                status = reader.IsDBNull(6) ? null : reader.GetString(6);
                dispatchStatus = reader.IsDBNull(7) ? null : reader.GetString(7);
                creatorPersonId = reader.IsDBNull(8) ? null : reader.GetInt32(8);
                projectId = reader.IsDBNull(9) ? null : reader.GetInt32(9);
                projectCode = reader.IsDBNull(10) ? null : reader.GetString(10);
                ticketTypeId = reader.IsDBNull(11) ? null : reader.GetInt32(11);
                areaId = reader.IsDBNull(12) ? null : reader.GetInt64(12);
                buildingId = reader.IsDBNull(13) ? null : reader.GetInt32(13);
                roomId = reader.IsDBNull(14) ? null : reader.GetInt32(14);
                contactPersonName = reader.IsDBNull(15) ? null : reader.GetString(15);
                contactPhone = reader.IsDBNull(16) ? null : reader.GetString(16);
                location = reader.IsDBNull(17) ? null : reader.GetString(17);
                assignedAt = reader.IsDBNull(18) ? null : reader.GetDateTime(18);
                startedAt = reader.IsDBNull(19) ? null : reader.GetDateTime(19);
                finishedAt = reader.IsDBNull(20) ? null : reader.GetDateTime(20);
                createdAt = reader.IsDBNull(21) ? null : reader.GetDateTime(21);
                updatedAt = reader.IsDBNull(22) ? null : reader.GetDateTime(22);
                assigneePersonId = reader.IsDBNull(23) ? null : reader.GetInt32(23);
                jobTypeId = reader.IsDBNull(24) ? null : reader.GetInt32(24);
            }
        }

        // 获取人员姓名
        var personIds = new List<int>();
        if (creatorPersonId.HasValue) personIds.Add(creatorPersonId.Value);
        if (assigneePersonId.HasValue) personIds.Add(assigneePersonId.Value);
        var personNames = await FetchPersonNames(personIds);

        // 获取工种名称
        if (jobTypeId.HasValue)
        {
            using var jtCmd = conn.CreateCommand();
            jtCmd.CommandText = "SELECT Name FROM JobTypes WHERE Id = " + jobTypeId.Value;
            var jtResult = await jtCmd.ExecuteScalarAsync();
            jobTypeName = jtResult as string;
        }

        Console.WriteLine($"[DEBUG GetTicket] dispatchStatus={dispatchStatus}, projectCode={projectCode}, id={id}");

        return Ok(new {
            success = true,
            data = new {
                id,
                ticketCode,
                title,
                description,
                category,
                priority,
                status,
                dispatchStatus,
                creatorPersonId,
                CreatorName = creatorPersonId.HasValue
                    ? personNames.GetValueOrDefault(creatorPersonId.Value)
                    : null,
                projectId,
                projectCode,
                ticketTypeId,
                areaId,
                buildingId,
                roomId,
                contactPersonName,
                contactPhone,
                location,
                assignedAt,
                startedAt,
                finishedAt,
                createdAt,
                updatedAt,
                assigneePersonId,
                AssigneeName = assigneePersonId.HasValue
                    ? personNames.GetValueOrDefault(assigneePersonId.Value)
                    : null,
                jobTypeId,
                jobTypeName
            }
        });
    }

    // POST /api/tenant/tickets
    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] TenantCreateTicketRequest request)
    {
        using var db = CreateDbContext();

        // 工单编号：{ProjectCode}-WO-YYYYMM-NNNNN（如 YGHY001-WO-202605-10001）
        var projectCode = Request.Headers["X-Project"].FirstOrDefault() ?? "UNKNOWN";
        var yearMonth = DateTime.Now.ToString("yyyyMM");
        var prefix = $"{projectCode}-WO-{yearMonth}-";
        var lastTicket = db.Tickets
            .Where(t => t.TicketCode.StartsWith(prefix))
            .OrderByDescending(t => t.TicketCode)
            .FirstOrDefault();
        int seq = lastTicket == null
            ? 10001
            : int.Parse(lastTicket.TicketCode.Substring(prefix.Length)) + 1;
        var ticketNo = $"{prefix}{seq}";

        var creatorId = request.CreatorPersonId ?? GetUserIdFromJwt() ?? 0;

        var ticket = new Ticket
        {
            TicketCode = ticketNo,
            Title = "",   // 先留空，创建后自动生成
            Description = request.Description,
            Category = request.Category ?? "",
            Priority = request.Priority ?? "Medium",
            Status = "New",
            CreatorPersonId = creatorId,
            AssigneePersonId = null,
            ProjectId = request.ProjectId,
            ProjectCode = projectCode,
            CreatedAt = DateTime.UtcNow,
            TicketTypeId = request.TicketTypeId,
            AreaId = request.AreaId,
            BuildingId = request.BuildingId,
            RoomId = request.RoomId,
            ContactPersonName = request.ContactPersonName,
            ContactPhone = request.ContactPhone,
            Location = request.Location,
            JobTypeId = request.JobTypeId,
        };

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // 自动生成标题：ticketTypeName + jobTypeName
        var ticketTypeNames = await GetTicketTypeNames(request.TicketTypeId.HasValue ? new List<int> { request.TicketTypeId.Value } : new List<int>(), db);
        var jobTypeNames = await GetJobTypeNames(request.JobTypeId.HasValue ? new List<int> { request.JobTypeId.Value } : new List<int>(), db);
        ticket.Title = BuildTitle(
            request.TicketTypeId.HasValue ? ticketTypeNames.GetValueOrDefault(request.TicketTypeId.Value) : null,
            request.JobTypeId.HasValue ? jobTypeNames.GetValueOrDefault(request.JobTypeId.Value) : null
        );
        await db.SaveChangesAsync();

        // 自动派单：调用 DispatchService
        try
        {
            var client = _httpClientFactory.CreateClient("DispatchService");
            var token = Request.Headers["Authorization"].FirstOrDefault();
            var dispatchPayload = new
            {
                TicketId = ticket.Id,
                TicketCode = ticket.TicketCode,
                JobTypeId = ticket.JobTypeId ?? 0,
                TicketTypeId = ticket.TicketTypeId ?? 0,
                AreaId = ticket.AreaId ?? 0,
                BuildingId = ticket.BuildingId ?? 0,
                ProjectId = ticket.ProjectId
            };
            var dispatchRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5241/api/tenant/dispatch/auto")
            {
                Content = JsonContent.Create(dispatchPayload)
            };
            if (!string.IsNullOrEmpty(token))
                dispatchRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));
            var dispatchResponse = await client.SendAsync(dispatchRequest);
            if (dispatchResponse.IsSuccessStatusCode)
            {
                ticket.DispatchStatus = "Dispatched";
                // 解析派单结果，获取被派人员ID并更新工单
                var dispatchResult = await dispatchResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                if (dispatchResult.TryGetProperty("data", out var dataEl) &&
                    dataEl.TryGetProperty("personId", out var personIdEl))
                {
                    ticket.AssigneePersonId = personIdEl.GetInt32();
                }
                await db.SaveChangesAsync(); // 保存派单状态和接单人员
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateTicket] Auto-dispatch failed: {ex.Message}");
        }

        return Ok(new { success = true, data = ticket, message = "工单创建成功" });
    }

    // PUT /api/tenant/tickets/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTicket(int id, [FromBody] TenantUpdateTicketRequest request)
    {
        using var db = CreateDbContext();
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null)
            return NotFound(new { success = false, message = "工单不存在" });

        if (!string.IsNullOrEmpty(request.Title)) ticket.Title = request.Title;
        if (!string.IsNullOrEmpty(request.Description)) ticket.Description = request.Description;
        if (!string.IsNullOrEmpty(request.Status)) ticket.Status = request.Status;
        if (request.Rating.HasValue) ticket.Rating = (int)(request.Rating ?? 0);
        _logger.LogInformation("UpdateTicket id={Id} Rating={Rating}", id, request.Rating);
        if (!string.IsNullOrEmpty(request.Priority)) ticket.Priority = request.Priority;
        if (!string.IsNullOrEmpty(request.Category)) ticket.Category = request.Category;
        if (!string.IsNullOrEmpty(request.ContactPersonName)) ticket.ContactPersonName = request.ContactPersonName;
        if (!string.IsNullOrEmpty(request.ContactPhone)) ticket.ContactPhone = request.ContactPhone;
        if (!string.IsNullOrEmpty(request.Location)) ticket.Location = request.Location;
        // 更新时如果改了 TicketTypeId 或 JobTypeId，自动重建标题
        bool titleChanged = request.JobTypeId.HasValue && request.JobTypeId != ticket.JobTypeId;
        if (request.TicketTypeId.HasValue && request.TicketTypeId != ticket.TicketTypeId)
            titleChanged = true;
        if (titleChanged)
        {
            ticket.TicketTypeId = request.TicketTypeId ?? ticket.TicketTypeId;
            ticket.JobTypeId = request.JobTypeId ?? ticket.JobTypeId;
            var ticketTypeNames = await GetTicketTypeNames(ticket.TicketTypeId.HasValue ? new List<int> { ticket.TicketTypeId.Value } : new List<int>(), db);
            var jobTypeNames = await GetJobTypeNames(ticket.JobTypeId.HasValue ? new List<int> { ticket.JobTypeId.Value } : new List<int>(), db);
            ticket.Title = BuildTitle(
                ticket.TicketTypeId.HasValue ? ticketTypeNames.GetValueOrDefault(ticket.TicketTypeId.Value) : null,
                ticket.JobTypeId.HasValue ? jobTypeNames.GetValueOrDefault(ticket.JobTypeId.Value) : null
            );
        }
        else
        {
            if (request.JobTypeId.HasValue)
                ticket.JobTypeId = request.JobTypeId;
            if (request.TicketTypeId.HasValue)
                ticket.TicketTypeId = request.TicketTypeId;
        }

        ticket.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { success = true, data = ticket, message = "工单更新成功" });
    }

    // DELETE /api/tenant/tickets/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTicket(int id)
    {
        using var db = CreateDbContext();
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null)
            return NotFound(new { success = false, message = "工单不存在" });

        db.Tickets.Remove(ticket);
        await db.SaveChangesAsync();

        return Ok(new { success = true, message = "工单删除成功" });
    }

    // POST /api/tenant/tickets/{id}/accept
    [HttpPost("{id}/accept")]
    public async Task<IActionResult> AcceptTicket(int id)
    {
        using var db = CreateDbContext();
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null)
            return NotFound(new { success = false, message = "工单不存在" });

        // 验证工单状态：只有已派单（Dispatched）才能接单
        if (ticket.DispatchStatus != "Dispatched")
            return Ok(new { success = false, message = $"当前状态「{ticket.DispatchStatus}」无法接单，请先等待派单" });

        // 验证指派人是否是当前登录人
        var userId = GetUserIdFromJwt();
        if (userId.HasValue && ticket.AssigneePersonId != userId.Value)
            return Ok(new { success = false, message = "该工单未指定给您，您无法接单" });

        // 记录变更前的状态
        var oldStatus = ticket.Status;
        var oldDispatchStatus = ticket.DispatchStatus;

        // 执行接单：Dispatched → Accepted，Status → InProgress
        ticket.DispatchStatus = "Accepted";
        ticket.Status = "InProgress";
        ticket.AssignedAt = DateTime.UtcNow;
        ticket.StartedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // 广播工单状态变更事件
        await BroadcastTicketEventAsync("status_changed", ticket.Id, ticket.TicketCode, ticket.Status, oldDispatchStatus);

        return Ok(new { success = true, message = "接单成功", data = new { ticket.Id, ticket.TicketCode, ticket.Status, ticket.DispatchStatus } });
    }

    // POST /api/tenant/tickets/{id}/progress
    [HttpPost("{id}/progress")]
    public async Task<IActionResult> ProgressTicket(int id)
    {
        using var db = CreateDbContext();
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null)
            return NotFound(new { success = false, message = "工单不存在" });

        // 验证状态：只有 InProgress 才能开始处理
        if (ticket.Status != "InProgress")
            return Ok(new { success = false, message = $"当前状态「{ticket.Status}」无法开始处理" });

        ticket.Status = "Processing";
        ticket.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // 广播工单状态变更事件
        await BroadcastTicketEventAsync("status_changed", ticket.Id, ticket.TicketCode, ticket.Status, "InProgress");

        return Ok(new { success = true, message = "已开始处理", data = new { ticket.Id, ticket.TicketCode, ticket.Status } });
    }

    // POST /api/tenant/tickets/{id}/finish
    [HttpPost("{id}/finish")]
    public async Task<IActionResult> FinishTicket(int id)
    {
        using var db = CreateDbContext();
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null)
            return NotFound(new { success = false, message = "工单不存在" });

        // 验证状态：只有 Processing 才能完成
        if (ticket.Status != "Processing")
            return Ok(new { success = false, message = $"当前状态「{ticket.Status}」无法完成" });

        ticket.Status = "Completed";
        ticket.FinishedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // 广播工单状态变更事件
        await BroadcastTicketEventAsync("status_changed", ticket.Id, ticket.TicketCode, ticket.Status, "Processing");

        return Ok(new { success = true, message = "工单已完成", data = new { ticket.Id, ticket.TicketCode, ticket.Status } });
    }

    // POST /api/tenant/tickets/{id}/rate
    [HttpPost("{id}/rate")]
    public async Task<IActionResult> RateTicket(int id, [FromBody] RateRequest request)
    {
        using var db = CreateDbContext();
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null)
            return NotFound(new { success = false, message = "工单不存在" });

        // 验证状态：只有 Finished 或 Survey_Pending 才能评价
        if (ticket.Status != "Finished" && ticket.Status != "Survey_Pending" && ticket.Status != "Completed")
            return Ok(new { success = false, message = $"当前状态「{ticket.Status}」无法评价（需先完成）" });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var operatorId = int.TryParse(userId, out var oid) ? oid : 0;
        var operatorName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "系统";
        var fromStatus = ticket.Status;

        ticket.Status = "Closed";
        ticket.Rating = request.Rating;
        ticket.CompletedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        db.TicketProcessRecords.Add(new TicketProcessRecord
        {
            TicketId = id,
            Action = "rate",
            OperatorId = operatorId,
            OperatorName = operatorName,
            FromStatus = fromStatus,
            ToStatus = "Closed",
            Content = $"评价: {request.Rating}星" + (request.Comment != null ? $" ({request.Comment})" : ""),
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        // 广播工单状态变更事件
        await BroadcastTicketEventAsync("status_changed", ticket.Id, ticket.TicketCode, ticket.Status, fromStatus);

        return Ok(new { success = true, message = "感谢您的评价", data = new { ticket.Id, ticket.TicketCode, ticket.Status, ticket.Rating } });
    }


    // 广播工单事件到 Gateway WebSocket
    private async Task BroadcastTicketEventAsync(string eventType, int ticketId, string ticketCode, string status, string previousStatus)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Gateway");
            var payload = new
            {
                module = "ticket",
                eventType = eventType,
                data = new
                {
                    ticketId,
                    ticketCode,
                    status,
                    previousStatus,
                    updatedAt = DateTime.UtcNow
                }
            };
            await client.PostAsJsonAsync("/internal/events/publish", payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "广播工单事件失败: {EventType} ticketId={TicketId}", eventType, ticketId);
        }
    }
}

public class TenantCreateTicketRequest
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public string? Location { get; set; }
    public int ProjectId { get; set; }
    public List<string>? Images { get; set; }
    public int? TicketTypeId { get; set; }
    public int? AreaId { get; set; }
    public int? BuildingId { get; set; }
    public int? RoomId { get; set; }
    public int? JobTypeId { get; set; }
    public string? ContactPersonName { get; set; }
    public string? ContactPhone { get; set; }
    public int? CreatorPersonId { get; set; }
}

public class TenantUpdateTicketRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Category { get; set; }
    public int? TicketTypeId { get; set; }
    public int? AreaId { get; set; }
    public int? BuildingId { get; set; }
    public int? RoomId { get; set; }
    public int? JobTypeId { get; set; }
    public string? ContactPersonName { get; set; }
    public string? ContactPhone { get; set; }
    public string? Location { get; set; }
    public double? Rating { get; set; }
}