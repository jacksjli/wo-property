using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.ContractService.Data;
using WO.Property.ContractService.Models;

namespace WO.Property.ContractService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContractsController : ControllerBase
{
    private readonly ContractDbContext _context;
    
    public ContractsController(ContractDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// 获取所有合同列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetContracts(
        [FromQuery] ContractType? type = null,
        [FromQuery] ContractStatus? status = null,
        [FromQuery] string? keyword = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Contracts.AsQueryable();
        
        if (type.HasValue)
        {
            query = query.Where(c => c.Type == type.Value);
        }
        
        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }
        
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(c => 
                c.ContractNumber.Contains(keyword) ||
                c.Title.Contains(keyword) ||
                c.PartyA.Contains(keyword) ||
                c.PartyB.Contains(keyword));
        }
        
        var total = await query.CountAsync();
        var contracts = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return Ok(new
        {
            success = true,
            total,
            page,
            pageSize,
            contracts
        });
    }
    
    /// <summary>
    /// 获取合同详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetContract(int id)
    {
        var contract = await _context.Contracts.FindAsync(id);
        
        if (contract == null)
        {
            return NotFound(new { success = false, message = "合同不存在" });
        }
        
        return Ok(new { success = true, data = contract });
    }
    
    /// <summary>
    /// 创建合同
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateContract([FromBody] Contract contract)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "数据验证失败", errors = ModelState });
        }
        
        // 生成合同编号
        if (string.IsNullOrEmpty(contract.ContractNumber))
        {
            var year = DateTime.Now.Year;
            var count = await _context.Contracts
                .Where(c => c.ContractNumber.StartsWith($"HT-{year}"))
                .CountAsync() + 1;
            contract.ContractNumber = $"HT-{year}-{count:D4}";
        }
        
        contract.CreatedAt = DateTime.UtcNow;
        contract.UpdatedAt = DateTime.UtcNow;
        
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "合同创建成功", data = contract });
    }
    
    /// <summary>
    /// 更新合同
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateContract(int id, [FromBody] Contract contract)
    {
        var existing = await _context.Contracts.FindAsync(id);
        
        if (existing == null)
        {
            return NotFound(new { success = false, message = "合同不存在" });
        }
        
        existing.Title = contract.Title;
        existing.Type = contract.Type;
        existing.Description = contract.Description;
        existing.PartyA = contract.PartyA;
        existing.PartyAContact = contract.PartyAContact;
        existing.PartyAPhone = contract.PartyAPhone;
        existing.PartyB = contract.PartyB;
        existing.PartyBContact = contract.PartyBContact;
        existing.PartyBPhone = contract.PartyBPhone;
        existing.Amount = contract.Amount;
        existing.Currency = contract.Currency;
        existing.StartDate = contract.StartDate;
        existing.EndDate = contract.EndDate;
        existing.AttachmentUrl = contract.AttachmentUrl;
        existing.Status = contract.Status;
        existing.ExtendedData = contract.ExtendedData;
        existing.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "合同更新成功", data = existing });
    }
    
    /// <summary>
    /// 删除合同
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContract(int id)
    {
        var contract = await _context.Contracts.FindAsync(id);
        
        if (contract == null)
        {
            return NotFound(new { success = false, message = "合同不存在" });
        }
        
        _context.Contracts.Remove(contract);
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "合同删除成功" });
    }
    
    /// <summary>
    /// 获取合同统计
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var now = DateTime.UtcNow;
        var thirtyDaysLater = now.AddDays(30);
        
        var stats = new
        {
            total = await _context.Contracts.CountAsync(),
            active = await _context.Contracts.CountAsync(c => c.Status == ContractStatus.Active),
            expiringSoon = await _context.Contracts.CountAsync(c => 
                c.Status == ContractStatus.Active && 
                c.EndDate <= thirtyDaysLater && 
                c.EndDate > now),
            expired = await _context.Contracts.CountAsync(c => 
                c.Status == ContractStatus.Expired || 
                c.EndDate < now),
            draft = await _context.Contracts.CountAsync(c => c.Status == ContractStatus.Draft),
            totalAmount = await _context.Contracts
                .Where(c => c.Status == ContractStatus.Active)
                .SumAsync(c => c.Amount),
            byType = await _context.Contracts
                .GroupBy(c => c.Type)
                .Select(g => new { type = g.Key.ToString(), count = g.Count() })
                .ToListAsync()
        };
        
        return Ok(new { success = true, data = stats });
    }
    
    /// <summary>
    /// 合同续签
    /// </summary>
    [HttpPost("{id}/renew")]
    public async Task<IActionResult> RenewContract(int id, [FromBody] RenewRequest request)
    {
        var contract = await _context.Contracts.FindAsync(id);
        
        if (contract == null)
        {
            return NotFound(new { success = false, message = "合同不存在" });
        }
        
        // 创建新合同
        var newContract = new Contract
        {
            ContractNumber = $"HT-{DateTime.Now.Year}-{id:D4}-R",
            Title = $"{contract.Title} (续签)",
            Type = contract.Type,
            Description = contract.Description,
            PartyA = contract.PartyA,
            PartyAContact = contract.PartyAContact,
            PartyAPhone = contract.PartyAPhone,
            PartyB = contract.PartyB,
            PartyBContact = contract.PartyBContact,
            PartyBPhone = contract.PartyBPhone,
            Amount = request.NewAmount > 0 ? request.NewAmount : contract.Amount,
            Currency = contract.Currency,
            StartDate = contract.EndDate.AddDays(1),
            EndDate = request.NewEndDate,
            Status = ContractStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.Contracts.Add(newContract);
        
        // 更新原合同状态
        contract.Status = ContractStatus.Terminated;
        contract.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "合同续签成功", data = newContract });
    }
}

public class RenewRequest
{
    public decimal NewAmount { get; set; }
    public DateTime NewEndDate { get; set; }
}
