using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.DeviceService.Data;
using System.Text;
using System.Net.Http.Json;
using DeviceEntity = WO.Property.DeviceService.Data.Device;
using MaintenanceEntity = WO.Property.DeviceService.Data.MaintenanceRecord;
using TicketEntity = WO.Property.DeviceService.Data.Ticket;
using Microsoft.AspNetCore.Authorization;

namespace WO.Property.DeviceService.Controllers;

/// <summary>
/// 设备多租户 API 控制器
/// </summary>
[ApiController]
[Authorize]
[Route("api/tenant/devices")]
public class TenantDeviceController : ControllerBase
{
    // 获取当前项目代码（从 X-Project header）
    private string? GetProjectCode()
    {
        if (Request.Headers.TryGetValue("X-Project", out var projectValues))
        {
            var projectCode = projectValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(projectCode))
                return projectCode;
        }
        return null;
    }


    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TenantDeviceController> _logger;

    public TenantDeviceController(
        IDbContextFactory<TenantDbContext> dbFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<TenantDeviceController> logger)
    {
        _dbFactory = dbFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    private int? GetUserIdFromJwt()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return null;

        var token = authHeader.Substring("Bearer ".Length).Trim();
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return null;

            var payload = parts[1];
            var json = Encoding.UTF8.GetString(Base64UrlDecode(payload));

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("nameidentifier", out var uidElement))
            {
                return int.TryParse(uidElement.GetString(), out var uid) ? uid : null;
            }
            return null;
        }
        catch { return null; }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }
        return Convert.FromBase64String(output);
    }

    /// <summary>
    /// 获取设备列表（分页）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDevices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? status = null)
    {
        using var db = CreateDbContext();
        var query = db.Devices.AsQueryable();

            // 按 project_code 过滤（单租户多项目）
            var projectCode = GetProjectCode();
            if (!string.IsNullOrEmpty(projectCode))
            {
                query = query.Where(x => x.ProjectCode == projectCode);
            }


        if (categoryId.HasValue)
            query = query.Where(d => d.DeviceTypeId == categoryId.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(d => d.Status == status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { success = true, data = items, total, page, pageSize });
    }

    /// <summary>
    /// 获取设备详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDevice(int id)
    {
        using var db = CreateDbContext();
        var device = await db.Devices.FindAsync(id);

        if (device == null)
            return NotFound(new { success = false, message = "设备不存在" });

        return Ok(new { success = true, data = device });
    }

    /// <summary>
    /// 创建设备
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateDevice([FromBody] CreateTenantDeviceRequest request)
    {
        using var db = CreateDbContext();
        var projectCode = GetProjectCode() ?? "UNKNOWN";

        var device = new DeviceEntity
        {
            Code = request.Code,
            Name = request.Name,
            Model = request.Model,
            SerialNumber = request.SerialNumber,
            DeviceTypeId = request.DeviceTypeId,
            Location = request.Location,
            BuildingId = request.BuildingId,
            Floor = request.Floor,
            PurchaseDate = request.PurchaseDate,
            WarrantyEndDate = request.WarrantyEndDate,
            Status = request.Status ?? "Active",
            CurrentStatus = request.CurrentStatus ?? "Normal",
            Remarks = request.Remarks,
            ProjectCode = projectCode,
            CreatedAt = DateTime.UtcNow
        };

        db.Devices.Add(device);
        await db.SaveChangesAsync();

        _logger.LogInformation("Device created: {DeviceId}", device.Id);

        return Ok(new { success = true, data = device, message = "设备创建成功" });
    }

    /// <summary>
    /// 更新设备
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDevice(int id, [FromBody] UpdateTenantDeviceRequest request)
    {
        using var db = CreateDbContext();
        var device = await db.Devices.FindAsync(id);

        if (device == null)
            return NotFound(new { success = false, message = "设备不存在" });

        device.Name = request.Name;
        device.Model = request.Model;
        device.SerialNumber = request.SerialNumber;
        device.DeviceTypeId = request.DeviceTypeId ?? device.DeviceTypeId;
        device.BuildingId = request.BuildingId ?? device.BuildingId;
        device.Floor = request.Floor ?? device.Floor;
        device.Location = request.Location ?? device.Location;
        device.PurchaseDate = request.PurchaseDate;
        device.WarrantyEndDate = request.WarrantyEndDate;
        device.Status = request.Status ?? device.Status;
        device.CurrentStatus = request.CurrentStatus ?? device.CurrentStatus;
        device.Remarks = request.Remarks;
        device.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        _logger.LogInformation("Device updated: {DeviceId}", id);

        return Ok(new { success = true, data = device, message = "设备更新成功" });
    }

    /// <summary>
    /// 删除设备
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDevice(int id)
    {
        using var db = CreateDbContext();
        var device = await db.Devices.FindAsync(id);

        if (device == null)
            return NotFound(new { success = false, message = "设备不存在" });

        db.Devices.Remove(device);
        await db.SaveChangesAsync();

        _logger.LogInformation("Device deleted: {DeviceId}", id);

        return Ok(new { success = true, message = "设备删除成功" });
    }

    /// <summary>
    /// 获取设备的维护记录
    /// </summary>
    [HttpGet("{id}/maintenance")]
    public async Task<IActionResult> GetMaintenanceHistory(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        using var db = CreateDbContext();

        var query = db.MaintenanceRecords.Where(m => m.DeviceId == id);
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.MaintenanceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { success = true, data = items, total, page, pageSize });
    }

    /// <summary>
    /// 添备维护记录
    /// </summary>
    [HttpPost("{id}/maintenance")]
    public async Task<IActionResult> CreateMaintenanceRecord(int id, [FromBody] CreateMaintenanceRecordRequest request)
    {
        using var db = CreateDbContext();

        var device = await db.Devices.FindAsync(id);
        if (device == null)
            return NotFound(new { success = false, message = "设备不存在" });

        var record = new MaintenanceEntity
        {
            DeviceId = id,
            MaintenanceType = request.MaintenanceType,
            MaintenanceDate = request.MaintenanceDate,
            Description = request.Description,
            Technician = request.Technician,
            Cost = request.Cost,
            Hours = request.Hours,
            Remarks = request.Remarks,
            CreatedAt = DateTime.UtcNow
        };

        db.MaintenanceRecords.Add(record);
        await db.SaveChangesAsync();

        _logger.LogInformation("Maintenance record created for device {DeviceId}", id);

        return Ok(new { success = true, data = record, message = "维护记录创建成功" });
    }

    /// <summary>
    /// 设备报修（创建工单）
    /// </summary>
    [HttpPost("{id}/repair")]
    public async Task<IActionResult> CreateDeviceRepairTicket(int id, [FromBody] DeviceRepairRequest request)
    {
        using var db = CreateDbContext();

        var device = await db.Devices.FindAsync(id);
        if (device == null)
            return NotFound(new { success = false, message = "设备不存在" });

        var projectCode = GetProjectCode() ?? "UNKNOWN";

        var projectId = request.ProjectId ?? 1;

        // 生成工单编号
        var yearMonth = DateTime.Now.ToString("yyyyMM");
        var prefix = $"{projectCode}-WO-{yearMonth}-";
        var lastTicket = db.Tickets
            .Where(t => t.TicketCode.StartsWith(prefix))
            .OrderByDescending(t => t.TicketCode)
            .FirstOrDefault();
        int seq = lastTicket == null ? 10001 : int.Parse(lastTicket.TicketCode.Substring(prefix.Length)) + 1;
        var ticketNo = $"{prefix}{seq}";

        var ticket = new TicketEntity
        {
            TicketCode = ticketNo,
            Title = $"[设备报修] {device.Name}",
            Description = request.Description ?? $"设备 {device.Name}（编号：{device.Code}）需要维修\n故障描述：{request.FaultDescription}",
            Category = "设备维修",
            Priority = request.Priority ?? "Medium",
            Status = "New",
            ProjectCode = projectCode,
            ProjectId = projectId,
            BuildingId = device.BuildingId,
            Location = device.Location,
            TicketTypeId = request.TicketTypeId,
            JobTypeId = request.JobTypeId,
            CreatedAt = DateTime.UtcNow
        };

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        _logger.LogInformation("Device repair ticket created: {TicketCode} for device {DeviceId}", ticket.TicketCode, id);

        // 自动派单
        try
        {
            var client = _httpClientFactory.CreateClient();
            var token = Request.Headers["Authorization"].FirstOrDefault();
            var dispatchPayload = new
            {
                TicketId = ticket.Id,
                TicketCode = ticket.TicketCode,
                JobTypeId = ticket.JobTypeId ?? 0,
                TicketTypeId = ticket.TicketTypeId ?? 0,
                AreaId = 0,
                BuildingId = ticket.BuildingId ?? 0,
                ProjectId = projectId
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
                var dispatchResult = await dispatchResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                if (dispatchResult.TryGetProperty("data", out var dataEl) &&
                    dataEl.TryGetProperty("personId", out var personIdEl))
                {
                    ticket.AssigneePersonId = personIdEl.GetInt32();
                }
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Auto-dispatch failed: {Message}", ex.Message);
        }

        return Ok(new { success = true, data = ticket, message = "报修工单已创建" });
    }

    /// <summary>
    /// 获取设备统计
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetDeviceStats()
    {
        using var db = CreateDbContext();
        var projectCode = GetProjectCode();

        IQueryable<DeviceEntity> query = db.Devices;
        if (!string.IsNullOrEmpty(projectCode))
            query = query.Where(d => d.ProjectCode == projectCode);

        var total = await query.CountAsync();
        var activeCount = await query.CountAsync(d => d.Status == "Active");
        var maintenanceCount = await query.CountAsync(d => d.Status == "Maintenance");
        var scrappedCount = await query.CountAsync(d => d.Status == "Scrapped");
        var normalCount = await query.CountAsync(d => d.CurrentStatus == "Normal");
        var faultCount = await query.CountAsync(d => d.CurrentStatus == "Fault");


        var categoryStats = await query
            .Where(d => d.DeviceTypeId != null)
            .GroupBy(d => d.DeviceTypeId)
            .Select(g => new { deviceTypeId = g.Key, count = g.Count() })
            .ToListAsync();

        var recentDevices = await query
            .OrderByDescending(d => d.CreatedAt)
            .Take(5)
            .Select(d => new { d.Id, d.Code, d.Name, d.Status, d.CurrentStatus, d.CreatedAt })
            .ToListAsync();

        return Ok(new { success = true, data = new
        {
            total,
            status = new { active = activeCount, maintenance = maintenanceCount, scrapped = scrappedCount },
            currentStatus = new { normal = normalCount, fault = faultCount },
            categoryDistribution = categoryStats,
            recentDevices
        }});
    }
}

// 请求模型
public class CreateTenantDeviceRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public int? DeviceTypeId { get; set; }
    public int? BuildingId { get; set; }
    public int? Floor { get; set; }
    public string? Location { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public string? Status { get; set; }
    public string? CurrentStatus { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateTenantDeviceRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public int? DeviceTypeId { get; set; }
    public int? BuildingId { get; set; }
    public int? Floor { get; set; }
    public string? Location { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public string? Status { get; set; }
    public string? CurrentStatus { get; set; }
    public string? Remarks { get; set; }
}

public class CreateMaintenanceRecordRequest
{
    public string MaintenanceType { get; set; } = string.Empty;
    public DateTime MaintenanceDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Technician { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public decimal Hours { get; set; }
    public string? Remarks { get; set; }
}

public class DeviceRepairRequest
{
    public string? Description { get; set; }
    public string? FaultDescription { get; set; }
    public string? Priority { get; set; }
    public int? ProjectId { get; set; }
    public int? TicketTypeId { get; set; }
    public int? JobTypeId { get; set; }
}