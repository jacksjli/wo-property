using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using WO.Property.MasterDataService.DTOs;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// 财务记录API（收支记录）
/// </summary>
[ApiController]
[Authorize]
[Route("api/finance-records")]
public class FinanceRecordsController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<FinanceRecordsController> _logger;

    public FinanceRecordsController(MySqlConnection db, ILogger<FinanceRecordsController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>
    /// 获取财务记录列表（分页/过滤）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? type,
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] string? keyword,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(type) && type != "all")
        {
            conditions.Add("Type = @type");
            parameters.Add(new MySqlParameter("@type", type));
        }

        if (!string.IsNullOrEmpty(category) && category != "all")
        {
            conditions.Add("Category = @category");
            parameters.Add(new MySqlParameter("@category", category));
        }

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            conditions.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(RecordNumber LIKE @keyword OR Description LIKE @keyword OR RelatedParty LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        if (!string.IsNullOrEmpty(startDate))
        {
            conditions.Add("RecordDate >= @startDate");
            parameters.Add(new MySqlParameter("@startDate", startDate));
        }

        if (!string.IsNullOrEmpty(endDate))
        {
            conditions.Add("RecordDate <= @endDate");
            parameters.Add(new MySqlParameter("@endDate", endDate));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM FinanceRecords {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT * FROM FinanceRecords
            {whereClause}
            ORDER BY RecordDate DESC, CreatedAt DESC
            LIMIT @offset, @pageSize";

        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<FinanceRecordResponse>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(MapToResponse(reader));
        }

        return Ok(new FinanceRecordListResponse
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
        var sql = "SELECT * FROM FinanceRecords WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Ok(new { Success = true, Data = MapToResponse(reader) });
        }
        return NotFound(new { Success = false, Message = "财务记录不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFinanceRecordRequest request)
    {
        if (string.IsNullOrEmpty(request.RecordNumber))
            return BadRequest(new { Success = false, Message = "记录编号不能为空" });
        if (request.Amount <= 0)
            return BadRequest(new { Success = false, Message = "金额必须大于0" });

        var insertSql = @"INSERT INTO FinanceRecords
            (RecordNumber, Type, Category, Amount, Balance, PaymentMethod, RecordDate, Handler, RelatedParty, ContractNo, BillNo, Description, ReceiptNo, Status, Remarks, CreatedAt)
            VALUES (@RecordNumber, @Type, @Category, @Amount, @Balance, @PaymentMethod, @RecordDate, @Handler, @RelatedParty, @ContractNo, @BillNo, @Description, @ReceiptNo, @Status, @Remarks, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@RecordNumber", request.RecordNumber);
        cmd.Parameters.AddWithValue("@Type", request.Type);
        cmd.Parameters.AddWithValue("@Category", (object)request.Category ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Amount", request.Amount);
        cmd.Parameters.AddWithValue("@Balance", (object)request.Balance ?? 0);
        cmd.Parameters.AddWithValue("@PaymentMethod", (object)request.PaymentMethod ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RecordDate", (object)request.RecordDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Handler", (object)request.Handler ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RelatedParty", (object)request.RelatedParty ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ContractNo", (object)request.ContractNo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@BillNo", (object)request.BillNo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Description", (object)request.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ReceiptNo", (object)request.ReceiptNo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", request.Status ?? "completed");
        cmd.Parameters.AddWithValue("@Remarks", (object)request.Remarks ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建财务记录: {Id} - {RecordNumber}", id, request.RecordNumber);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            Success = true,
            Message = "财务记录创建成功",
            Data = new { Id = id, RecordNumber = request.RecordNumber }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFinanceRecordRequest request)
    {
        using (var checkCmd = new MySqlCommand("SELECT * FROM FinanceRecords WHERE Id = @id", _db))
        {
            checkCmd.Parameters.AddWithValue("@id", id);
            using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { Success = false, Message = "财务记录不存在" });
        }

        var updates = new List<string> { "UpdatedAt = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        if (!string.IsNullOrEmpty(request.RecordNumber))
        {
            updates.Add("RecordNumber = @recordNumber");
            parameters.Add(new MySqlParameter("@recordNumber", request.RecordNumber));
        }

        if (!string.IsNullOrEmpty(request.Type))
        {
            updates.Add("Type = @type");
            parameters.Add(new MySqlParameter("@type", request.Type));
        }

        if (!string.IsNullOrEmpty(request.Category))
        {
            updates.Add("Category = @category");
            parameters.Add(new MySqlParameter("@category", request.Category));
        }

        if (request.Amount.HasValue)
        {
            updates.Add("Amount = @amount");
            parameters.Add(new MySqlParameter("@amount", request.Amount.Value));
        }

        if (request.Balance.HasValue)
        {
            updates.Add("Balance = @balance");
            parameters.Add(new MySqlParameter("@balance", request.Balance.Value));
        }

        if (!string.IsNullOrEmpty(request.PaymentMethod))
        {
            updates.Add("PaymentMethod = @paymentMethod");
            parameters.Add(new MySqlParameter("@paymentMethod", request.PaymentMethod));
        }

        if (!string.IsNullOrEmpty(request.RecordDate))
        {
            updates.Add("RecordDate = @recordDate");
            parameters.Add(new MySqlParameter("@recordDate", request.RecordDate));
        }

        if (!string.IsNullOrEmpty(request.Handler))
        {
            updates.Add("Handler = @handler");
            parameters.Add(new MySqlParameter("@handler", request.Handler));
        }

        if (!string.IsNullOrEmpty(request.RelatedParty))
        {
            updates.Add("RelatedParty = @relatedParty");
            parameters.Add(new MySqlParameter("@relatedParty", request.RelatedParty));
        }

        if (!string.IsNullOrEmpty(request.ContractNo))
        {
            updates.Add("ContractNo = @contractNo");
            parameters.Add(new MySqlParameter("@contractNo", request.ContractNo));
        }

        if (!string.IsNullOrEmpty(request.BillNo))
        {
            updates.Add("BillNo = @billNo");
            parameters.Add(new MySqlParameter("@billNo", request.BillNo));
        }

        if (!string.IsNullOrEmpty(request.Description))
        {
            updates.Add("Description = @description");
            parameters.Add(new MySqlParameter("@description", request.Description));
        }

        if (!string.IsNullOrEmpty(request.ReceiptNo))
        {
            updates.Add("ReceiptNo = @receiptNo");
            parameters.Add(new MySqlParameter("@receiptNo", request.ReceiptNo));
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            updates.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", request.Status));
        }

        if (!string.IsNullOrEmpty(request.Remarks))
        {
            updates.Add("Remarks = @remarks");
            parameters.Add(new MySqlParameter("@remarks", request.Remarks));
        }

        var sql = $"UPDATE FinanceRecords SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新财务记录: {Id}", id);
        return Ok(new { Success = true, Message = "财务记录更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM FinanceRecords WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
            return NotFound(new { Success = false, Message = "财务记录不存在" });

        _logger.LogInformation("删除财务记录: {Id}", id);
        return Ok(new { Success = true, Message = "财务记录已删除" });
    }

    private static FinanceRecordResponse MapToResponse(MySqlDataReader reader)
    {
        return new FinanceRecordResponse
        {
            Id = Convert.ToInt32(reader["Id"]),
            RecordNumber = reader["RecordNumber"].ToString() ?? "",
            Type = reader["Type"].ToString() ?? "",
            Category = reader["Category"] as string,
            Amount = Convert.ToDecimal(reader["Amount"]),
            Balance = reader["Balance"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Balance"]),
            PaymentMethod = reader["PaymentMethod"] as string,
            RecordDate = reader["RecordDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["RecordDate"]).ToString("yyyy-MM-dd"),
            Handler = reader["Handler"] as string,
            RelatedParty = reader["RelatedParty"] as string,
            ContractNo = reader["ContractNo"] as string,
            BillNo = reader["BillNo"] as string,
            Description = reader["Description"] as string,
            ReceiptNo = reader["ReceiptNo"] as string,
            Status = reader["Status"].ToString() ?? "completed",
            Remarks = reader["Remarks"] as string,
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
            UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
        };
    }
}

public class FinanceRecordResponse
{
    public int Id { get; set; }
    public string RecordNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal Amount { get; set; }
    public decimal Balance { get; set; }
    public string? PaymentMethod { get; set; }
    public string? RecordDate { get; set; }
    public string? Handler { get; set; }
    public string? RelatedParty { get; set; }
    public string? ContractNo { get; set; }
    public string? BillNo { get; set; }
    public string? Description { get; set; }
    public string? ReceiptNo { get; set; }
    public string Status { get; set; } = "completed";
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class FinanceRecordListResponse
{
    public bool Success { get; set; } = true;
    public List<FinanceRecordResponse> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new PaginationInfo();
}

public class CreateFinanceRecordRequest
{
    public string RecordNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal Amount { get; set; }
    public decimal? Balance { get; set; }
    public string? PaymentMethod { get; set; }
    public string? RecordDate { get; set; }
    public string? Handler { get; set; }
    public string? RelatedParty { get; set; }
    public string? ContractNo { get; set; }
    public string? BillNo { get; set; }
    public string? Description { get; set; }
    public string? ReceiptNo { get; set; }
    public string? Status { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateFinanceRecordRequest
{
    public string? RecordNumber { get; set; }
    public string? Type { get; set; }
    public string? Category { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Balance { get; set; }
    public string? PaymentMethod { get; set; }
    public string? RecordDate { get; set; }
    public string? Handler { get; set; }
    public string? RelatedParty { get; set; }
    public string? ContractNo { get; set; }
    public string? BillNo { get; set; }
    public string? Description { get; set; }
    public string? ReceiptNo { get; set; }
    public string? Status { get; set; }
    public string? Remarks { get; set; }
}
