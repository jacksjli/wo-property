using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.FinanceService.Data;
using WO.Property.FinanceService.Models;
using BillStatusEnum = WO.Property.FinanceService.Models.BillStatus;

namespace WO.Property.FinanceService.Controllers;

[ApiController]
[Route("api/tenant/finance")]
public class TenantFinanceController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ILogger<TenantFinanceController> _logger;

    public TenantFinanceController(IDbContextFactory<TenantDbContext> dbFactory, ILogger<TenantFinanceController> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    // GET /api/tenant/finance/bills
    [HttpGet("bills")]
    public async Task<IActionResult> GetBills([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Bills.AsQueryable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.Status.ToString() == status);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new
                {
                    id = b.Id,
                    billNumber = b.BillNumber,
                    roomNumber = b.RoomNumber,
                    ownerName = b.OwnerName,
                    feeType = b.FeeType.ToString(),
                    amount = b.Amount,
                    actualAmount = b.ActualAmount,
                    dueDate = b.DueDate,
                    paidDate = b.PaidDate,
                    status = b.Status.ToString()
                })
                .ToListAsync();
            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBills failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/finance/bills
    [HttpPost("bills")]
    public async Task<IActionResult> CreateBill([FromBody] CreateBillRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var billNum = $"B-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            var bill = new Bill
            {
                BillNumber = billNum,
                RoomNumber = request.RoomNumber,
                OwnerName = request.OwnerName,
                OwnerPhone = request.OwnerPhone,
                FeeType = Enum.Parse<FeeType>(request.FeeType ?? "PropertyFee"),
                Amount = request.Amount,
                ActualAmount = request.ActualAmount,
                BillingPeriodStart = request.BillingPeriodStart,
                BillingPeriodEnd = request.BillingPeriodEnd,
                DueDate = request.DueDate,
                Status = BillStatusEnum.Pending,
                CreatedAt = DateTime.UtcNow
            };
            db.Bills.Add(bill);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "账单创建成功", data = new { billNumber = billNum } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateBill failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // PUT /api/tenant/finance/bills/{id}/pay
    [HttpPut("bills/{id}/pay")]
    public async Task<IActionResult> PayBill(int id, [FromBody] TenantPayBillRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var bill = await db.Bills.FindAsync(id);
            if (bill == null) return NotFound(new { success = false, message = "账单不存在" });
            bill.Status = BillStatusEnum.Paid;
            bill.PaidDate = DateTime.UtcNow;

            var payNum = $"P-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            var payment = new Payment
            {
                PaymentNumber = payNum,
                BillId = id,
                Amount = bill.ActualAmount,
                PaymentMethod = Enum.Parse<PaymentMethod>(request.PaymentMethod ?? "BankTransfer"),
                PaymentDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            db.Payments.Add(payment);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "支付成功", data = new { paymentNumber = payNum } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayBill failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/finance/payments
    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            using var db = CreateDbContext();
            var total = await db.Payments.CountAsync();
            var items = await db.Payments
                .OrderByDescending(p => p.PaymentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    id = p.Id,
                    paymentNumber = p.PaymentNumber,
                    billId = p.BillId,
                    amount = p.Amount,
                    paymentMethod = p.PaymentMethod.ToString(),
                    paymentDate = p.PaymentDate,
                    transactionId = p.TransactionId ?? ""
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

public class CreateBillRequest
{
    public string RoomNumber { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string? OwnerPhone { get; set; }
    public string? FeeType { get; set; }
    public decimal Amount { get; set; }
    public decimal ActualAmount { get; set; }
    public DateTime BillingPeriodStart { get; set; }
    public DateTime BillingPeriodEnd { get; set; }
    public DateTime DueDate { get; set; }
}

public class TenantPayBillRequest
{
    public string? PaymentMethod { get; set; }
}