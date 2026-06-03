using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Microsoft.AspNetCore.Authorization;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 外卖订单管理API
/// </summary>
[ApiController]
[Authorize]
[Route("api/takeout-orders")]
public class TakeoutOrdersController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<TakeoutOrdersController> _logger;

    public TakeoutOrdersController(MySqlConnection db, ILogger<TakeoutOrdersController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? foodType = null,
        [FromQuery] string? keyword = null)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(status))
        {
            conditions.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        if (!string.IsNullOrEmpty(foodType))
        {
            conditions.Add("FoodType = @foodType");
            parameters.Add(new MySqlParameter("@foodType", foodType));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(OrderNo LIKE @keyword OR ResidentName LIKE @keyword OR RestaurantName LIKE @keyword OR Phone LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM TakeoutOrders {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT * FROM TakeoutOrders
            {whereClause}
            ORDER BY CreateTime DESC
            LIMIT @offset, @pageSize";

        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<TakeoutOrderItem>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapOrder(reader));

        return Ok(new
        {
            success = true,
            data = items,
            pagination = new { page, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        using var cmd = new MySqlCommand("SELECT * FROM TakeoutOrders WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return Ok(new { success = true, data = MapOrder(reader) });
        return NotFound(new { success = false, message = "订单不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TakeoutOrderItem req)
    {
        if (string.IsNullOrEmpty(req.RestaurantName))
            return BadRequest(new { success = false, message = "餐厅名称不能为空" });
        if (string.IsNullOrEmpty(req.ResidentName))
            return BadRequest(new { success = false, message = "住户姓名不能为空" });
        if (string.IsNullOrEmpty(req.Phone))
            return BadRequest(new { success = false, message = "联系电话不能为空" });

        var sql = @"INSERT INTO TakeoutOrders
            (OrderNo, RestaurantName, FoodType, ResidentName, RoomNo, Phone, DeliveryPerson, DeliveryPhone,
             DeliveryTime, Status, TotalAmount, Remark, CreateTime)
            VALUES
            (@OrderNo, @RestaurantName, @FoodType, @ResidentName, @RoomNo, @Phone, @DeliveryPerson, @DeliveryPhone,
             @DeliveryTime, @Status, @TotalAmount, @Remark, @CreateTime);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@OrderNo", req.OrderNo ?? "TK" + DateTime.Now.ToString("yyyyMMddHHmmss"));
        cmd.Parameters.AddWithValue("@RestaurantName", req.RestaurantName);
        cmd.Parameters.AddWithValue("@FoodType", req.FoodType ?? "快餐");
        cmd.Parameters.AddWithValue("@ResidentName", req.ResidentName);
        cmd.Parameters.AddWithValue("@RoomNo", (object)req.RoomNo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Phone", req.Phone);
        cmd.Parameters.AddWithValue("@DeliveryPerson", (object)req.DeliveryPerson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DeliveryPhone", (object)req.DeliveryPhone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DeliveryTime", (object)req.DeliveryTime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", req.Status ?? "待取餐");
        cmd.Parameters.AddWithValue("@TotalAmount", req.TotalAmount);
        cmd.Parameters.AddWithValue("@Remark", (object)req.Remark ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreateTime", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建外卖订单: {Id} - {OrderNo}", id, req.OrderNo);
        return CreatedAtAction(nameof(GetById), new { id }, new { success = true, message = "订单创建成功", data = new { id } });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] TakeoutOrderItem req)
    {
        using var checkCmd = new MySqlCommand("SELECT * FROM TakeoutOrders WHERE Id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        using var reader = await checkCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFound(new { success = false, message = "订单不存在" });
        reader.Close(); // 关闭 reader 才能执行下一个命令

        var updates = new List<string> { "UpdateTime = @updateTime" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id), new MySqlParameter("@updateTime", DateTime.UtcNow) };

        if (!string.IsNullOrEmpty(req.RestaurantName)) { updates.Add("RestaurantName = @restaurantName"); parameters.Add(new MySqlParameter("@restaurantName", req.RestaurantName)); }
        if (!string.IsNullOrEmpty(req.FoodType)) { updates.Add("FoodType = @foodType"); parameters.Add(new MySqlParameter("@foodType", req.FoodType)); }
        if (!string.IsNullOrEmpty(req.ResidentName)) { updates.Add("ResidentName = @residentName"); parameters.Add(new MySqlParameter("@residentName", req.ResidentName)); }
        if (!string.IsNullOrEmpty(req.RoomNo)) { updates.Add("RoomNo = @roomNo"); parameters.Add(new MySqlParameter("@roomNo", req.RoomNo)); }
        if (!string.IsNullOrEmpty(req.Phone)) { updates.Add("Phone = @phone"); parameters.Add(new MySqlParameter("@phone", req.Phone)); }
        if (!string.IsNullOrEmpty(req.DeliveryPerson)) { updates.Add("DeliveryPerson = @deliveryPerson"); parameters.Add(new MySqlParameter("@deliveryPerson", req.DeliveryPerson)); }
        if (!string.IsNullOrEmpty(req.DeliveryPhone)) { updates.Add("DeliveryPhone = @deliveryPhone"); parameters.Add(new MySqlParameter("@deliveryPhone", req.DeliveryPhone)); }
        if (!string.IsNullOrEmpty(req.DeliveryTime)) { updates.Add("DeliveryTime = @deliveryTime"); parameters.Add(new MySqlParameter("@deliveryTime", req.DeliveryTime)); }
        if (!string.IsNullOrEmpty(req.Status)) { updates.Add("Status = @status"); parameters.Add(new MySqlParameter("@status", req.Status)); }
        if (req.TotalAmount.HasValue) { updates.Add("TotalAmount = @totalAmount"); parameters.Add(new MySqlParameter("@totalAmount", req.TotalAmount.Value)); }
        if (req.Remark != null) { updates.Add("Remark = @remark"); parameters.Add(new MySqlParameter("@remark", req.Remark)); }

        var sql = $"UPDATE TakeoutOrders SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新外卖订单: {Id}", id);
        return Ok(new { success = true, message = "订单更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM TakeoutOrders WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { success = false, message = "订单不存在" });
        _logger.LogInformation("删除外卖订单: {Id}", id);
        return Ok(new { success = true, message = "订单已删除" });
    }

    private static TakeoutOrderItem MapOrder(MySqlDataReader r) => new()
    {
        Id = Convert.ToInt32(r["Id"]),
        OrderNo = r["OrderNo"].ToString() ?? "",
        RestaurantName = r["RestaurantName"].ToString() ?? "",
        FoodType = r["FoodType"].ToString() ?? "快餐",
        ResidentName = r["ResidentName"].ToString() ?? "",
        RoomNo = r["RoomNo"].ToString() ?? "",
        Phone = r["Phone"].ToString() ?? "",
        DeliveryPerson = r["DeliveryPerson"] as string,
        DeliveryPhone = r["DeliveryPhone"] as string,
        DeliveryTime = r["DeliveryTime"] == DBNull.Value ? null : Convert.ToDateTime(r["DeliveryTime"]).ToString("yyyy-MM-dd HH:mm:ss"),
        Status = r["Status"].ToString() ?? "待取餐",
        TotalAmount = r["TotalAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(r["TotalAmount"]),
        Remark = r["Remark"] as string,
        CreateTime = Convert.ToDateTime(r["CreateTime"]).ToString("yyyy-MM-dd HH:mm:ss"),
        UpdateTime = r["UpdateTime"] == DBNull.Value ? null : Convert.ToDateTime(r["UpdateTime"]).ToString("yyyy-MM-dd HH:mm:ss")
    };
}

public class TakeoutOrderItem
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = "";
    public string RestaurantName { get; set; } = "";
    public string FoodType { get; set; } = "快餐";
    public string ResidentName { get; set; } = "";
    public string RoomNo { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? DeliveryPerson { get; set; }
    public string? DeliveryPhone { get; set; }
    public string? DeliveryTime { get; set; }
    public string Status { get; set; } = "待取餐";
    public decimal? TotalAmount { get; set; }
    public string? Remark { get; set; }
    public string CreateTime { get; set; } = "";
    public string? UpdateTime { get; set; }
}