using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.FinanceService.Data;
using WO.Property.FinanceService.Models;

namespace WO.Property.FinanceService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly FinanceDbContext _context;
    
    public PaymentsController(FinanceDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// 获取付款记录列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPayments(
        [FromQuery] int? billId = null,
        [FromQuery] PaymentMethod? paymentMethod = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Payments
            .Include(p => p.Bill)
            .AsQueryable();
        
        if (billId.HasValue)
        {
            query = query.Where(p => p.BillId == billId.Value);
        }
        
        if (paymentMethod.HasValue)
        {
            query = query.Where(p => p.PaymentMethod == paymentMethod.Value);
        }
        
        if (startDate.HasValue)
        {
            query = query.Where(p => p.PaymentDate >= startDate.Value);
        }
        
        if (endDate.HasValue)
        {
            query = query.Where(p => p.PaymentDate <= endDate.Value);
        }
        
        var total = await query.CountAsync();
        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
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
            .Include(p => p.Bill)
            .FirstOrDefaultAsync(p => p.Id == id);
        
        if (payment == null)
        {
            return NotFound(new { success = false, message = "付款记录不存在" });
        }
        
        return Ok(new { success = true, data = payment });
    }
    
    /// <summary>
    /// 获取收据
    /// </summary>
    [HttpGet("{id}/receipt")]
    public async Task<IActionResult> GetReceiptByPayment(int id)
    {
        var receipt = await _context.Receipts
            .FirstOrDefaultAsync(r => r.PaymentId == id);
        
        if (receipt == null)
        {
            return NotFound(new { success = false, message = "收据不存在" });
        }
        
        return Ok(new { success = true, data = receipt });
    }
    
    /// <summary>
    /// 付款统计
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfYear = new DateTime(now.Year, 1, 1);
        
        var stats = new
        {
            totalPayments = await _context.Payments.CountAsync(),
            totalAmount = await _context.Payments.SumAsync(p => p.Amount),
            monthlyPayments = await _context.Payments
                .Where(p => p.PaymentDate >= startOfMonth)
                .CountAsync(),
            monthlyAmount = await _context.Payments
                .Where(p => p.PaymentDate >= startOfMonth)
                .SumAsync(p => p.Amount),
            yearlyAmount = await _context.Payments
                .Where(p => p.PaymentDate >= startOfYear)
                .SumAsync(p => p.Amount),
            byMethod = await _context.Payments
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new { method = g.Key.ToString(), count = g.Count(), amount = g.Sum(p => p.Amount) })
                .ToListAsync()
        };
        
        return Ok(new { success = true, data = stats });
    }
}

/// <summary>
/// 费用科目控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FeeItemsController : ControllerBase
{
    private readonly FinanceDbContext _context;
    
    public FeeItemsController(FinanceDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// 获取费用科目列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFeeItems([FromQuery] bool activeOnly = true)
    {
        var query = _context.FeeItems.AsQueryable();
        
        if (activeOnly)
        {
            query = query.Where(f => f.IsActive);
        }
        
        var items = await query.OrderBy(f => f.Type).ToListAsync();
        
        return Ok(new { success = true, data = items });
    }
    
    /// <summary>
    /// 获取费用科目详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetFeeItem(int id)
    {
        var item = await _context.FeeItems.FindAsync(id);
        
        if (item == null)
        {
            return NotFound(new { success = false, message = "费用科目不存在" });
        }
        
        return Ok(new { success = true, data = item });
    }
    
    /// <summary>
    /// 创建费用科目
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateFeeItem([FromBody] FeeItem item)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "数据验证失败", errors = ModelState });
        }
        
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        
        _context.FeeItems.Add(item);
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "费用科目创建成功", data = item });
    }
    
    /// <summary>
    /// 更新费用科目
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFeeItem(int id, [FromBody] FeeItem item)
    {
        var existing = await _context.FeeItems.FindAsync(id);
        
        if (existing == null)
        {
            return NotFound(new { success = false, message = "费用科目不存在" });
        }
        
        existing.Name = item.Name;
        existing.Type = item.Type;
        existing.UnitPrice = item.UnitPrice;
        existing.Unit = item.Unit;
        existing.Description = item.Description;
        existing.IsActive = item.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "费用科目更新成功", data = existing });
    }
}

/// <summary>
/// 收据控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReceiptsController : ControllerBase
{
    private readonly FinanceDbContext _context;
    
    public ReceiptsController(FinanceDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// 获取收据列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReceipts(
        [FromQuery] string? roomNumber = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Receipts.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(roomNumber))
        {
            query = query.Where(r => r.RoomNumber.Contains(roomNumber));
        }
        
        if (startDate.HasValue)
        {
            query = query.Where(r => r.IssuedDate >= startDate.Value);
        }
        
        if (endDate.HasValue)
        {
            query = query.Where(r => r.IssuedDate <= endDate.Value);
        }
        
        var total = await query.CountAsync();
        var receipts = await query
            .Include(r => r.Payment)
            .OrderByDescending(r => r.IssuedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return Ok(new
        {
            success = true,
            total,
            page,
            pageSize,
            receipts
        });
    }
    
    /// <summary>
    /// 获取收据详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReceipt(int id)
    {
        var receipt = await _context.Receipts
            .Include(r => r.Payment)
            .ThenInclude(p => p!.Bill)
            .FirstOrDefaultAsync(r => r.Id == id);
        
        if (receipt == null)
        {
            return NotFound(new { success = false, message = "收据不存在" });
        }
        
        return Ok(new { success = true, data = receipt });
    }
}
