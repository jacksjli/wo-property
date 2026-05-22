using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.PaymentService.Data;
using WO.Property.PaymentService.Models;

namespace WO.Property.PaymentService.Controllers;

[ApiController]
[Route("api/tenant/payments")]
public class TenantPaymentController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ILogger<TenantPaymentController> _logger;

    public TenantPaymentController(
        IDbContextFactory<TenantDbContext> dbFactory,
        ILogger<TenantPaymentController> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    // GET /api/tenant/payments - 分页查询账单
    [HttpGet]
    public async Task<IActionResult> GetPayments(
        [FromQuery] string? unit = null,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Bills.AsQueryable();

            if (!string.IsNullOrEmpty(unit))
                query = query.Where(b => b.Unit != null && b.Unit.Contains(unit));
            if (!string.IsNullOrEmpty(type))
                query = query.Where(b => b.Type == type);
            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.Status == status);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(b => b.BillingMonth)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new
                {
                    id = b.Id,
                    billNo = b.BillNo,
                    type = b.Type ?? "",
                    description = b.Description ?? "",
                    unit = b.Unit ?? "",
                    amount = b.Amount,
                    billingMonth = b.BillingMonth,
                    status = b.Status ?? "",
                    paidAt = b.PaidAt,
                    paymentMethod = b.PaymentMethod ?? "",
                    transactionId = b.TransactionId ?? "",
                    projectId = b.ProjectId,
                    createdAt = b.CreatedAt
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

    // GET /api/tenant/payments/{id} - 获取单个账单
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPayment(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var bill = await db.Bills.FindAsync(id);
            if (bill == null)
                return Ok(new { success = false, message = "账单不存在" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = bill.Id,
                    billNo = bill.BillNo,
                    type = bill.Type ?? "",
                    description = bill.Description ?? "",
                    unit = bill.Unit ?? "",
                    amount = bill.Amount,
                    billingMonth = bill.BillingMonth,
                    status = bill.Status ?? "",
                    paidAt = bill.PaidAt,
                    paymentMethod = bill.PaymentMethod ?? "",
                    transactionId = bill.TransactionId ?? "",
                    remark = bill.Remark ?? "",
                    projectId = bill.ProjectId,
                    createdAt = bill.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPayment failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/payments - 创建账单
    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var bill = new Bill
            {
                BillNo = request.BillNo ?? $"BILL-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Type = request.Type ?? "property",
                Description = request.Description,
                Unit = request.Unit,
                Amount = request.Amount,
                BillingMonth = request.BillingMonth ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1),
                Status = request.Status ?? "unpaid",
                ProjectId = request.ProjectId,
                CreatedAt = DateTime.UtcNow
            };

            db.Bills.Add(bill);
            await db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "账单创建成功",
                data = new
                {
                    id = bill.Id,
                    billNo = bill.BillNo
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreatePayment failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // PUT /api/tenant/payments/{id} - 更新账单
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePayment(int id, [FromBody] UpdatePaymentRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var bill = await db.Bills.FindAsync(id);
            if (bill == null)
                return Ok(new { success = false, message = "账单不存在" });

            if (request.Type != null) bill.Type = request.Type;
            if (request.Description != null) bill.Description = request.Description;
            if (request.Unit != null) bill.Unit = request.Unit;
            if (request.Amount.HasValue) bill.Amount = request.Amount.Value;
            if (request.Status != null) bill.Status = request.Status;
            if (request.Remark != null) bill.Remark = request.Remark;

            bill.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "账单更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdatePayment failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // DELETE /api/tenant/payments/{id} - 删除账单
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePayment(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var bill = await db.Bills.FindAsync(id);
            if (bill == null)
                return Ok(new { success = false, message = "账单不存在" });

            db.Bills.Remove(bill);
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "账单删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeletePayment failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/payments/{id}/pay - 支付账单
    [HttpPost("{id}/pay")]
    public async Task<IActionResult> PayPayment(int id, [FromBody] PayPaymentRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var bill = await db.Bills.FindAsync(id);
            if (bill == null)
                return Ok(new { success = false, message = "账单不存在" });

            bill.Status = "paid";
            bill.PaidAt = DateTime.UtcNow;
            bill.PaymentMethod = request.Method ?? "cash";
            bill.TransactionId = "TXN-" + Guid.NewGuid().ToString()[..12].ToUpper();
            bill.Remark = request.Remark ?? bill.Remark;
            bill.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "支付成功",
                data = new
                {
                    id = bill.Id,
                    status = bill.Status,
                    transactionId = bill.TransactionId
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPayment failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

public class CreatePaymentRequest
{
    public string? BillNo { get; set; }
    public string? Type { get; set; }
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public decimal Amount { get; set; }
    public DateTime? BillingMonth { get; set; }
    public string? Status { get; set; }
    public int ProjectId { get; set; } = 1;
}

public class UpdatePaymentRequest
{
    public string? Type { get; set; }
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public decimal? Amount { get; set; }
    public string? Status { get; set; }
    public string? Remark { get; set; }
}

public class PayPaymentRequest
{
    public string? Method { get; set; }
    public string? Remark { get; set; }
}