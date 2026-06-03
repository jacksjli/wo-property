using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using WO.Property.DispatchService.Data;
using WO.Property.DispatchService.Models;
using WO.Property.DispatchService.Tenant;
using System.Text;

namespace WO.Property.DispatchService.Controllers;

/// <summary>
/// 派单控制器
/// </summary>
[ApiController]
[Authorize]
[Route("api/tenant/dispatch")]
public class TenantDispatchController : ControllerBase
{
    // 角色升级顺序
    private static readonly string[] EscalationRoles = {
        "operator", "supervisor", "manager", "department_head", "company_head"
    };

    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TenantDispatchController> _logger;

    public TenantDispatchController(
        IDbContextFactory<TenantDbContext> dbFactory,
        ITenantDbFactory tenantDbFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<TenantDispatchController> logger)
    {
        _dbFactory = dbFactory;
        _tenantDbFactory = tenantDbFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private async Task PublishDispatchEventAsync(string eventType, DispatchRecord record)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Gateway");
            var payload = new
            {
                module = "dispatch",
                eventType = eventType,
                data = new
                {
                    id = record.Id,
                    ticketId = record.TicketId,
                    ticketCode = record.TicketCode,
                    status = record.Status,
                    fromPersonId = record.FromPersonId,
                    toPersonId = record.ToPersonId,
                    toPersonName = record.ToPersonName
                }
            };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );
            await client.PostAsync("/internal/events/publish", content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish dispatch event: {EventType}", eventType);
        }
    }

    /// <summary>
    /// 自动派单
    /// </summary>
    [HttpPost("auto")]
    public async Task<IActionResult> AutoDispatch([FromBody] AutoDispatchRequest request)
    {
        try
        {
            _logger.LogInformation("AutoDispatch started for ticket: {TicketCode}", request.TicketCode);
            var tenantCode = _tenantDbFactory.GetCurrentTenantCode() ?? "wo_property";

            using var db = await _dbFactory.CreateDbContextAsync();

            // 1. 查询候选人员（工种+区域匹配）
            var authHeader = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var candidates = await GetCandidatePersons(request.JobTypeId, request.AreaId, request.BuildingId, tenantCode, authHeader);
            if (candidates == null || candidates.Count == 0)
            {
                return Ok(new { success = false, message = "没有符合条件的维修人员" });
            }

            // 2. 获取每个人的当前负载
            var candidateIds = candidates.Select(c => c.Id).ToList();
            var workloads = await db.PersonWorkloads
                .Where(w => candidateIds.Contains(w.PersonId))
                .ToDictionaryAsync(w => w.PersonId);

            // 3. 负载均衡选人（最少派单策略）
            // 优先级：operator > supervisor > manager > 其他
            var selectedPerson = candidates.OrderBy(c =>
            {
                var rolePriority = c.Role.ToLower() switch {
                    "operator" => 0,
                    "supervisor" => 1,
                    "manager" => 2,
                    _ => 3
                };
                return rolePriority;
            }).ThenBy(c =>
            {
                var workload = workloads.GetValueOrDefault(c.Id);
                return workload?.ActiveTicketCount ?? 0;
            }).First();

            // 4. 写入派单记录
            var dispatchRecord = new DispatchRecord
            {
                TicketId = request.TicketId,
                TicketCode = request.TicketCode,
                DispatchTime = DateTime.UtcNow,
                FromPersonId = 0,
                FromPersonName = "系统",
                ToPersonId = selectedPerson.Id,
                ToPersonName = selectedPerson.Name,
                Status = "Pending",
                Source = "Auto",
                TenantCode = tenantCode,
                ProjectId = request.ProjectId,
                CreatedAt = DateTime.UtcNow
            };

            db.DispatchRecords.Add(dispatchRecord);
            await db.SaveChangesAsync();

            // 5. 更新人员负载
            await UpdatePersonWorkload(selectedPerson.Id, tenantCode);

            // 6. 发布派单事件
            await PublishDispatchEventAsync("dispatched", dispatchRecord);

            _logger.LogInformation("AutoDispatch completed: PersonId={PersonId}, Name={Name}", selectedPerson.Id, selectedPerson.Name);

            return Ok(new
            {
                success = true,
                data = new
                {
                    dispatchRecordId = dispatchRecord.Id,
                    personId = selectedPerson.Id,
                    personName = selectedPerson.Name,
                    status = dispatchRecord.Status,
                    dispatchTime = dispatchRecord.DispatchTime
                },
                message = "派单成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AutoDispatch failed for ticket: {TicketCode}", request.TicketCode);
            return Ok(new { success = false, message = $"派单失败: {ex.Message}" });
        }
    }

    /// <summary>
    /// 获取候选人员（工种+区域匹配）
    /// </summary>
    private async Task<List<PersonInfo>?> GetCandidatePersons(int jobTypeId, int areaId, int buildingId, string tenantCode, string? authToken)
    {
        try
        {
            // 调用 PersonService 获取人员
            var client = _httpClientFactory.CreateClient("PersonService");
            if (!string.IsNullOrEmpty(authToken))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            var response = await client.GetAsync($"/api/tenant/persons?page=1&pageSize=100");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get persons from PersonService");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = System.Text.Json.JsonDocument.Parse(content);
            var data = json.RootElement.GetProperty("data");

            var persons = new List<PersonInfo>();
            foreach (var item in data.EnumerateArray())
            {
                Console.WriteLine($"[Dispatch] Processing person ID={item.GetProperty("id").GetInt32()}");
                
                // 解析 JSON 数组（specialtyIds 可能是 ["1","2"] 或 [1,2] 格式）
                var specialtyIds = item.TryGetProperty("specialtyIds", out var s) ? s.GetString() : null;
                Console.WriteLine($"[Dispatch] specialtyIds raw: {specialtyIds}");
                List<int> specialties = new List<int>();
                if (!string.IsNullOrEmpty(specialtyIds))
                {
                    // 尝试解析为字符串数组 ["1","2"]
                    try {
                        var strList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(specialtyIds) ?? new List<string>();
                        Console.WriteLine($"[Dispatch] strList: {string.Join(",", strList)}");
                        foreach (var str in strList)
                        {
                            if (int.TryParse(str, out var v) && v > 0)
                                specialties.Add(v);
                        }
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"[Dispatch] String parse failed: {ex.Message}");
                        // 如果失败，尝试解析为整数数组 [1,2]
                        try {
                            var intList = System.Text.Json.JsonSerializer.Deserialize<List<int>>(specialtyIds) ?? new List<int>();
                            foreach (var v in intList)
                                if (v > 0) specialties.Add(v);
                        }
                        catch (Exception ex2) {
                            Console.WriteLine($"[Dispatch] Int parse also failed: {ex2.Message}");
                        }
                    }
                }
                Console.WriteLine($"[Dispatch] specialties parsed: {string.Join(",", specialties)}");
                Console.WriteLine($"[Dispatch] Looking for jobTypeId={jobTypeId}, areaId={areaId}, buildingId={buildingId}");
                
                // 解析 building_ids
                List<int> personBuildings = new List<int>();
                if (item.TryGetProperty("buildingIds", out var buildingIdsElement))
                {
                    var buildingIdsStr = buildingIdsElement.GetString();
                    if (!string.IsNullOrEmpty(buildingIdsStr))
                    {
                        try {
                            var strList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(buildingIdsStr) ?? new List<string>();
                            foreach (var str in strList)
                                if (int.TryParse(str, out var v) && v > 0) personBuildings.Add(v);
                        }
                        catch {
                            try {
                                var intList = System.Text.Json.JsonSerializer.Deserialize<List<int>>(buildingIdsStr) ?? new List<int>();
                                foreach (var v in intList)
                                    if (v > 0) personBuildings.Add(v);
                            }
                            catch { }
                        }
                    }
                }

                // areaIds 可能不存在于 API 返回中，默认为空列表
                List<int> areas = new List<int>();
                if (item.TryGetProperty("areaIds", out var areaIdsElement))
                {
                    var areaIdsStr = areaIdsElement.GetString();
                    if (!string.IsNullOrEmpty(areaIdsStr))
                    {
                        // 尝试解析为字符串数组 ["1","2"]
                        try {
                            var strList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(areaIdsStr) ?? new List<string>();
                            foreach (var str in strList)
                            {
                                if (int.TryParse(str, out var v) && v > 0)
                                    areas.Add(v);
                            }
                        }
                        catch {
                            // 如果失败，尝试解析为整数数组 [1,2]
                            try {
                                var intList = System.Text.Json.JsonSerializer.Deserialize<List<int>>(areaIdsStr) ?? new List<int>();
                                foreach (var v in intList)
                                    if (v > 0) areas.Add(v);
                            }
                            catch { }
                        }
                    }
                }

                // 匹配工种和区域
                // areaId == 0 表示不限制区域，或者 areas 为空表示该人员覆盖所有区域
                bool areaMatch = areaId == 0 || areas.Count == 0 || areas.Contains(areaId);
                Console.WriteLine($"[Dispatch] areaId={areaId}, areas.Count={areas.Count}, areaMatch={areaMatch}");

                // 匹配楼栋
                // buildingId == 0 表示不限制楼栋，或者 personBuildings 为空表示该人员负责该区域所有楼栋
                bool buildingMatch = buildingId == 0 || personBuildings.Count == 0 || personBuildings.Contains(buildingId);
                Console.WriteLine($"[Dispatch] buildingId={buildingId}, personBuildings.Count={personBuildings.Count}, buildingMatch={buildingMatch}");

                if (specialties.Contains(jobTypeId) && areaMatch && buildingMatch)
                {
                    Console.WriteLine($"[Dispatch] MATCHED!");
                    persons.Add(new PersonInfo
                    {
                        Id = item.GetProperty("id").GetInt32(),
                        Name = item.GetProperty("name").GetString() ?? "",
                        Phone = item.GetProperty("phone").GetString() ?? "",
                        Role = item.TryGetProperty("role", out var r) ? r.GetString() ?? "" : ""
                    });
                }
            }
            Console.WriteLine($"[Dispatch] Total matched persons: {persons.Count}");
            return persons;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCandidatePersons failed");
            return null;
        }
    }

    /// <summary>
    /// 更新人员负载
    /// </summary>
    private async Task UpdatePersonWorkload(int personId, string tenantCode)
    {
        using var db = await _dbFactory.CreateDbContextAsync();

        var workload = await db.PersonWorkloads.FirstOrDefaultAsync(w => w.PersonId == personId);
        if (workload == null)
        {
            workload = new PersonWorkload
            {
                PersonId = personId,
                ActiveTicketCount = 1,
                TotalDispatched = 1,
                TotalCompleted = 0,
                LastDispatchTime = DateTime.UtcNow,
                TenantCode = tenantCode,
                ProjectId = 1
            };
            db.PersonWorkloads.Add(workload);
        }
        else
        {
            workload.ActiveTicketCount++;
            workload.TotalDispatched++;
            workload.LastDispatchTime = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// 获取待处理工单列表
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingDispatches([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] long? personId = null)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode() ?? "wo_property";

        var query = db.DispatchRecords
            .Where(d => d.TenantCode == tenantCode && d.Status == "Pending");

        if (personId.HasValue)
            query = query.Where(d => d.ToPersonId == personId.Value);

        var orderedQuery = query.OrderByDescending(d => d.DispatchTime);
        var total = await query.CountAsync();
        var items = orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = items,
            total,
            page,
            pageSize
        });
    }

    /// <summary>
    /// 接收工单
    /// </summary>
    [HttpPost("{id}/receive")]
    public async Task<IActionResult> ReceiveDispatch(long id)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var record = await db.DispatchRecords.FindAsync(id);

        if (record == null)
        {
            return Ok(new { success = false, message = "派单记录不存在" });
        }

        record.Status = "WReceived";
        await db.SaveChangesAsync();

        // 通过 TicketService PUT /tickets/{id} 更新状态并写入时间戳
        await UpdateTicketStatusAsync(record.TicketId, "InProgress", record.TenantCode, DateTime.UtcNow, null, null);

        // 发布接单事件
        await PublishDispatchEventAsync("received", record);

        return Ok(new { success = true, message = "已接收" });
    }

    /// <summary>
    /// 完工提交
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteDispatch(long id, [FromBody] CompleteRequest? request)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var record = await db.DispatchRecords.FindAsync(id);

        if (record == null)
        {
            return Ok(new { success = false, message = "派单记录不存在" });
        }

        record.Status = "Completed";
        record.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // 通过 TicketService PUT /tickets/{id} 更新状态并写入时间戳
        await UpdateTicketStatusAsync(record.TicketId, "Completed", record.TenantCode, null, DateTime.UtcNow, DateTime.UtcNow);

        // 更新人员负载（active_ticket_count - 1, total_completed + 1）
        var workload = await db.PersonWorkloads.FirstOrDefaultAsync(w => w.PersonId == record.ToPersonId);
        if (workload != null)
        {
            workload.ActiveTicketCount = Math.Max(0, workload.ActiveTicketCount - 1);
            workload.TotalCompleted++;
        }

        // 发布完工事件
        await PublishDispatchEventAsync("completed", record);

        return Ok(new
        {
            success = true,
            data = new
            {
                dispatchRecordId = record.Id,
                status = record.Status,
                completedAt = record.CompletedAt
            },
            message = "完工已提交"
        });
    }

    /// <summary>
    /// 更新工单状态（通过 HTTP 调用 TicketService）
    /// </summary>
    /// <summary>
    /// 接单时：调用 TicketService /accept 接口，写入 startedAt
    /// </summary>
    private async Task UpdateTicketStartAsync(long ticketId, string tenantCode)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("DispatchService");
            httpClient.BaseAddress = new Uri("http://localhost:5102");
            httpClient.DefaultRequestHeaders.Remove("X-Tenant");
            httpClient.DefaultRequestHeaders.Add("X-Tenant", tenantCode);
            
            var payload = new { notes = "派单接单" };
            var response = await httpClient.PostAsJsonAsync($"/api/tenant/tickets/{ticketId}/accept", payload);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Ticket {TicketId} started (startedAt written)", ticketId);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to start ticket {TicketId}: {StatusCode} - {Content}", 
                    ticketId, response.StatusCode, content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start ticket {TicketId}", ticketId);
        }
    }

    /// <summary>
    /// 完工时：调用 TicketService /finish 接口，写入 finishedAt
    /// </summary>
    private async Task UpdateTicketFinishAsync(long ticketId, string tenantCode)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("DispatchService");
            httpClient.BaseAddress = new Uri("http://localhost:5102");
            httpClient.DefaultRequestHeaders.Remove("X-Tenant");
            httpClient.DefaultRequestHeaders.Add("X-Tenant", tenantCode);
            
            var payload = new { finishNote = "完工" };
            var response = await httpClient.PostAsJsonAsync($"/api/tenant/tickets/{ticketId}/finish", payload);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Ticket {TicketId} finished (finishedAt written)", ticketId);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to finish ticket {TicketId}: {StatusCode} - {Content}", 
                    ticketId, response.StatusCode, content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to finish ticket {TicketId}", ticketId);
        }
    }

    /// <summary>
    /// 通用状态+时间更新（通过 HTTP 调用 TicketService PUT /tickets/{id}）
    /// </summary>
    private async Task UpdateTicketStatusAsync(long ticketId, string status, string tenantCode, DateTime? startedAt, DateTime? finishedAt, DateTime? completedAt, double? rating = null)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("DispatchService");
            httpClient.BaseAddress = new Uri("http://localhost:5102");
            httpClient.DefaultRequestHeaders.Remove("X-Project");
            httpClient.DefaultRequestHeaders.Add("X-Project", tenantCode);
            
            var payload = new { status, startedAt, finishedAt, completedAt, rating };
            var response = await httpClient.PutAsJsonAsync($"/api/tenant/tickets/{ticketId}", payload);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Updated ticket {TicketId} status={Status} startedAt={StartedAt} finishedAt={FinishedAt}", 
                    ticketId, status, startedAt != null, finishedAt != null);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to update ticket {TicketId}: {StatusCode} - {Content}", 
                    ticketId, response.StatusCode, content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update ticket {TicketId}", ticketId);
        }
    }

    /// <summary>
    /// 获取派单历史
    /// </summary>
    [HttpGet("history/{ticketId}")]
    public async Task<IActionResult> GetDispatchHistory(long ticketId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();

        var records = await db.DispatchRecords
            .Where(d => d.TicketId == ticketId)
            .OrderByDescending(d => d.DispatchTime)
            .ToListAsync();

        return Ok(new { success = true, data = records });
    }

    /// <summary>
    /// 获取工单升级记录（含升级状态）
    /// </summary>
    [HttpGet("escalations/{ticketId}")]
    public async Task<IActionResult> GetEscalations(long ticketId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();


        // 查询该工单的所有升级记录
        var escalations = await db.TimeoutEscalations
            .Where(e => e.TicketId == ticketId)
            .OrderByDescending(e => e.EscalatedAt)
            .ToListAsync();

        // 查询该工单的所有派单记录（包含升级信息）
        var dispatchRecords = await db.DispatchRecords
            .Where(d => d.TicketId == ticketId)
            .OrderByDescending(d => d.DispatchTime)
            .ToListAsync();


        // 构建升级状态响应
        var latestEscalation = escalations.FirstOrDefault();
        var currentLevel = latestEscalation?.Level ?? 0;
        var nextRole = currentLevel < EscalationRoles.Length ? EscalationRoles[currentLevel] : null;


        var response = new
        {
            success = true,
            data = new
            {
                currentLevel = currentLevel,
                currentStatus = latestEscalation?.Status ?? "Active",
                nextEscalationRole = nextRole,
                escalations = escalations.Select(e => new
                {
                    e.Id,
                    e.Level,
                    e.FromPersonId,
                    e.FromPersonName,
                    e.ToPersonId,
                    e.ToPersonName,
                    e.ToRole,
                    e.EscalatedAt,
                    e.Status
                }),
                dispatchRecords = dispatchRecords.Select(d => new
                {
                    d.Id,
                    d.ToPersonId,
                    d.ToPersonName,
                    d.Status,
                    d.Source,
                    d.EscalationLevel,
                    d.ParentDispatchId,
                    d.DispatchTime
                })
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// 申请转单（简化流程：直接转给目标工程师，无需审批）
    /// </summary>
    [HttpPost("transfer")]
    public async Task<IActionResult> TransferDispatch([FromBody] TransferDispatchRequest request)
    {
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var tenantCode = _tenantDbFactory.GetCurrentTenantCode() ?? "wo_property";

            var original = await db.DispatchRecords.FindAsync(request.DispatchRecordId);
            if (original == null)
                return Ok(new { success = false, message = "派单记录不存在" });

            if (string.IsNullOrWhiteSpace(request.Reason))
                return Ok(new { success = false, message = "转单原因必填" });

            // 记录原派单为 Transferred
            original.Status = "Transferred";
            original.UpdatedAt = DateTime.UtcNow;

            // 创建新的派单记录（转单目标）
            var newDispatch = new DispatchRecord
            {
                TicketId = original.TicketId,
                TicketCode = original.TicketCode,
                DispatchTime = DateTime.UtcNow,
                FromPersonId = original.ToPersonId,
                FromPersonName = original.ToPersonName,
                ToPersonId = request.ToPersonId,
                ToPersonName = request.ToPersonName,
                Status = "Pending",
                Source = "Transfer",
                TenantCode = tenantCode,
                ProjectId = original.ProjectId,
                SourceDispatchId = original.Id,
                SourceType = "Transfer",
                CreatedAt = DateTime.UtcNow
            };
            db.DispatchRecords.Add(newDispatch);

            // 记录转单凭证
            var transferRequest = new TransferRequest
            {
                DispatchRecordId = request.DispatchRecordId,
                TicketId = original.TicketId,
                TicketCode = original.TicketCode,
                FromPersonId = original.ToPersonId,
                FromPersonName = original.ToPersonName,
                ToPersonId = request.ToPersonId,
                ToPersonName = request.ToPersonName,
                Reason = request.Reason,
                Status = "Pending",
                TenantCode = tenantCode,
                ProjectId = original.ProjectId,
                CreatedAt = DateTime.UtcNow
            };
            db.TransferRequests.Add(transferRequest);

            await db.SaveChangesAsync();
            return Ok(new { success = true, data = new
            {
                transferRequestId = transferRequest.Id,
                newDispatchRecordId = newDispatch.Id,
                status = "Pending",
                message = "已提交转单请求，等待对方确认"
            }, message = "已提交转单请求，等待对方确认" });
        }
        catch (Exception ex) { return Ok(new { success = false, message = $"转单失败: {ex.Message}" }); }
    }

    /// <summary>
    /// 接受转单（目标工程师接受）
    /// </summary>
    [HttpPut("{id}/accept")]
    public async Task<IActionResult> AcceptDispatch(long id)
    {
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var dispatch = await db.DispatchRecords.FindAsync(id);
            if (dispatch == null)
                return Ok(new { success = false, message = "派单记录不存在" });

            if (dispatch.Status != "Pending")
                return Ok(new { success = false, message = $"当前状态「{dispatch.Status}」无法接受" });

            dispatch.Status = "Accepted";
            dispatch.AcceptedAt = DateTime.UtcNow;
            dispatch.UpdatedAt = DateTime.UtcNow;

            // 更新 TransferRequest 状态
            var transfer = await db.TransferRequests
                .Where(t => t.DispatchRecordId == dispatch.SourceDispatchId && t.ToPersonId == dispatch.ToPersonId && t.Status == "Pending")
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
            if (transfer != null)
            {
                transfer.Status = "Accepted";
                transfer.ApprovedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "已接受工单，请按正常流程处理" });
        }
        catch (Exception ex) { return Ok(new { success = false, message = $"接受失败: {ex.Message}" }); }
    }

    /// <summary>
    /// 拒绝转单（目标工程师拒绝，原工单恢复给原工程师）
    /// </summary>
    [HttpPut("{id}/reject")]
    public async Task<IActionResult> RejectDispatch(long id, [FromBody] RejectDispatchRequest request)
    {
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var dispatch = await db.DispatchRecords.FindAsync(id);
            if (dispatch == null)
                return Ok(new { success = false, message = "派单记录不存在" });

            if (dispatch.Status != "Pending")
                return Ok(new { success = false, message = $"当前状态「{dispatch.Status}」无法拒绝" });

            dispatch.Status = "Rejected";
            dispatch.UpdatedAt = DateTime.UtcNow;

            // 恢复原派单状态（继续 SLA 计时，不豁免）
            var original = await db.DispatchRecords.FindAsync(dispatch.SourceDispatchId);
            if (original != null)
            {
                original.Status = "Processing";
                original.UpdatedAt = DateTime.UtcNow;
            }

            // 更新 TransferRequest 状态
            var transfer = await db.TransferRequests
                .Where(t => t.DispatchRecordId == dispatch.SourceDispatchId && t.ToPersonId == dispatch.ToPersonId && t.Status == "Pending")
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
            if (transfer != null)
            {
                transfer.Status = "Rejected";
                transfer.ApprovedAt = DateTime.UtcNow;
                if (request?.Reason != null)
                    transfer.ApprovedReason = request.Reason;
            }

            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "已拒绝，原工程师继续处理" });
        }
        catch (Exception ex) { return Ok(new { success = false, message = $"拒绝失败: {ex.Message}" }); }
    }

    /// <summary>
    /// 获取我的待处理转单（目标工程师查看自己被转交的工单）
    /// </summary>
    [HttpGet("transfer/pending-for-me")]
    public async Task<IActionResult> GetMyPendingTransfers([FromQuery] int personId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode() ?? "wo_property";

        var query = db.DispatchRecords
            .Where(d => d.ToPersonId == personId && d.Status == "Pending" && d.SourceType == "Transfer")
            .OrderByDescending(d => d.CreatedAt);

        var items = await query.ToListAsync();
        return Ok(new { success = true, data = items });
    }

    [HttpGet("transfer/pending")]
    public async Task<IActionResult> GetPendingTransfers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode() ?? "wo_property";
        var query = db.TransferRequests.Where(t => t.TenantCode == tenantCode && t.Status == "Pending").OrderByDescending(t => t.CreatedAt);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { success = true, data = items, total, page, pageSize });
    }

    /// <summary>
    /// 获取工单当前活跃的派单记录（用于转单）
    /// </summary>
    [HttpGet("active/{ticketCode}")]
    public async Task<IActionResult> GetActiveDispatch(string ticketCode)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode() ?? "wo_property";

        // 找到该工单最新一条非终结状态的派单记录
        var active = await db.DispatchRecords
            .Where(d => d.TicketCode == ticketCode &&
                        (d.Status == "Accepted" || d.Status == "Processing" || d.Status == "InProgress" || d.Status == "Transferred"))
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync();

        if (active == null)
            return Ok(new { success = false, message = "无进行中的派单记录" });

        return Ok(new { success = true, data = active });
    }

    [HttpPut("transfer/{id}")]
    public async Task<IActionResult> ApproveTransfer(long id, [FromBody] ApproveTransferRequest request)
    {
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var tenantCode = _tenantDbFactory.GetCurrentTenantCode() ?? "wo_property";
            var transferRequest = await db.TransferRequests.FindAsync(id);
            if (transferRequest == null) return Ok(new { success = false, message = "转单申请不存在" });

            if (request.Action == "reject")
            {
                transferRequest.Status = "Rejected";
                transferRequest.ApprovedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return Ok(new { success = true, message = "转单已拒绝" });
            }

            transferRequest.Status = "Approved";
            transferRequest.ApprovedAt = DateTime.UtcNow;
            var originalDispatch = await db.DispatchRecords.FindAsync(transferRequest.DispatchRecordId);
            if (originalDispatch != null)
            {
                originalDispatch.Status = "Transferred";
                var newDispatch = new DispatchRecord
                {
                    TicketId = transferRequest.TicketId, TicketCode = transferRequest.TicketCode,
                    DispatchTime = DateTime.UtcNow, FromPersonId = transferRequest.FromPersonId,
                    FromPersonName = transferRequest.FromPersonName, ToPersonId = transferRequest.ToPersonId,
                    ToPersonName = transferRequest.ToPersonName, Status = "Pending", Source = "Transfer",
                    TenantCode = tenantCode, ProjectId = transferRequest.ProjectId, CreatedAt = DateTime.UtcNow
                };
                db.DispatchRecords.Add(newDispatch);
                transferRequest.TransferDispatchId = newDispatch.Id;
            }
            await db.SaveChangesAsync();
            return Ok(new { success = true, data = new { newDispatchRecordId = transferRequest.TransferDispatchId, status = transferRequest.Status }, message = "转单已审批通过" });
        }
        catch (Exception ex) { return Ok(new { success = false, message = $"审批失败: {ex.Message}" }); }
    }

    [HttpGet("transfer/history")]
    public async Task<IActionResult> GetTransferHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode() ?? "wo_property";
        var query = db.TransferRequests.Where(t => t.TenantCode == tenantCode).OrderByDescending(t => t.CreatedAt);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { success = true, data = items, total, page, pageSize });
    }

    /// <summary>
    /// 确认完工并评价
    /// </summary>
    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> ConfirmDispatch(long id, [FromBody] ConfirmRequest request)
    {
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var tenantCode = _tenantDbFactory.GetCurrentTenantCode() ?? "wo_property";

            var dispatchRecord = await db.DispatchRecords.FindAsync(id);
            if (dispatchRecord == null)
                return Ok(new { success = false, message = "派单记录不存在" });

            var rating = new SatisfactionRating
            {
                TicketId = dispatchRecord.TicketId,
                TicketCode = dispatchRecord.TicketCode,
                DispatchRecordId = id,
                RaterId = request.RaterId,
                RaterName = request.RaterName,
                RateeId = dispatchRecord.ToPersonId,
                RateeName = dispatchRecord.ToPersonName,
                QualityScore = request.QualityScore > 0 ? request.QualityScore : 5,
                AttitudeScore = request.AttitudeScore > 0 ? request.AttitudeScore : 5,
                TimelinessScore = request.TimelinessScore > 0 ? request.TimelinessScore : 5,
                OverallScore = request.OverallScore > 0 ? request.OverallScore : 5,
                Comment = request.Comment,
                RatedAt = DateTime.UtcNow,
                IsAutoRated = false,
                TenantCode = tenantCode,
                ProjectId = dispatchRecord.ProjectId,
                CreatedAt = DateTime.UtcNow
            };
            db.SatisfactionRatings.Add(rating);

            dispatchRecord.Status = "Confirmed";
            dispatchRecord.ConfirmedAt = DateTime.UtcNow;
            dispatchRecord.ConfirmedBy = request.RaterId;
            dispatchRecord.ConfirmedByName = request.RaterName;
            dispatchRecord.RatingId = rating.Id;

            // 通过 TicketService HTTP API 更新工单状态和评价
            var ratingFloat = request.OverallScore > 0 ? request.OverallScore : 5.0;
            await UpdateTicketStatusAsync(dispatchRecord.TicketId, "Closed", tenantCode, null, null, null, ratingFloat);
            return Ok(new { success = true, data = new { dispatchRecordId = id, ratingId = rating.Id, status = dispatchRecord.Status }, message = "确认完成，感谢评价" });
        }
        catch (Exception ex) { return Ok(new { success = false, message = $"确认失败: {ex.Message}" }); }
    }

    [HttpGet("rating/{ticketId}")]
    public async Task<IActionResult> GetRating(long ticketId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode() ?? "wo_property";
        var rating = await db.SatisfactionRatings.Where(r => r.TenantCode == tenantCode && r.TicketId == ticketId).FirstOrDefaultAsync();
        return Ok(new { success = true, data = rating });
    }

    [HttpGet("rating/stats")]
    public async Task<IActionResult> GetRatingStats([FromQuery] int personId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode() ?? "wo_property";
        var stats = await db.SatisfactionRatings.Where(r => r.TenantCode == tenantCode && r.RateeId == personId)
            .GroupBy(r => r.RateeId)
            .Select(g => new { personId = g.Key, totalCount = g.Count(), avgQuality = g.Average(r => r.QualityScore), avgAttitude = g.Average(r => r.AttitudeScore), avgTimeliness = g.Average(r => r.TimelinessScore), avgOverall = g.Average(r => r.OverallScore) })
            .FirstOrDefaultAsync();
        return Ok(new { success = true, data = stats });
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> GetTimeoutAlerts([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? status = null, [FromQuery] int? personId = null)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode() ?? "wo_property";
        var query = db.TimeoutAlerts.Where(a => a.TenantCode == tenantCode);
        if (!string.IsNullOrEmpty(status)) query = query.Where(a => a.Status == status);
        if (personId.HasValue) query = query.Where(a => a.NotifyTargetId == personId.Value);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(a => a.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { success = true, data = items, total, page, pageSize });
    }

    [HttpPut("alerts/{id}/process")]
    public async Task<IActionResult> ProcessAlert(long id)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var alert = await db.TimeoutAlerts.FindAsync(id);
        if (alert == null) return Ok(new { success = false, message = "告警不存在" });
        alert.Status = "Processed";
        alert.ProcessedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { success = true, message = "告警已处理" });
    }
}

