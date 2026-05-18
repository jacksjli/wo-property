using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.ContractService.Data;
using WO.Property.ContractService.Models;
using ContractStatusEnum = WO.Property.ContractService.Models.ContractStatus;

namespace WO.Property.ContractService.Controllers;

[ApiController]
[Route("api/tenant/contract")]
public class TenantContractController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ILogger<TenantContractController> _logger;

    public TenantContractController(IDbContextFactory<TenantDbContext> dbFactory, ILogger<TenantContractController> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    // GET /api/tenant/contract/contracts
    [HttpGet("contracts")]
    public async Task<IActionResult> GetContracts([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Contracts.AsQueryable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(c => c.Status.ToString() == status);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    id = c.Id,
                    contractNumber = c.ContractNumber,
                    title = c.Title,
                    type = c.Type.ToString(),
                    partyA = c.PartyA,
                    partyB = c.PartyB,
                    amount = c.Amount,
                    currency = c.Currency,
                    startDate = c.StartDate,
                    endDate = c.EndDate,
                    status = c.Status.ToString()
                })
                .ToListAsync();
            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetContracts failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/contract/contracts
    [HttpPost("contracts")]
    public async Task<IActionResult> CreateContract([FromBody] CreateContractRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var contractNum = $"C-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            var contract = new Contract
            {
                ContractNumber = contractNum,
                Title = request.Title,
                Type = Enum.Parse<ContractType>(request.Type ?? "Property"),
                PartyA = request.PartyA,
                PartyAContact = request.PartyAContact,
                PartyAPhone = request.PartyAPhone,
                PartyB = request.PartyB,
                PartyBContact = request.PartyBContact,
                PartyBPhone = request.PartyBPhone,
                Amount = request.Amount,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = ContractStatusEnum.Draft,
                CreatedAt = DateTime.UtcNow
            };
            db.Contracts.Add(contract);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "合同创建成功", data = new { contractNumber = contractNum } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateContract failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // PUT /api/tenant/contract/contracts/{id}/status
    [HttpPut("contracts/{id}/status")]
    public async Task<IActionResult> UpdateContractStatus(int id, [FromBody] UpdateContractStatusRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var contract = await db.Contracts.FindAsync(id);
            if (contract == null) return NotFound(new { success = false, message = "合同不存在" });
            if (!string.IsNullOrEmpty(request.Status))
                contract.Status = Enum.Parse<ContractStatusEnum>(request.Status);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "状态更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateContractStatus failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/contract/payments
    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            using var db = CreateDbContext();
            var total = await db.Payments.CountAsync();
            var items = await db.Payments
                .OrderByDescending(p => p.DueDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    id = p.Id,
                    contractId = p.ContractId,
                    paymentNumber = p.PaymentNumber,
                    description = p.Description,
                    amount = p.Amount,
                    currency = p.Currency,
                    dueDate = p.DueDate,
                    paidDate = p.PaidDate,
                    status = p.Status.ToString()
                })
                .ToListAsync();
            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPayments failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

public class CreateContractRequest
{
    public string Title { get; set; } = "";
    public string? Type { get; set; }
    public string PartyA { get; set; } = "";
    public string? PartyAContact { get; set; }
    public string? PartyAPhone { get; set; }
    public string PartyB { get; set; } = "";
    public string? PartyBContact { get; set; }
    public string? PartyBPhone { get; set; }
    public decimal Amount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class UpdateContractStatusRequest
{
    public string? Status { get; set; }
}