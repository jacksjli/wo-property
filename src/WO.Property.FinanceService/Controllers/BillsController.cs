using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.FinanceService.Data;
using WO.Property.FinanceService.Models;

namespace WO.Property.FinanceService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BillsController : ControllerBase
{
    private readonly FinanceDbContext _context;
    
    public BillsController(FinanceDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// 获取账单列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBills(
        [FromQuery] string? roomNumber = null,
        [FromQuery] BillStatus? status = null,
        [FromQuery] FeeType? feeType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Bills.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(roomNumber))
        {
            query = query.Where(b => b.RoomNumber.Contains(roomNumber));
        }
        
        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }
        
        if (feeType.HasValue)
        {
            query = query.Where(b => b.FeeType == feeType.Value);
        }
        
        var total = await query.CountAsync();
        var bills = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return Ok(new
        {
            success = true,
            total,
            page,
            pageSize,
            bills
        });
    }
    
    /// <summary>
    /// 获取账单详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBill(int id)
    {
        var bill = await _context.Bills.FindAsync(id);
        
        if (bill == null)
        {
            return NotFound(new { success = false, message = "账单不存在" });
        }
        
        return Ok(new { success = true, data = bill });
    }
    
    /// <summary>
    /// 创建账单
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateBill([FromBody] Bill bill)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "数据验证失败", errors = ModelState });
        }
        
        // 生成账单编号
        if (string.IsNullOrEmpty(bill.BillNumber))
        {
            var year = DateTime.Now.Year;
            var count = await _context.Bills
                .Where(b => b.BillNumber.StartsWith($"BILL-{year}"))
                .CountAsync() + 1;
            bill.BillNumber = $"BILL-{year}-{count:D4}";
        }
        
        bill.ActualAmount = bill.Amount - bill.DiscountAmount;
        bill.CreatedAt = DateTime.UtcNow;
        bill.UpdatedAt = DateTime.UtcNow;
        
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "账单创建成功", data = bill });
    }
    
    /// <summary>
    /// 更新账单
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBill(int id, [FromBody] Bill bill)
    {
        var existing = await _context.Bills.FindAsync(id);
        
        if (existing == null)
        {
            return NotFound(new { success = false, message = "账单不存在" });
        }
        
        existing.RoomNumber = bill.RoomNumber;
        existing.OwnerName = bill.OwnerName;
        existing.OwnerPhone = bill.OwnerPhone;
        existing.FeeType = bill.FeeType;
        existing.Amount = bill.Amount;
        existing.DiscountAmount = bill.DiscountAmount;
        existing.ActualAmount = bill.Amount - bill.DiscountAmount;
        existing.BillingPeriodStart = bill.BillingPeriodStart;
        existing.BillingPeriodEnd = bill.BillingPeriodEnd;
        existing.DueDate = bill.DueDate;
        existing.Status = bill.Status;
        existing.Remarks = bill.Remarks;
        existing.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "账单更新成功", data = existing });
    }
    
    /// <summary>
    /// 删除账单
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBill(int id)
    {
        var bill = await _context.Bills.FindAsync(id);
        
        if (bill == null)
        {
            return NotFound(new { success = false, message = "账单不存在" });
        }
        
        if (bill.Status == BillStatus.Paid)
        {
            return BadRequest(new { success = false, message = "已支付的账单不能删除" });
        }
        
        _context.Bills.Remove(bill);
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "账单删除成功" });
    }
    
    /// <summary>
    /// 账单统计
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var now = DateTime.UtcNow;
        
        var stats = new
        {
            totalBills = await _context.Bills.CountAsync(),
            pendingBills = await _context.Bills.CountAsync(b => b.Status == BillStatus.Pending),
            paidBills = await _context.Bills.CountAsync(b => b.Status == BillStatus.Paid),
            overdueBills = await _context.Bills.CountAsync(b => b.Status == BillStatus.Overdue),
            totalPendingAmount = await _context.Bills
                .Where(b => b.Status == BillStatus.Pending)
                .SumAsync(b => b.ActualAmount),
            totalPaidAmount = await _context.Bills
                .Where(b => b.Status == BillStatus.Paid)
                .SumAsync(b => b.ActualAmount),
            totalOverdueAmount = await _context.Bills
                .Where(b => b.Status == BillStatus.Overdue)
                .SumAsync(b => b.ActualAmount),
            byFeeType = await _context.Bills
                .Where(b => b.Status == BillStatus.Pending)
                .GroupBy(b => b.FeeType)
                .Select(g => new { feeType = g.Key.ToString(), count = g.Count(), amount = g.Sum(b => b.ActualAmount) })
                .ToListAsync()
        };
        
        return Ok(new { success = true, data = stats });
    }
    
    /// <summary>
    /// 支付账单
    /// </summary>
    [HttpPost("{id}/pay")]
    public async Task<IActionResult> PayBill(int id, [FromBody] PayBillRequest request)
    {
        var bill = await _context.Bills.FindAsync(id);
        
        if (bill == null)
        {
            return NotFound(new { success = false, message = "账单不存在" });
        }
        
        if (bill.Status == BillStatus.Paid)
        {
            return BadRequest(new { success = false, message = "账单已支付" });
        }
        
        // 创建付款记录
        var year = DateTime.Now.Year;
        var paymentCount = await _context.Payments
            .Where(p => p.PaymentNumber.StartsWith($"PAY-{year}"))
            .CountAsync() + 1;
        
        var payment = new Payment
        {
            PaymentNumber = $"PAY-{year}-{paymentCount:D4}",
            BillId = bill.Id,
            Amount = bill.ActualAmount,
            Currency = bill.Currency,
            PaymentMethod = request.PaymentMethod,
            PaymentDate = request.PaymentDate ?? DateTime.UtcNow,
            TransactionId = request.TransactionId,
            Remarks = request.Remarks,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Payments.Add(payment);
        
        // 创建收据
        var receiptCount = await _context.Receipts
            .Where(r => r.ReceiptNumber.StartsWith($"RCP-{year}"))
            .CountAsync() + 1;
        
        var receipt = new Receipt
        {
            ReceiptNumber = $"RCP-{year}-{receiptCount:D4}",
            PaymentId = payment.Id,
            PayerName = bill.OwnerName,
            PayerPhone = bill.OwnerPhone,
            RoomNumber = bill.RoomNumber,
            Amount = bill.ActualAmount,
            Description = $"{bill.FeeType} - {bill.BillingPeriodStart:yyyyMMdd}-{bill.BillingPeriodEnd:yyyyMMdd}",
            IssuedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Receipts.Add(receipt);
        
        // 更新账单状态
        bill.Status = BillStatus.Paid;
        bill.PaidDate = payment.PaymentDate;
        bill.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return Ok(new
        {
            success = true,
            message = "账单支付成功",
            data = new
            {
                bill,
                payment,
                receipt
            }
        });
    }
}

public class PayBillRequest
{
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? TransactionId { get; set; }
    public string? Remarks { get; set; }
}