/// <summary>
/// 人员信息
/// </summary>
public class PersonInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Role { get; set; } = "";
}

/// <summary>
/// 自动派单请求
/// </summary>
public class AutoDispatchRequest
{
    public long TicketId { get; set; }
    public string TicketCode { get; set; } = "";
    public int TicketTypeId { get; set; }
    public string? TicketTypeName { get; set; }
    public int JobTypeId { get; set; }
    public int AreaId { get; set; }
    public string? AreaName { get; set; }
    public int BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public int RoomId { get; set; }
    public string? RoomName { get; set; }
    public int ProjectId { get; set; }
}

/// <summary>
/// 完工请求
/// </summary>
public class CompleteRequest
{
    public string? CompletionRemark { get; set; }
}

/// <summary>
/// 转单请求
/// </summary>
public class TransferDispatchRequest
{
    public long DispatchRecordId { get; set; }
    public int ToPersonId { get; set; }
    public string? ToPersonName { get; set; }
    public string Reason { get; set; } = "";
}

/// <summary>
/// 审批转单请求
/// </summary>
public class ApproveTransferRequest
{
    public string Action { get; set; } = "";
    public string? Reason { get; set; }
}

/// <summary>
/// 拒绝转单请求
/// </summary>
public class RejectDispatchRequest
{
    public string? Reason { get; set; }
}

/// <summary>
/// 确认完工请求
/// </summary>
public class ConfirmRequest
{
    public int RaterId { get; set; }
    public string? RaterName { get; set; }
    public int QualityScore { get; set; }
    public int AttitudeScore { get; set; }
    public int TimelinessScore { get; set; }
    public int OverallScore { get; set; }
    public string? Comment { get; set; }
}
