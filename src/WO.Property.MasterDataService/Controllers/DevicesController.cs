using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Authorization;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 设备管理API
/// </summary>
[ApiController]
[Authorize]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(MySqlConnection db, ILogger<DevicesController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>
    /// 获取设备列表（分页/过滤）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? deviceType,
        [FromQuery] int? buildingId,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            conditions.Add("d.Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        if (!string.IsNullOrEmpty(deviceType) && deviceType != "all")
        {
            conditions.Add("d.DeviceTypeId = @deviceTypeId");
            parameters.Add(new MySqlParameter("@deviceTypeId", int.Parse(deviceType)));
        }

        if (buildingId.HasValue)
        {
            conditions.Add("d.BuildingId = @buildingId");
            parameters.Add(new MySqlParameter("@buildingId", buildingId.Value));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(d.DeviceName LIKE @keyword OR d.DeviceCode LIKE @keyword OR d.Location LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM Devices d {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT d.*, dt.Name as DeviceTypeName, b.Name as BuildingName, s.Name as SupplierName
            FROM Devices d
            LEFT JOIN DeviceTypes dt ON d.DeviceTypeId = dt.Id
            LEFT JOIN Buildings b ON d.BuildingId = b.Id
            LEFT JOIN Suppliers s ON d.SupplierId = s.Id
            {whereClause}
            ORDER BY d.CreatedAt DESC
            LIMIT @offset, @pageSize";

        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<DeviceResponse>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(MapToResponse(reader));
        }

        return Ok(new DeviceListResponse
        {
            Success = true,
            Data = items,
            Pagination = new PaginationInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var sql = @"
            SELECT d.*, dt.Name as DeviceTypeName, b.Name as BuildingName, s.Name as SupplierName
            FROM Devices d
            LEFT JOIN DeviceTypes dt ON d.DeviceTypeId = dt.Id
            LEFT JOIN Buildings b ON d.BuildingId = b.Id
            LEFT JOIN Suppliers s ON d.SupplierId = s.Id
            WHERE d.Id = @id";

        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Ok(new { Success = true, Data = MapToResponse(reader) });
        }
        return NotFound(new { Success = false, Message = "设备不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeviceRequest request)
    {
        if (string.IsNullOrEmpty(request.DeviceCode) || string.IsNullOrEmpty(request.DeviceName))
            return BadRequest(new { Success = false, Message = "设备编号和名称不能为空" });

        var insertSql = @"INSERT INTO Devices
            (DeviceCode, DeviceName, DeviceTypeId, BuildingId, Floor, Location, Status, LastMaintenanceDate, NextMaintenanceDate, PurchaseDate, SupplierId, Remarks, CreatedAt)
            VALUES (@DeviceCode, @DeviceName, @DeviceTypeId, @BuildingId, @Floor, @Location, @Status, @LastMaintenanceDate, @NextMaintenanceDate, @PurchaseDate, @SupplierId, @Remarks, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@DeviceCode", request.DeviceCode);
        cmd.Parameters.AddWithValue("@DeviceName", request.DeviceName);
        cmd.Parameters.AddWithValue("@DeviceTypeId", (object)request.DeviceTypeId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@BuildingId", (object)request.BuildingId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Floor", (object)request.Floor ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Location", (object)request.Location ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", request.Status ?? "normal");
        cmd.Parameters.AddWithValue("@LastMaintenanceDate", (object)request.LastMaintenanceDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@NextMaintenanceDate", (object)request.NextMaintenanceDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PurchaseDate", (object)request.PurchaseDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SupplierId", (object)request.SupplierId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Remarks", (object)request.Remarks ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建设备: {Id} - {DeviceCode}", id, request.DeviceCode);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            Success = true,
            Message = "设备创建成功",
            Data = new { Id = id }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDeviceRequest request)
    {
        using (var checkCmd = new MySqlCommand("SELECT * FROM Devices WHERE Id = @id", _db))
        {
            checkCmd.Parameters.AddWithValue("@id", id);
            using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { Success = false, Message = "设备不存在" });
        }

        var updates = new List<string> { "UpdatedAt = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        if (!string.IsNullOrEmpty(request.Status))
        {
            var validStatuses = new[] { "normal", "maintenance", "broken", "deprecated" };
            if (!validStatuses.Contains(request.Status))
                return BadRequest(new { Success = false, Message = $"无效的状态: {request.Status}" });
            updates.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", request.Status));
        }

        if (!string.IsNullOrEmpty(request.DeviceName))
        {
            updates.Add("DeviceName = @deviceName");
            parameters.Add(new MySqlParameter("@deviceName", request.DeviceName));
        }

        if (!string.IsNullOrEmpty(request.Location))
        {
            updates.Add("Location = @location");
            parameters.Add(new MySqlParameter("@location", request.Location));
        }

        if (request.Floor.HasValue)
        {
            updates.Add("Floor = @floor");
            parameters.Add(new MySqlParameter("@floor", request.Floor.Value));
        }

        if (request.NextMaintenanceDate.HasValue)
        {
            updates.Add("NextMaintenanceDate = @nextMaintenanceDate");
            parameters.Add(new MySqlParameter("@nextMaintenanceDate", request.NextMaintenanceDate.Value));
        }

        if (!string.IsNullOrEmpty(request.Remarks))
        {
            updates.Add("Remarks = @remarks");
            parameters.Add(new MySqlParameter("@remarks", request.Remarks));
        }

        var sql = $"UPDATE Devices SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新设备: {Id}", id);
        return Ok(new { Success = true, Message = "设备更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM Devices WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
            return NotFound(new { Success = false, Message = "设备不存在" });

        _logger.LogInformation("删除设备: {Id}", id);
        return Ok(new { Success = true, Message = "设备已删除" });
    }

    private static DeviceResponse MapToResponse(MySqlDataReader reader)
    {
        return new DeviceResponse
        {
            Id = Convert.ToInt32(reader["Id"]),
            DeviceCode = reader["DeviceCode"].ToString() ?? "",
            DeviceName = reader["DeviceName"].ToString() ?? "",
            DeviceTypeId = reader["DeviceTypeId"] == DBNull.Value ? null : Convert.ToInt32(reader["DeviceTypeId"]),
            DeviceTypeName = reader["DeviceTypeName"] as string,
            BuildingId = reader["BuildingId"] == DBNull.Value ? null : Convert.ToInt32(reader["BuildingId"]),
            BuildingName = reader["BuildingName"] as string,
            Floor = reader["Floor"] == DBNull.Value ? null : Convert.ToInt32(reader["Floor"]),
            Location = reader["Location"] as string,
            Status = reader["Status"].ToString() ?? "normal",
            LastMaintenanceDate = reader["LastMaintenanceDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["LastMaintenanceDate"]),
            NextMaintenanceDate = reader["NextMaintenanceDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["NextMaintenanceDate"]),
            PurchaseDate = reader["PurchaseDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["PurchaseDate"]),
            SupplierId = reader["SupplierId"] == DBNull.Value ? null : Convert.ToInt32(reader["SupplierId"]),
            SupplierName = reader["SupplierName"] as string,
            Remarks = reader["Remarks"] as string,
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
            UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
        };
    }
}

public class DeviceResponse
{
    public int Id { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public int? DeviceTypeId { get; set; }
    public string? DeviceTypeName { get; set; }
    public int? BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public int? Floor { get; set; }
    public string? Location { get; set; }
    public string Status { get; set; } = "normal";
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class DeviceListResponse
{
    public bool Success { get; set; } = true;
    public List<DeviceResponse> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new PaginationInfo();
}

public class CreateDeviceRequest
{
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public int? DeviceTypeId { get; set; }
    public int? BuildingId { get; set; }
    public int? Floor { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public int? SupplierId { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateDeviceRequest
{
    public string? Status { get; set; }
    public string? DeviceName { get; set; }
    public int? Floor { get; set; }
    public string? Location { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public string? Remarks { get; set; }
}
