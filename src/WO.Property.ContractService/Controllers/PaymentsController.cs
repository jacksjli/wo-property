using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.ContractService.Data;
using WO.Property.ContractService.Models;

namespace WO.Property.ContractService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly ContractDbContext _context;
    
    public PaymentsController(ContractDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// 获取所有付款记录
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPayments(
        [FromQuery] int? contractId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Payments
            .Include(p => p.Contract)
            .AsQueryable();
        
        if (contractId.HasValue)
        {
            query = query.Where(p => p.ContractId == contractId.Value);
        }
        
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(p => p.Status == status);
        }
        
        var total = await query.CountAsync();
        var payments = await query
            .OrderByDescending(p => p.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return Ok(new
        {
            success = true,
            total,
            page,
            pageSize,
            payments
        });
    }
    
    /// <summary>
    /// 获取付款记录详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPayment(int id)
    {
        var payment = await _context.Payments
            .Include(p => p.Contract)
            .FirstOrDefaultAsync(p => p.Id == id);
        
        if (payment == null)
        {
            return NotFound(new { success = false, message = "付款记录不存在" });
        }
        
        return Ok(new { success = true, data = payment });
    }
    
    /// <summary>
    /// 创建付款记录
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] Payment payment)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "数据验证失败", errors = ModelState });
        }
        
        // 生成付款编号
        if (string.IsNullOrEmpty(payment.PaymentNumber))
        {
            var year = DateTime.UtcNow.Year;
            var count = await _context.Payments
                .Where(p => p.PaymentNumber.StartsWith($"PAY-{year}"))
                .CountAsync() + 1;
            payment.PaymentNumber = $"PAY-{year}-{count:D4}";
        }
        
        payment.CreatedAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;
        
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "付款记录创建成功", data = payment });
    }
    
    /// <summary>
    /// 更新付款记录
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePayment(int id, [FromBody] Payment payment)
    {
        var existing = await _context.Payments.FindAsync(id);
        
        if (existing == null)
        {
            return NotFound(new { success = false, message = "付款记录不存在" });
        }
        
        existing.Description = payment.Description;
        existing.Amount = payment.Amount;
        existing.DueDate = payment.DueDate;
        existing.PaidDate = payment.PaidDate;
        existing.Status = payment.Status;
        existing.Remarks = payment.Remarks;
        existing.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "付款记录更新成功", data = existing });
    }
    
    /// <summary>
    /// 标记付款
    /// </summary>
    [HttpPost("{id}/pay")]
    public async Task<IActionResult> MarkAsPaid(int id, [FromBody] PayRequest request)
    {
        var payment = await _context.Payments.FindAsync(id);
        
        if (payment == null)
        {
            return NotFound(new { success = false, message = "付款记录不存在" });
        }
        
        payment.Status = "Paid";
        payment.PaidDate = request.PaidDate ?? DateTime.UtcNow;
        payment.Remarks = request.Remarks;
        payment.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "付款已确认", data = payment });
    }
    
    /// <summary>
    /// 删除付款记录
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePayment(int id)
    {
        var payment = await _context.Payments.FindAsync(id);
        
        if (payment == null)
        {
            return NotFound(new { success = false, message = "付款记录不存在" });
        }
        
        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "付款记录删除成功" });
    }
    
    /// <summary>
    /// 获取付款统计
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var now = DateTime.UtcNow;
        
        var stats = new
        {
            total = await _context.Payments.CountAsync(),
            pending = await _context.Payments.CountAsync(p => p.Status == "Pending"),
            paid = await _context.Payments.CountAsync(p => p.Status == "Paid"),
            overdue = await _context.Payments.CountAsync(p => 
                p.Status == "Pending" && p.DueDate < now),
            totalPendingAmount = await _context.Payments
                .Where(p => p.Status == "Pending")
                .SumAsync(p => p.Amount),
            totalPaidAmount = await _context.Payments
                .Where(p => p.Status == "Paid")
                .SumAsync(p => p.Amount),
            totalOverdueAmount = await _context.Payments
                .Where(p => p.Status == "Pending" && p.DueDate < now)
                .SumAsync(p => p.Amount)
        };
        
        return Ok(new { success = true, data = stats });
    }
}

public class PayRequest
{
    public DateTime? PaidDate { get; set; }
    public string? Remarks { get; set; }
}
