using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Authorization;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 财务缴费API
/// </summary>
[ApiController]
[Authorize]
[Route("api/payment-records")]
public class PaymentsController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(MySqlConnection db, ILogger<PaymentsController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>
    /// 获取缴费记录列表（分页/过滤）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? paymentType,
        [FromQuery] int? residentId,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            conditions.Add("p.Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        if (!string.IsNullOrEmpty(paymentType) && paymentType != "all")
        {
            conditions.Add("p.PaymentType = @paymentType");
            parameters.Add(new MySqlParameter("@paymentType", paymentType));
        }

        if (residentId.HasValue)
        {
            conditions.Add("p.ResidentId = @residentId");
            parameters.Add(new MySqlParameter("@residentId", residentId.Value));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(p.PaymentNumber LIKE @keyword OR p.TransactionId LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM PaymentRecords p {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT p.*, r.Name as ResidentName, rm.RoomNumber, b.Name as BuildingName
            FROM PaymentRecords p
            LEFT JOIN Residents r ON p.ResidentId = r.Id
            LEFT JOIN Rooms rm ON p.RoomId = rm.Id
            LEFT JOIN Buildings b ON rm.BuildingId = b.Id
            {whereClause}
            ORDER BY p.CreatedAt DESC
            LIMIT @offset, @pageSize";

        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<PaymentResponse>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(MapToResponse(reader));
        }

        return Ok(new PaymentListResponse
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
            SELECT p.*, r.Name as ResidentName, rm.RoomNumber, b.Name as BuildingName
            FROM PaymentRecords p
            LEFT JOIN Residents r ON p.ResidentId = r.Id
            LEFT JOIN Rooms rm ON p.RoomId = rm.Id
            LEFT JOIN Buildings b ON rm.BuildingId = b.Id
            WHERE p.Id = @id";

        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Ok(new { Success = true, Data = MapToResponse(reader) });
        }
        return NotFound(new { Success = false, Message = "缴费记录不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest(new { Success = false, Message = "缴费金额必须大于0" });

        var paymentNumber = "PAY" + DateTime.UtcNow.ToString("yyyyMMdd") + new Random().Next(1000, 9999);

        var insertSql = @"INSERT INTO PaymentRecords
            (PaymentNumber, ResidentId, RoomId, PaymentType, Amount, PeriodStart, PeriodEnd, DueDate, PaidDate, Status, PaymentMethod, TransactionId, Remarks, CreatedAt)
            VALUES (@PaymentNumber, @ResidentId, @RoomId, @PaymentType, @Amount, @PeriodStart, @PeriodEnd, @DueDate, @PaidDate, @Status, @PaymentMethod, @TransactionId, @Remarks, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@PaymentNumber", paymentNumber);
        cmd.Parameters.AddWithValue("@ResidentId", (object)request.ResidentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RoomId", (object)request.RoomId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PaymentType", (object)request.PaymentType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Amount", request.Amount);
        cmd.Parameters.AddWithValue("@PeriodStart", (object)request.PeriodStart ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PeriodEnd", (object)request.PeriodEnd ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DueDate", (object)request.DueDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PaidDate", (object)request.PaidDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", request.Status ?? "unpaid");
        cmd.Parameters.AddWithValue("@PaymentMethod", (object)request.PaymentMethod ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TransactionId", (object)request.TransactionId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Remarks", (object)request.Remarks ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建缴费记录: {Id} - {PaymentNumber}", id, paymentNumber);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            Success = true,
            Message = "缴费记录创建成功",
            Data = new { Id = id, PaymentNumber = paymentNumber }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePaymentRequest request)
    {
        using (var checkCmd = new MySqlCommand("SELECT * FROM PaymentRecords WHERE Id = @id", _db))
        {
            checkCmd.Parameters.AddWithValue("@id", id);
            using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { Success = false, Message = "缴费记录不存在" });
        }

        var updates = new List<string> { "UpdatedAt = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        if (!string.IsNullOrEmpty(request.Status))
        {
            var validStatuses = new[] { "unpaid", "paid", "overdue", "cancelled" };
            if (!validStatuses.Contains(request.Status))
                return BadRequest(new { Success = false, Message = $"无效的状态: {request.Status}" });
            updates.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", request.Status));
        }

        if (!string.IsNullOrEmpty(request.PaymentMethod))
        {
            updates.Add("PaymentMethod = @paymentMethod");
            parameters.Add(new MySqlParameter("@paymentMethod", request.PaymentMethod));
        }

        if (!string.IsNullOrEmpty(request.TransactionId))
        {
            updates.Add("TransactionId = @transactionId");
            parameters.Add(new MySqlParameter("@transactionId", request.TransactionId));
        }

        if (request.PaidDate.HasValue)
        {
            updates.Add("PaidDate = @paidDate");
            parameters.Add(new MySqlParameter("@paidDate", request.PaidDate.Value));
            updates.Add("Status = 'paid'");
        }

        if (request.Amount.HasValue)
        {
            updates.Add("Amount = @amount");
            parameters.Add(new MySqlParameter("@amount", request.Amount.Value));
        }

        if (!string.IsNullOrEmpty(request.Remarks))
        {
            updates.Add("Remarks = @remarks");
            parameters.Add(new MySqlParameter("@remarks", request.Remarks));
        }

        var sql = $"UPDATE PaymentRecords SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新缴费记录: {Id}", id);
        return Ok(new { Success = true, Message = "缴费记录更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM PaymentRecords WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
            return NotFound(new { Success = false, Message = "缴费记录不存在" });

        _logger.LogInformation("删除缴费记录: {Id}", id);
        return Ok(new { Success = true, Message = "缴费记录已删除" });
    }

    private static PaymentResponse MapToResponse(MySqlDataReader reader)
    {
        return new PaymentResponse
        {
            Id = Convert.ToInt32(reader["Id"]),
            PaymentNumber = reader["PaymentNumber"].ToString() ?? "",
            ResidentId = reader["ResidentId"] == DBNull.Value ? null : Convert.ToInt32(reader["ResidentId"]),
            ResidentName = reader["ResidentName"] as string,
            RoomId = reader["RoomId"] == DBNull.Value ? null : Convert.ToInt32(reader["RoomId"]),
            RoomNumber = reader["RoomNumber"] as string,
            BuildingName = reader["BuildingName"] as string,
            PaymentType = reader["PaymentType"] as string,
            Amount = Convert.ToDecimal(reader["Amount"]),
            PeriodStart = reader["PeriodStart"] == DBNull.Value ? null : Convert.ToDateTime(reader["PeriodStart"]),
            PeriodEnd = reader["PeriodEnd"] == DBNull.Value ? null : Convert.ToDateTime(reader["PeriodEnd"]),
            DueDate = reader["DueDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["DueDate"]),
            PaidDate = reader["PaidDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["PaidDate"]),
            Status = reader["Status"].ToString() ?? "unpaid",
            PaymentMethod = reader["PaymentMethod"] as string,
            TransactionId = reader["TransactionId"] as string,
            Remarks = reader["Remarks"] as string,
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
            UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
        };
    }
}

public class PaymentResponse
{
    public int Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public int? ResidentId { get; set; }
    public string? ResidentName { get; set; }
    public int? RoomId { get; set; }
    public string? RoomNumber { get; set; }
    public string? BuildingName { get; set; }
    public string? PaymentType { get; set; }
    public decimal Amount { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public string Status { get; set; } = "unpaid";
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PaymentListResponse
{
    public bool Success { get; set; } = true;
    public List<PaymentResponse> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new PaginationInfo();
}

public class CreatePaymentRequest
{
    public int? ResidentId { get; set; }
    public int? RoomId { get; set; }
    public string? PaymentType { get; set; }
    public decimal Amount { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? Status { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public string? Remarks { get; set; }
}

public class UpdatePaymentRequest
{
    public string? Status { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? PaidDate { get; set; }
    public decimal? Amount { get; set; }
    public string? Remarks { get; set; }
}
