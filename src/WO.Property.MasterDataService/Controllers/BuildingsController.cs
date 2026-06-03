using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using WO.Property.MasterDataService.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 楼栋管理API
/// </summary>
[ApiController]
[Authorize]
[Route("api/buildings")]
public class BuildingsController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<BuildingsController> _logger;

    public BuildingsController(MySqlConnection db, ILogger<BuildingsController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>获取楼栋列表（分页）</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        var where = string.IsNullOrEmpty(status) ? "" : "WHERE Status = @status";
        var countSql = $"SELECT COUNT(*) FROM Buildings {where}";
        using var countCmd = new MySqlCommand(countSql, _db);
        if (!string.IsNullOrEmpty(status)) countCmd.Parameters.AddWithValue("@status", status);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $"SELECT * FROM Buildings {where} ORDER BY Id LIMIT @offset, @pageSize";
        using var dataCmd = new MySqlCommand(dataSql, _db);
        if (!string.IsNullOrEmpty(status)) dataCmd.Parameters.AddWithValue("@status", status);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<BuildingItem>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapBuilding(reader));

        return Ok(new
        {
            success = true,
            data = items,
            pagination = new { page, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    /// <summary>获取单个楼栋</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        using var cmd = new MySqlCommand("SELECT * FROM Buildings WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return Ok(new { success = true, data = MapBuilding(reader) });
        return NotFound(new { success = false, message = "楼栋不存在" });
    }

    /// <summary>新增楼栋</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BuildingItem req)
    {
        using var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Buildings WHERE Code = @code", _db);
        checkCmd.Parameters.AddWithValue("@code", req.Code);
        if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0)
            return BadRequest(new { success = false, message = $"楼栋编号 '{req.Code}' 已存在" });

        var sql = @"INSERT INTO Buildings (Name, Code, Description, Address, TotalFloors, TotalUnits, Status)
                    VALUES (@Name, @Code, @Description, @Address, @TotalFloors, @TotalUnits, @Status);
                    SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@Name", req.Name);
        cmd.Parameters.AddWithValue("@Code", req.Code);
        cmd.Parameters.AddWithValue("@Description", (object)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Address", (object)req.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TotalFloors", req.TotalFloors ?? 1);
        cmd.Parameters.AddWithValue("@TotalUnits", req.TotalUnits ?? 1);
        cmd.Parameters.AddWithValue("@Status", req.Status ?? "Active");
        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        _logger.LogInformation("创建楼栋: {Code} ({Name})", req.Code, req.Name);
        return CreatedAtAction(nameof(GetById), new { id }, new { success = true, message = "楼栋创建成功", data = new { id } });
    }

    /// <summary>更新楼栋</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] BuildingItem req)
    {
        using var checkCmd = new MySqlCommand("SELECT * FROM Buildings WHERE Id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        using var reader = await checkCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFound(new { success = false, message = "楼栋不存在" });
        reader.Close(); // 关闭 reader 才能执行下一个命令

        var updates = new List<string> { "Area = @area", "Name = @name", "Code = @code", "Description = @description", "Address = @address", "TotalFloors = @totalFloors", "TotalUnits = @totalUnits", "Status = @status", "UpdatedAt = @updatedAt" };
        using var cmd = new MySqlCommand($"UPDATE Buildings SET {string.Join(", ", updates)} WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@area", (object)req.Area ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@name", req.Name);
        cmd.Parameters.AddWithValue("@code", req.Code);
        cmd.Parameters.AddWithValue("@description", (object)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@address", (object)req.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@totalFloors", req.TotalFloors ?? 1);
        cmd.Parameters.AddWithValue("@totalUnits", req.TotalUnits ?? 1);
        cmd.Parameters.AddWithValue("@status", req.Status ?? "Active");
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新楼栋: {Id}", id);
        return Ok(new { success = true, message = "楼栋更新成功" });
    }

    /// <summary>批量导入楼栋</summary>
    [HttpPost("import")]
    public async Task<IActionResult> ImportBuildings([FromBody] ImportBuildingsRequest req)
    {
        if (req.Rows == null || req.Rows.Count == 0)
            return BadRequest(new { success = false, message = "没有数据" });

        var successCount = 0;
        var failedCount = 0;
        var skippedCount = 0;
        var errors = new List<string>();

        foreach (var row in req.Rows)
        {
            try
            {
                // 必填字段校验
                if (string.IsNullOrWhiteSpace(row.Name))
                {
                    skippedCount++;
                    errors.Add($"行 Skip: 楼栋名称为空");
                    continue;
                }

                // 生成编码（名称转拼音首字母）
                var code = toPinyinCode(row.Name);
                if (string.IsNullOrWhiteSpace(code)) code = Guid.NewGuid().ToString("N")[..8];

                // 检查是否已存在（按名称查重，名称相同则覆盖）
                using var checkCmd = new MySqlCommand("SELECT Id FROM Buildings WHERE Name = @name", _db);
                checkCmd.Parameters.AddWithValue("@name", row.Name);
                var existsId = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                if (existsId > 0)
                {
                    // 更新
                    var sql = @"UPDATE Buildings SET Area = @area, TotalFloors = @totalFloors,
                               TotalUnits = @totalUnits, Description = @description,
                               UpdatedAt = @updatedAt WHERE Id = @id";
                    using var updateCmd = new MySqlCommand(sql, _db);
                    updateCmd.Parameters.AddWithValue("@id", existsId);
                    updateCmd.Parameters.AddWithValue("@area", (object)row.Area ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@totalFloors", row.TotalFloors ?? 1);
                    updateCmd.Parameters.AddWithValue("@totalUnits", row.TotalUnits ?? 1);
                    updateCmd.Parameters.AddWithValue("@description", (object)row.Description ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // 新增
                    var sql = @"INSERT INTO Buildings (Name, Code, Area, TotalFloors, TotalUnits, Description, Status)
                               VALUES (@name, @code, @area, @totalFloors, @totalUnits, @description, 'Active');
                               SELECT LAST_INSERT_ID();";
                    using var insertCmd = new MySqlCommand(sql, _db);
                    insertCmd.Parameters.AddWithValue("@name", row.Name);
                    insertCmd.Parameters.AddWithValue("@code", code);
                    insertCmd.Parameters.AddWithValue("@area", (object)row.Area ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@totalFloors", row.TotalFloors ?? 1);
                    insertCmd.Parameters.AddWithValue("@totalUnits", row.TotalUnits ?? 1);
                    insertCmd.Parameters.AddWithValue("@description", (object)row.Description ?? DBNull.Value);
                    await insertCmd.ExecuteScalarAsync();
                }
                successCount++;
            }
            catch (Exception ex)
            {
                failedCount++;
                errors.Add($"行 Error: {row.Name ?? "(空)"} - {ex.Message}");
            }
        }

        _logger.LogInformation("批量导入楼栋完成: 成功{SuccessCount}, 失败{FailedCount}, 跳过{SkippedCount}", successCount, failedCount, skippedCount);
        return Ok(new ImportBuildingsResponse
        {
            Success = failedCount == 0,
            SuccessCount = successCount,
            FailedCount = failedCount,
            SkippedCount = skippedCount,
            Errors = errors
        });
    }

    private static string toPinyinCode(string name)
    {
        // 简单实现：取每个汉字拼音首字母，非汉字保留原字符
        // 这里用简化的方式，只取第一音节的首字母
        if (string.IsNullOrEmpty(name)) return "";
        var sb = new System.Text.StringBuilder();
        foreach (var c in name)
        {
            if (c >= '0' && c <= '9') sb.Append(c);
            else if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z') sb.Append(char.ToUpper(c));
            else if (c >= 0x4e00 && c <= 0x9fff)
            {
                // 常见汉字拼音首字母映射（简化版）
                var py = chinesePinyinMap.GetValueOrDefault(c, c.ToString());
                if (!string.IsNullOrEmpty(py)) sb.Append(py[0]);
            }
        }
        return sb.ToString();
    }

    private static readonly Dictionary<char, string> chinesePinyinMap = new()
    {
        {'一',"yi"},{'二',"er"},{'三',"san"},{'四',"si"},{'五',"wu"},{'六',"liu"},
        {'七',"qi"},{'八',"ba"},{'九',"jiu"},{'十',"shi"},
        {'亚',"ya"},{'奥',"ao"},{'北',"bei"},{'碧',"bi"},{'滨',"bin"},
        {'彩',"cai"},{'翠',"cui"},{'大',"da"},{'德',"de"},{'东',"dong"},
        {'福',"fu"},{'港',"gang"},{'光',"guang"},{'桂',"gui"},{'国',"guo"},
        {'海',"hai"},{'花',"hua"},{'华',"hua"},{'黄',"huang"},
        {'佳',"jia"},{'金',"jin"},{'锦',"jin"},{'京',"jing"},{'景',"jing"},
        {'康',"kang"},{'科',"ke"},{'兰',"lan"},{'蓝',"lan"},{'朗',"lang"},
        {'里',"li"},{'丽',"li"},{'连',"lian"},{'龙',"long"},{'绿',"lv"},
        {'梅',"mei"},{'美',"mei"},{'南',"nan"},{'宁',"ning"},
        {'平',"ping"},{'栖',"qi"},{'前',"qian"},{'青',"qing"},{'清',"qing"},
        {'仁',"ren"},{'瑞',"rui"},{'山',"shan"},{'上',"shang"},{'盛',"sheng"},
        {'世',"shi"},{'树',"shu"},{'松',"song"},{'苏',"su"},
        {'泰',"tai"},{'天',"tian"},{'通',"tong"},{'万',"wan"},
        {'文',"wen"},{'西',"xi"},{'厦',"xia"},{'新',"xin"},{'星',"xing"},
        {'学',"xue"},{'雅',"ya"},{'阳',"yang"},{'银',"yin"},{'英',"ying"},
        {'友',"you"},{'园',"yuan"},{'月',"yue"},{'悦',"yue"},
        {'在',"zai"},{'振',"zhen"},{'中',"zhong"},{'紫',"zi"},
    };

    /// <summary>删除楼栋（自动清除关联）</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // 先删除 Rooms（BuildingId 为 NOT NULL，需先删除关联记录）
        var delRoomsCmd = new MySqlCommand("DELETE FROM Rooms WHERE BuildingId = @id", _db);
        delRoomsCmd.Parameters.AddWithValue("@id", id);
        await delRoomsCmd.ExecuteNonQueryAsync();

        // 删除 CleaningRecords（BuildingId 为 NOT NULL）
        var delCleaningCmd = new MySqlCommand("DELETE FROM CleaningRecords WHERE BuildingId = @id", _db);
        delCleaningCmd.Parameters.AddWithValue("@id", id);
        await delCleaningCmd.ExecuteNonQueryAsync();

        // 清除其他关联表的 BuildingId 引用（这些列允许 NULL）
        var nullableTables = new[] { "Devices", "InspectionRecords", 
            "ParkingRecords", "Residents", "Tickets", "Visitors" };
        
        foreach (var table in nullableTables)
        {
            var clearCmd = new MySqlCommand($"UPDATE {table} SET BuildingId = NULL WHERE BuildingId = @id", _db);
            clearCmd.Parameters.AddWithValue("@id", id);
            await clearCmd.ExecuteNonQueryAsync();
        }

        // 删除楼栋
        using var cmd = new MySqlCommand("DELETE FROM Buildings WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { success = false, message = "楼栋不存在" });
        _logger.LogInformation("删除楼栋: {Id}", id);
        return Ok(new { success = true, message = "楼栋已删除" });
    }

    private static BuildingItem MapBuilding(MySqlDataReader r) => new()
    {
        Id = Convert.ToInt32(r["Id"]),
        Name = r["Name"].ToString() ?? "",
        Code = r["Code"].ToString() ?? "",
        Area = r["Area"] as string ?? "",
        Description = r["Description"] as string,
        Address = r["Address"] as string,
        TotalFloors = r["TotalFloors"] == DBNull.Value ? null : Convert.ToInt32(r["TotalFloors"]),
        TotalUnits = r["TotalUnits"] == DBNull.Value ? null : Convert.ToInt32(r["TotalUnits"]),
        Status = r["Status"].ToString() ?? "Active",
        CreatedAt = Convert.ToDateTime(r["CreatedAt"]),
        UpdatedAt = r["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(r["UpdatedAt"])
    };
}

public class BuildingItem
{
    public int Id { get; set; }
    public string? Area { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Description { get; set; }
    public string? Address { get; set; }
    public int? TotalFloors { get; set; }
    public int? TotalUnits { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}