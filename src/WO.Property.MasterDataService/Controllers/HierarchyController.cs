using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 区域-楼栋-房号三级层级管理API
/// </summary>
[ApiController]
[Route("api/hierarchy")]
public class HierarchyController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<HierarchyController> _logger;

    public HierarchyController(MySqlConnection db, ILogger<HierarchyController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>
    /// 获取所有区域（不带楼栋，用于左侧列表）
    /// </summary>
    [HttpGet("areas")]
    public async Task<IActionResult> GetAreas()
    {
        var areas = new List<object>();
        var cmd = new MySqlCommand("SELECT * FROM Areas ORDER BY SortOrder, Id", _db);
        var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            areas.Add(new {
                id = reader.GetInt32("Id"),
                name = reader.GetString("Name"),
                code = reader.GetString("Code"),
                description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString("Description"),
                region = reader.IsDBNull(reader.GetOrdinal("Region")) ? "" : reader.GetString("Region"),
                status = reader.GetString("Status"),
                sortOrder = reader.GetInt32("SortOrder")
            });
        }
        await reader.CloseAsync();
        return Ok(new { success = true, data = areas });
    }

    /// <summary>
    /// 获取某个区域下的楼栋和房号（完整层级）
    /// </summary>
    [HttpGet("area-buildings")]
    public async Task<IActionResult> GetAreaBuildings([FromQuery] int? areaId = null)
    {
        if (!areaId.HasValue)
        {
            // 返回所有区域
            var areas = new List<object>();
            var cmd = new MySqlCommand("SELECT * FROM Areas ORDER BY SortOrder, Id", _db);
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                areas.Add(new {
                    id = reader.GetInt32("Id"),
                    name = reader.GetString("Name"),
                    code = reader.GetString("Code"),
                    description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString("Description"),
                    region = reader.IsDBNull(reader.GetOrdinal("Region")) ? "" : reader.GetString("Region"),
                    status = reader.GetString("Status"),
                    sortOrder = reader.GetInt32("SortOrder")
                });
            }
            await reader.CloseAsync();
            return Ok(new { success = true, data = areas });
        }

        // Step 1: 获取 area 信息
        var areaObj = new { id = 0, name = "", code = "", description = "", region = "", status = "", sortOrder = 0 };
        {
            var areaCmd = new MySqlCommand("SELECT * FROM Areas WHERE Id = @id", _db);
            areaCmd.Parameters.AddWithValue("@id", areaId.Value);
            var areaReader = await areaCmd.ExecuteReaderAsync();
            if (!await areaReader.ReadAsync())
            {
                await areaReader.CloseAsync();
                return Ok(new { success = false, message = "区域不存在" });
            }
            areaObj = new {
                id = areaReader.GetInt32("Id"),
                name = areaReader.GetString("Name"),
                code = areaReader.GetString("Code"),
                description = areaReader.IsDBNull(areaReader.GetOrdinal("Description")) ? "" : areaReader.GetString("Description"),
                region = areaReader.IsDBNull(areaReader.GetOrdinal("Region")) ? "" : areaReader.GetString("Region"),
                status = areaReader.GetString("Status"),
                sortOrder = areaReader.GetInt32("SortOrder")
            };
            await areaReader.CloseAsync();
        }

        // Step 2: 获取楼栋列表
        var buildingList = new List<object>();
        {
            var bldCmd = new MySqlCommand("SELECT * FROM Buildings WHERE Area = @area ORDER BY Id", _db);
            bldCmd.Parameters.AddWithValue("@area", areaObj.name);
            var bldReader = await bldCmd.ExecuteReaderAsync();
            while (await bldReader.ReadAsync())
            {
                buildingList.Add(new {
                    id = bldReader.GetInt32("Id"),
                    name = bldReader.IsDBNull(bldReader.GetOrdinal("Name")) ? "" : bldReader.GetString("Name"),
                    code = bldReader.IsDBNull(bldReader.GetOrdinal("Code")) ? "" : bldReader.GetString("Code"),
                    description = bldReader.IsDBNull(bldReader.GetOrdinal("Description")) ? "" : bldReader.GetString("Description"),
                    address = bldReader.IsDBNull(bldReader.GetOrdinal("Address")) ? "" : bldReader.GetString("Address"),
                    totalFloors = bldReader.GetInt32("TotalFloors"),
                    totalUnits = bldReader.GetInt32("TotalUnits"),
                    area = bldReader.IsDBNull(bldReader.GetOrdinal("Area")) ? "" : bldReader.GetString("Area"),
                    status = bldReader.GetString("Status"),
                    rooms = new List<object>()
                });
            }
            await bldReader.CloseAsync();
        }

        // Step 3: 获取每个楼栋的房号
        var buildings = new List<object>();
        foreach (var b in buildingList)
        {
            var bldId = (int)b.GetType().GetProperty("id")!.GetValue(b)!;
            var rooms = new List<object>();
            var roomCmd = new MySqlCommand("SELECT * FROM Rooms WHERE BuildingId = @buildingId ORDER BY RoomNumber", _db);
            roomCmd.Parameters.AddWithValue("@buildingId", bldId);
            var roomReader = await roomCmd.ExecuteReaderAsync();
            while (await roomReader.ReadAsync())
            {
                rooms.Add(new {
                    id = roomReader.GetInt32("Id"),
                    buildingId = roomReader.GetInt32("BuildingId"),
                    floor = roomReader.IsDBNull(roomReader.GetOrdinal("Floor")) ? "" : roomReader.GetString("Floor"),
                    unit = roomReader.IsDBNull(roomReader.GetOrdinal("Unit")) ? "" : roomReader.GetString("Unit"),
                    roomNumber = roomReader.GetString("RoomNumber"),
                    roomType = roomReader.IsDBNull(roomReader.GetOrdinal("RoomType")) ? "" : roomReader.GetString("RoomType"),
                    area = roomReader.IsDBNull(roomReader.GetOrdinal("Area")) ? (decimal?)null : roomReader.GetDecimal("Area"),
                    status = roomReader.GetString("Status")
                });
            }
            await roomReader.CloseAsync();

            var bName = (string)b.GetType().GetProperty("name")!.GetValue(b)!;
            var bCode = (string)b.GetType().GetProperty("code")!.GetValue(b)!;
            var bDesc = (string)b.GetType().GetProperty("description")!.GetValue(b)!;
            var bAddr = (string)b.GetType().GetProperty("address")!.GetValue(b)!;
            var bFloors = (int)b.GetType().GetProperty("totalFloors")!.GetValue(b)!;
            var bUnits = (int)b.GetType().GetProperty("totalUnits")!.GetValue(b)!;
            var bArea = (string)b.GetType().GetProperty("area")!.GetValue(b)!;
            var bStatus = (string)b.GetType().GetProperty("status")!.GetValue(b)!;
            buildings.Add(new {
                id = bldId,
                name = bName,
                code = bCode,
                description = bDesc,
                address = bAddr,
                totalFloors = bFloors,
                totalUnits = bUnits,
                area = bArea,
                status = bStatus,
                rooms
            });
        }

        return Ok(new { success = true, data = new { area = areaObj, buildings } });
    }

    /// <summary>
    /// 创建楼栋（属于某个Area）
    /// </summary>
    [HttpPost("buildings")]
    public async Task<IActionResult> CreateBuilding([FromBody] CreateBuildingRequest req)
    {
        if (string.IsNullOrEmpty(req.name) || string.IsNullOrEmpty(req.code))
            return Ok(new { success = false, message = "名称和编码不能为空" });

        object? areaName = null;
        {
            var areaCmd = new MySqlCommand("SELECT Name FROM Areas WHERE Id = @id", _db);
            areaCmd.Parameters.AddWithValue("@id", req.areaId);
            areaName = await areaCmd.ExecuteScalarAsync();
        }
        if (areaName == null)
            return Ok(new { success = false, message = "区域不存在" });

        var insertCmd = new MySqlCommand(@"
            INSERT INTO Buildings (Name, Code, Area, Description, Address, TotalFloors, TotalUnits, Status)
            VALUES (@name, @code, @area, @desc, @addr, @floors, @units, @status)", _db);
        insertCmd.Parameters.AddWithValue("@name", req.name);
        insertCmd.Parameters.AddWithValue("@code", req.code);
        insertCmd.Parameters.AddWithValue("@area", areaName);
        insertCmd.Parameters.AddWithValue("@desc", req.description ?? "");
        insertCmd.Parameters.AddWithValue("@addr", req.address ?? "");
        insertCmd.Parameters.AddWithValue("@floors", req.totalFloors);
        insertCmd.Parameters.AddWithValue("@units", req.totalUnits);
        insertCmd.Parameters.AddWithValue("@status", req.status ?? "Active");
        await insertCmd.ExecuteNonQueryAsync();

        return Ok(new { success = true, message = "楼栋创建成功" });
    }

    /// <summary>
    /// 创建房号（属于某个Building）
    /// </summary>
    [HttpPost("rooms")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest req)
    {
        if (!req.buildingId.HasValue || string.IsNullOrEmpty(req.roomNumber))
            return Ok(new { success = false, message = "楼栋和房号不能为空" });

        var insertCmd = new MySqlCommand(@"
            INSERT INTO Rooms (BuildingId, Floor, Unit, RoomNumber, RoomType, Area, Status)
            VALUES (@buildingId, @floor, @unit, @roomNumber, @roomType, @area, @status)", _db);
        insertCmd.Parameters.AddWithValue("@buildingId", req.buildingId.Value);
        insertCmd.Parameters.AddWithValue("@floor", req.floor ?? "");
        insertCmd.Parameters.AddWithValue("@unit", req.unit ?? "");
        insertCmd.Parameters.AddWithValue("@roomNumber", req.roomNumber);
        insertCmd.Parameters.AddWithValue("@roomType", req.roomType ?? "");
        insertCmd.Parameters.AddWithValue("@area", req.area ?? (object)DBNull.Value);
        insertCmd.Parameters.AddWithValue("@status", req.status ?? "Active");
        await insertCmd.ExecuteNonQueryAsync();

        return Ok(new { success = true, message = "房号创建成功" });
    }

    /// <summary>
    /// 删除楼栋（检查是否有房号）
    /// </summary>
    [HttpDelete("buildings/{id}")]
    public async Task<IActionResult> DeleteBuilding(int id)
    {
        object? count = null;
        {
            var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Rooms WHERE BuildingId = @id", _db);
            checkCmd.Parameters.AddWithValue("@id", id);
            count = await checkCmd.ExecuteScalarAsync();
        }
        if (Convert.ToInt32(count) > 0)
            return Ok(new { success = false, message = $"该楼栋下有 {count} 个房号，请先删除房号" });

        var delCmd = new MySqlCommand("DELETE FROM Buildings WHERE Id = @id", _db);
        delCmd.Parameters.AddWithValue("@id", id);
        await delCmd.ExecuteNonQueryAsync();
        return Ok(new { success = true, message = "楼栋删除成功" });
    }

    /// <summary>
    /// 删除房号（自动清除关联）
    /// </summary>
    [HttpDelete("rooms/{id}")]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        // 先清除所有关联表的 RoomId 引用（避免外键约束失败）
        var tables = new[] { "DeliveryRequests", "ExpressRecords", "PaymentRecords", 
            "RenovationRequests", "Residents", "Tickets", "Visitors" };
        
        foreach (var table in tables)
        {
            var clearCmd = new MySqlCommand($"UPDATE {table} SET RoomId = NULL WHERE RoomId = @id", _db);
            clearCmd.Parameters.AddWithValue("@id", id);
            await clearCmd.ExecuteNonQueryAsync();
        }

        // 删除房号
        var delCmd = new MySqlCommand("DELETE FROM Rooms WHERE Id = @id", _db);
        delCmd.Parameters.AddWithValue("@id", id);
        await delCmd.ExecuteNonQueryAsync();
        return Ok(new { success = true, message = "房号删除成功" });
    }

    /// <summary>
    /// 更新房号
    /// </summary>
    [HttpPut("rooms/{id}")]
    public async Task<IActionResult> UpdateRoom(int id, [FromBody] CreateRoomRequest req)
    {
        var checkCmd = new MySqlCommand("SELECT * FROM Rooms WHERE Id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        using var reader = await checkCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return Ok(new { success = false, message = "房号不存在" });
        reader.Close();

        var updates = new List<string> { "Floor = @floor", "Unit = @unit", "RoomNumber = @roomNumber", "RoomType = @roomType", "Area = @area", "Status = @status" };
        var cmd = new MySqlCommand($"UPDATE Rooms SET {string.Join(", ", updates)} WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@floor", req.floor ?? "");
        cmd.Parameters.AddWithValue("@unit", req.unit ?? "");
        cmd.Parameters.AddWithValue("@roomNumber", req.roomNumber);
        cmd.Parameters.AddWithValue("@roomType", req.roomType ?? "");
        cmd.Parameters.AddWithValue("@area", req.area ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@status", req.status ?? "Active");
        await cmd.ExecuteNonQueryAsync();

        return Ok(new { success = true, message = "房号更新成功" });
    }
}

public class CreateBuildingRequest
{
    public int areaId { get; set; }
    public string name { get; set; } = "";
    public string code { get; set; } = "";
    public string? description { get; set; }
    public string? address { get; set; }
    public int totalFloors { get; set; } = 1;
    public int totalUnits { get; set; } = 1;
    public string? status { get; set; }
}

public class CreateRoomRequest
{
    public int? buildingId { get; set; }
    public string? floor { get; set; }
    public string? unit { get; set; }
    public string roomNumber { get; set; } = "";
    public string? roomType { get; set; }
    public decimal? area { get; set; }
    public string? status { get; set; }
}