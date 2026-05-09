using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 合同管理API
/// </summary>
[ApiController]
[Route("api/contracts")]
public class ContractsController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<ContractsController> _logger;

    public ContractsController(MySqlConnection db, ILogger<ContractsController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>
    /// 获取合同列表（分页/过滤）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? contractType,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            conditions.Add("c.Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        if (!string.IsNullOrEmpty(contractType) && contractType != "all")
        {
            conditions.Add("c.ContractType = @contractType");
            parameters.Add(new MySqlParameter("@contractType", contractType));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(c.ContractName LIKE @keyword OR c.ContractNumber LIKE @keyword OR c.PartyA LIKE @keyword OR c.PartyB LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM Contracts c {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT c.*
            FROM Contracts c
            {whereClause}
            ORDER BY c.CreatedAt DESC
            LIMIT @offset, @pageSize";

        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<ContractResponse>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(MapToResponse(reader));
        }

        return Ok(new ContractListResponse
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
        var sql = "SELECT * FROM Contracts WHERE Id = @id";

        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Ok(new { Success = true, Data = MapToResponse(reader) });
        }
        return NotFound(new { Success = false, Message = "合同不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContractRequest request)
    {
        if (string.IsNullOrEmpty(request.ContractNumber) || string.IsNullOrEmpty(request.ContractName))
            return BadRequest(new { Success = false, Message = "合同编号和名称不能为空" });

        var insertSql = @"INSERT INTO Contracts
            (ContractNumber, ContractName, ContractType, PartyA, PartyB, SignedDate, StartDate, EndDate, Amount, Status, AttachmentUrl, Remarks, CreatedAt)
            VALUES (@ContractNumber, @ContractName, @ContractType, @PartyA, @PartyB, @SignedDate, @StartDate, @EndDate, @Amount, @Status, @AttachmentUrl, @Remarks, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@ContractNumber", request.ContractNumber);
        cmd.Parameters.AddWithValue("@ContractName", request.ContractName);
        cmd.Parameters.AddWithValue("@ContractType", (object)request.ContractType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PartyA", (object)request.PartyA ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PartyB", (object)request.PartyB ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SignedDate", (object)request.SignedDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@StartDate", (object)request.StartDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EndDate", (object)request.EndDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Amount", (object)request.Amount ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", request.Status ?? "draft");
        cmd.Parameters.AddWithValue("@AttachmentUrl", (object)request.AttachmentUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Remarks", (object)request.Remarks ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建合同: {Id} - {ContractNumber}", id, request.ContractNumber);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            Success = true,
            Message = "合同创建成功",
            Data = new { Id = id }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateContractRequest request)
    {
        using (var checkCmd = new MySqlCommand("SELECT * FROM Contracts WHERE Id = @id", _db))
        {
            checkCmd.Parameters.AddWithValue("@id", id);
            using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { Success = false, Message = "合同不存在" });
        }

        var updates = new List<string> { "UpdatedAt = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        if (!string.IsNullOrEmpty(request.Status))
        {
            var validStatuses = new[] { "draft", "active", "expired", "terminated" };
            if (!validStatuses.Contains(request.Status))
                return BadRequest(new { Success = false, Message = $"无效的状态: {request.Status}" });
            updates.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", request.Status));
        }

        if (!string.IsNullOrEmpty(request.ContractName))
        {
            updates.Add("ContractName = @contractName");
            parameters.Add(new MySqlParameter("@contractName", request.ContractName));
        }

        if (!string.IsNullOrEmpty(request.PartyA))
        {
            updates.Add("PartyA = @partyA");
            parameters.Add(new MySqlParameter("@partyA", request.PartyA));
        }

        if (!string.IsNullOrEmpty(request.PartyB))
        {
            updates.Add("PartyB = @partyB");
            parameters.Add(new MySqlParameter("@partyB", request.PartyB));
        }

        if (request.Amount.HasValue)
        {
            updates.Add("Amount = @amount");
            parameters.Add(new MySqlParameter("@amount", request.Amount.Value));
        }

        if (request.EndDate.HasValue)
        {
            updates.Add("EndDate = @endDate");
            parameters.Add(new MySqlParameter("@endDate", request.EndDate.Value));
        }

        if (!string.IsNullOrEmpty(request.AttachmentUrl))
        {
            updates.Add("AttachmentUrl = @attachmentUrl");
            parameters.Add(new MySqlParameter("@attachmentUrl", request.AttachmentUrl));
        }

        if (!string.IsNullOrEmpty(request.Remarks))
        {
            updates.Add("Remarks = @remarks");
            parameters.Add(new MySqlParameter("@remarks", request.Remarks));
        }

        var sql = $"UPDATE Contracts SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新合同: {Id}", id);
        return Ok(new { Success = true, Message = "合同更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM Contracts WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
            return NotFound(new { Success = false, Message = "合同不存在" });

        _logger.LogInformation("删除合同: {Id}", id);
        return Ok(new { Success = true, Message = "合同已删除" });
    }

    private static ContractResponse MapToResponse(MySqlDataReader reader)
    {
        return new ContractResponse
        {
            Id = Convert.ToInt32(reader["Id"]),
            ContractNumber = reader["ContractNumber"].ToString() ?? "",
            ContractName = reader["ContractName"].ToString() ?? "",
            ContractType = reader["ContractType"] as string,
            PartyA = reader["PartyA"] as string,
            PartyB = reader["PartyB"] as string,
            SignedDate = reader["SignedDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["SignedDate"]),
            StartDate = reader["StartDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["StartDate"]),
            EndDate = reader["EndDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["EndDate"]),
            Amount = reader["Amount"] == DBNull.Value ? null : Convert.ToDecimal(reader["Amount"]),
            Status = reader["Status"].ToString() ?? "draft",
            AttachmentUrl = reader["AttachmentUrl"] as string,
            Remarks = reader["Remarks"] as string,
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
            UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
        };
    }
}

public class ContractResponse
{
    public int Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string ContractName { get; set; } = string.Empty;
    public string? ContractType { get; set; }
    public string? PartyA { get; set; }
    public string? PartyB { get; set; }
    public DateTime? SignedDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Amount { get; set; }
    public string Status { get; set; } = "draft";
    public string? AttachmentUrl { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ContractListResponse
{
    public bool Success { get; set; } = true;
    public List<ContractResponse> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new PaginationInfo();
}

public class CreateContractRequest
{
    public string ContractNumber { get; set; } = string.Empty;
    public string ContractName { get; set; } = string.Empty;
    public string? ContractType { get; set; }
    public string? PartyA { get; set; }
    public string? PartyB { get; set; }
    public DateTime? SignedDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Amount { get; set; }
    public string? Status { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateContractRequest
{
    public string? Status { get; set; }
    public string? ContractName { get; set; }
    public string? PartyA { get; set; }
    public string? PartyB { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? EndDate { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? Remarks { get; set; }
}
