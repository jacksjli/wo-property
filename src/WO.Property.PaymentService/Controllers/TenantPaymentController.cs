using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WO.Property.PaymentService.Data;
using WO.Property.PaymentService.Models;
using System.Text;

namespace WO.Property.PaymentService.Controllers;

[ApiController]
[Authorize]
[Route("api/tenant/payments")]
public class TenantPaymentController : ControllerBase
{
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

    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TenantPaymentController> _logger;

    public TenantPaymentController(
        IDbContextFactory<TenantDbContext> dbFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<TenantPaymentController> logger)
    {
        _dbFactory = dbFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    private async Task PublishPaymentEventAsync(string eventType, object data)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Gateway");
            var payload = new
            {
                module = "payment",
                eventType = eventType,
                data = data
            };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );
            await client.PostAsync("/internal/events/publish", content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish payment event: {EventType}", eventType);
        }
    }

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

            await PublishPaymentEventAsync("created", new
            {
                id = bill.Id,
                billNo = bill.BillNo,
                type = bill.Type,
                amount = bill.Amount,
                status = bill.Status,
                unit = bill.Unit
            });

            return Ok(new
            {
                success = true,
                message = "账单创建成功",
                data = new { id = bill.Id, billNo = bill.BillNo }
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

            if (bill.Status == "paid")
                return Ok(new { success = false, message = "账单已支付" });

            bill.Status = "paid";
            bill.PaidAt = DateTime.UtcNow;
            bill.PaymentMethod = request.Method ?? "cash";
            bill.TransactionId = "TXN-" + Guid.NewGuid().ToString()[..12].ToUpper();
            bill.Remark = request.Remark ?? bill.Remark;
            bill.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            await PublishPaymentEventAsync("status_changed", new
            {
                id = bill.Id,
                billNo = bill.BillNo,
                status = bill.Status,
                transactionId = bill.TransactionId,
                paymentMethod = bill.PaymentMethod,
                amount = bill.Amount
            });

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

    // POST /api/tenant/payments/{id}/refund - 退款
    [HttpPost("{id}/refund")]
    public async Task<IActionResult> RefundPayment(int id, [FromBody] RefundPaymentRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var bill = await db.Bills.FindAsync(id);
            if (bill == null)
                return Ok(new { success = false, message = "账单不存在" });

            if (bill.Status != "paid")
                return Ok(new { success = false, message = "只能对已支付账单退款" });

            bill.Status = "refunded";
            bill.Remark = $"退款: {request.Reason ?? "用户申请退款"} | 原支付方式: {bill.PaymentMethod} | 原交易号: {bill.TransactionId}";
            bill.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            await PublishPaymentEventAsync("refunded", new
            {
                id = bill.Id,
                billNo = bill.BillNo,
                status = bill.Status,
                originalTransactionId = bill.TransactionId,
                amount = bill.Amount,
                refundReason = request.Reason
            });

            return Ok(new
            {
                success = true,
                message = "退款成功",
                data = new
                {
                    id = bill.Id,
                    status = bill.Status,
                    refundAt = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RefundPayment failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/payments/pending - 待缴费列表
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingPayments(
        [FromQuery] string? unit = null,
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Bills.Where(b => b.Status == "unpaid");

            if (!string.IsNullOrEmpty(unit))
                query = query.Where(b => b.Unit != null && b.Unit.Contains(unit));
            if (!string.IsNullOrEmpty(type))
                query = query.Where(b => b.Type == type);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(b => b.BillingMonth)
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
                    status = b.Status,
                    projectId = b.ProjectId,
                    createdAt = b.CreatedAt,
                    overdueDays = (DateTime.UtcNow - b.BillingMonth.AddMonths(1)).Days
                })
                .ToListAsync();

            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPendingPayments failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/payments/stats - 缴费统计
    [HttpGet("stats")]
    public async Task<IActionResult> GetPaymentStats([FromQuery] int? projectId = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Bills.AsQueryable();
            if (projectId.HasValue)
                query = query.Where(b => b.ProjectId == projectId.Value);

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = monthStart.AddMonths(-1);
            var yearStart = new DateTime(now.Year, 1, 1);

            var thisMonthBills = await query.Where(b => b.BillingMonth >= monthStart).ToListAsync();
            var thisMonthRevenue = thisMonthBills.Where(b => b.Status == "paid").Sum(b => b.Amount);
            var thisMonthPending = thisMonthBills.Where(b => b.Status == "unpaid").Sum(b => b.Amount);
            var thisMonthPaidCount = thisMonthBills.Count(b => b.Status == "paid");
            var thisMonthTotal = thisMonthBills.Count;

            var lastMonthBills = await query.Where(b => b.BillingMonth >= lastMonthStart && b.BillingMonth < monthStart).ToListAsync();
            var lastMonthRevenue = lastMonthBills.Where(b => b.Status == "paid").Sum(b => b.Amount);
            var lastMonthPaidCount = lastMonthBills.Count(b => b.Status == "paid");
            var lastMonthTotal = lastMonthBills.Count;

            var yearBills = await query.Where(b => b.BillingMonth >= yearStart).ToListAsync();
            var yearRevenue = yearBills.Where(b => b.Status == "paid").Sum(b => b.Amount);

            var allUnpaidCount = await query.CountAsync(b => b.Status == "unpaid");
            var allUnpaidAmount = await query.Where(b => b.Status == "unpaid").SumAsync(b => b.Amount);
            var allRefundedCount = await query.CountAsync(b => b.Status == "refunded");

            return Ok(new
            {
                success = true,
                data = new
                {
                    thisMonth = new
                    {
                        revenue = thisMonthRevenue,
                        pending = thisMonthPending,
                        collectionRate = thisMonthTotal > 0 ? Math.Round(100.0 * thisMonthPaidCount / thisMonthTotal, 1) : 0,
                        totalBills = thisMonthTotal,
                        paidBills = thisMonthPaidCount
                    },
                    lastMonth = new
                    {
                        revenue = lastMonthRevenue,
                        collectionRate = lastMonthTotal > 0 ? Math.Round(100.0 * lastMonthPaidCount / lastMonthTotal, 1) : 0,
                        totalBills = lastMonthTotal
                    },
                    yearToDate = new { revenue = yearRevenue },
                    overview = new
                    {
                        unpaidCount = allUnpaidCount,
                        unpaidAmount = allUnpaidAmount,
                        refundedCount = allRefundedCount
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPaymentStats failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

// ==================== Request DTOs ====================

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

public class RefundPaymentRequest
{
    public string? Reason { get; set; }
}