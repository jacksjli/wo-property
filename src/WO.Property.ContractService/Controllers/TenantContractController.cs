using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WO.Property.ContractService.Data;
using WO.Property.ContractService.Models;

namespace WO.Property.ContractService.Controllers;

[ApiController]
[Authorize]
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

    // GET /api/tenant/contract/contracts
    [HttpGet("contracts")]
    public async Task<IActionResult> GetContracts([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Contracts.AsQueryable();
            
            // 按 project_code 过滤（单租户多项目）
            var projectCode = GetProjectCode();
            if (!string.IsNullOrEmpty(projectCode))
            {
                query = query.Where(c => c.ProjectCode == projectCode);
            }

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
                Type = request.Type ?? "Property",
                PartyA = request.PartyA,
                PartyAContact = request.PartyAContact,
                PartyAPhone = request.PartyAPhone,
                PartyB = request.PartyB,
                PartyBContact = request.PartyBContact,
                PartyBPhone = request.PartyBPhone,
                Amount = request.Amount,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = "Draft",
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
                contract.Status = request.Status;
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "状态更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateContractStatus failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/contract/contracts/{id} - 合同详情
    [HttpGet("contracts/{id}")]
    public async Task<IActionResult> GetContractDetail(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var contract = await db.Contracts.FindAsync(id);
            if (contract == null) return NotFound(new { success = false, message = "合同不存在" });

            var response = new ContractDetailResponse
            {
                Id = contract.Id,
                ContractNumber = contract.ContractNumber,
                Type = contract.Type,
                Title = contract.Title,
                Description = contract.Description,
                PartyA = contract.PartyA,
                PartyAContact = contract.PartyAContact,
                PartyAPhone = contract.PartyAPhone,
                PartyB = contract.PartyB,
                PartyBContact = contract.PartyBContact,
                PartyBPhone = contract.PartyBPhone,
                Amount = contract.Amount,
                Currency = contract.Currency,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                AttachmentUrl = contract.AttachmentUrl,
                Status = contract.Status,
                CreatedAt = contract.CreatedAt,
                UpdatedAt = contract.UpdatedAt,
                ProjectCode = contract.ProjectCode
            };
            return Ok(new { success = true, data = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetContractDetail failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // PUT /api/tenant/contract/contracts/{id} - 更新合同
    [HttpPut("contracts/{id}")]
    public async Task<IActionResult> UpdateContract(int id, [FromBody] UpdateContractRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var contract = await db.Contracts.FindAsync(id);
            if (contract == null) return NotFound(new { success = false, message = "合同不存在" });

            if (!string.IsNullOrEmpty(request.Title)) contract.Title = request.Title;
            if (!string.IsNullOrEmpty(request.Type)) contract.Type = request.Type;
            if (!string.IsNullOrEmpty(request.PartyA)) contract.PartyA = request.PartyA;
            if (request.PartyAContact != null) contract.PartyAContact = request.PartyAContact;
            if (request.PartyAPhone != null) contract.PartyAPhone = request.PartyAPhone;
            if (!string.IsNullOrEmpty(request.PartyB)) contract.PartyB = request.PartyB;
            if (request.PartyBContact != null) contract.PartyBContact = request.PartyBContact;
            if (request.PartyBPhone != null) contract.PartyBPhone = request.PartyBPhone;
            if (request.Amount.HasValue) contract.Amount = request.Amount;
            if (!string.IsNullOrEmpty(request.Currency)) contract.Currency = request.Currency;
            if (request.StartDate.HasValue) contract.StartDate = request.StartDate;
            if (request.EndDate.HasValue) contract.EndDate = request.EndDate;
            if (request.Description != null) contract.Description = request.Description;
            if (request.AttachmentUrl != null) contract.AttachmentUrl = request.AttachmentUrl;

            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "合同更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateContract failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // DELETE /api/tenant/contract/contracts/{id} - 删除合同
    [HttpDelete("contracts/{id}")]
    public async Task<IActionResult> DeleteContract(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var contract = await db.Contracts.FindAsync(id);
            if (contract == null) return NotFound(new { success = false, message = "合同不存在" });

            db.Contracts.Remove(contract);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "合同删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteContract failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/contract/contracts/{id}/activate - 激活合同
    [HttpPost("contracts/{id}/activate")]
    public async Task<IActionResult> ActivateContract(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var contract = await db.Contracts.FindAsync(id);
            if (contract == null) return NotFound(new { success = false, message = "合同不存在" });

            if (contract.Status == "Active")
                return Ok(new { success = false, message = "合同已是激活状态" });
            if (contract.Status == "Terminated")
                return Ok(new { success = false, message = "已终止的合同无法激活" });

            contract.Status = "Active";
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "合同激活成功", data = new { status = "Active" } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ActivateContract failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/contract/contracts/{id}/terminate - 终止合同
    [HttpPost("contracts/{id}/terminate")]
    public async Task<IActionResult> TerminateContract(int id, [FromBody] TerminateContractRequest? request = null)
    {
        try
        {
            using var db = CreateDbContext();
            var contract = await db.Contracts.FindAsync(id);
            if (contract == null) return NotFound(new { success = false, message = "合同不存在" });

            if (contract.Status == "Terminated")
                return Ok(new { success = false, message = "合同已是终止状态" });

            contract.Status = "Terminated";
            if (request?.TerminateDate != null)
                contract.EndDate = request.TerminateDate;

            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "合同终止成功", data = new { status = "Terminated" } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TerminateContract failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/contract/contracts/{id}/renew - 续约
    [HttpPost("contracts/{id}/renew")]
    public async Task<IActionResult> RenewContract(int id, [FromBody] RenewContractRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var contract = await db.Contracts.FindAsync(id);
            if (contract == null) return NotFound(new { success = false, message = "合同不存在" });

            // 创建新合同（原合同标记为 Renewed）
            var oldStatus = contract.Status;
            contract.Status = "Renewed";

            var newContractNum = $"C-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            var newContract = new Contract
            {
                ContractNumber = newContractNum,
                Title = contract.Title + " (续约)",
                Type = contract.Type,
                PartyA = contract.PartyA,
                PartyAContact = contract.PartyAContact,
                PartyAPhone = contract.PartyAPhone,
                PartyB = contract.PartyB,
                PartyBContact = contract.PartyBContact,
                PartyBPhone = contract.PartyBPhone,
                Amount = request.NewAmount ?? contract.Amount,
                Currency = contract.Currency,
                StartDate = request.NewStartDate,
                EndDate = request.NewEndDate,
                Status = "Active",
                Description = contract.Description,
                ProjectCode = contract.ProjectCode,
                CreatedAt = DateTime.UtcNow
            };

            db.Contracts.Add(newContract);
            await db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "合同续约成功",
                data = new
                {
                    oldContractId = id,
                    oldContractNumber = contract.ContractNumber,
                    newContractId = newContract.Id,
                    newContractNumber = newContractNum,
                    newStartDate = request.NewStartDate,
                    newEndDate = request.NewEndDate
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RenewContract failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/contract/contracts/expiring - 30天内到期合同
    [HttpGet("contracts/expiring")]
    public async Task<IActionResult> GetExpiringContracts([FromQuery] int days = 30)
    {
        try
        {
            using var db = CreateDbContext();
            var projectCode = GetProjectCode();
            var now = DateTime.UtcNow;
            var cutoff = now.AddDays(days);

            var query = db.Contracts.AsQueryable();

            if (!string.IsNullOrEmpty(projectCode))
                query = query.Where(c => c.ProjectCode == projectCode);

            var items = await query
                .Where(c => c.EndDate != null && c.EndDate >= now && c.EndDate <= cutoff)
                .Where(c => c.Status == "Active" || c.Status == "ExpiringSoon")
                .OrderBy(c => c.EndDate)
                .Select(c => new
                {
                    id = c.Id,
                    contractNumber = c.ContractNumber,
                    title = c.Title,
                    type = c.Type,
                    partyA = c.PartyA,
                    partyB = c.PartyB,
                    amount = c.Amount,
                    currency = c.Currency,
                    startDate = c.StartDate,
                    endDate = c.EndDate,
                    status = c.Status,
                    daysUntilExpiry = (int)(c.EndDate!.Value - DateTime.UtcNow).TotalDays
                })
                .ToListAsync();

            return Ok(new { success = true, data = items, total = items.Count, checkDays = days });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetExpiringContracts failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/contract/contracts/{id}/attachment - 上传合同附件
    [HttpPost("contracts/{id}/attachment")]
    public async Task<IActionResult> UploadAttachment(int id, [FromBody] UploadAttachmentRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var contract = await db.Contracts.FindAsync(id);
            if (contract == null) return NotFound(new { success = false, message = "合同不存在" });

            if (string.IsNullOrEmpty(request.Url))
                return Ok(new { success = false, message = "附件URL不能为空" });

            contract.AttachmentUrl = request.Url;
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "附件上传成功", data = new { attachmentUrl = request.Url } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UploadAttachment failed");
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
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Description { get; set; }
    public string? AttachmentUrl { get; set; }
}

public class UpdateContractRequest
{
    public string? Title { get; set; }
    public string? Type { get; set; }
    public string? PartyA { get; set; }
    public string? PartyAContact { get; set; }
    public string? PartyAPhone { get; set; }
    public string? PartyB { get; set; }
    public string? PartyBContact { get; set; }
    public string? PartyBPhone { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Description { get; set; }
    public string? AttachmentUrl { get; set; }
}

public class UpdateContractStatusRequest
{
    public string? Status { get; set; }
}

public class TerminateContractRequest
{
    public DateTime? TerminateDate { get; set; }
    public string? Reason { get; set; }
}

public class UploadAttachmentRequest
{
    public string Url { get; set; } = "";
    public string? FileName { get; set; }
}

public class RenewContractRequest
{
    public DateTime NewStartDate { get; set; }
    public DateTime NewEndDate { get; set; }
    public decimal? NewAmount { get; set; }
}

public class ContractDetailResponse
{
    public int Id { get; set; }
    public string ContractNumber { get; set; } = "";
    public string? Type { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? PartyA { get; set; }
    public string? PartyAContact { get; set; }
    public string? PartyAPhone { get; set; }
    public string? PartyB { get; set; }
    public string? PartyBContact { get; set; }
    public string? PartyBPhone { get; set; }
    public decimal? Amount { get; set; }
    public string Currency { get; set; } = "CNY";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? AttachmentUrl { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? ProjectCode { get; set; }
}