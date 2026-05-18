using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.KeyService.Data;
using WO.Property.KeyService.Models;

namespace WO.Property.KeyService.Controllers;

/// <summary>
/// Phase 1 多租户钥匙服务控制器
/// </summary>
[ApiController]
[Route("api/tenant/key")]
public class TenantKeyController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantKeyController> _logger;

    public TenantKeyController(
        IDbContextFactory<TenantDbContext> dbFactory,
        Tenant.ITenantDbFactory tenantDbFactory,
        ILogger<TenantKeyController> logger)
    {
        _dbFactory = dbFactory;
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    // GET /api/tenant/key/keys
    [HttpGet("keys")]
    public async Task<IActionResult> GetKeys(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Keys.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(k => k.Status.ToString() == status);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(k => k.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(k => new
                {
                    id = k.Id,
                    keyNumber = k.KeyNumber,
                    name = k.Name,
                    description = k.Description ?? "",
                    location = k.Location ?? "",
                    roomNumber = k.RoomNumber ?? "",
                    status = k.Status.ToString(),
                    totalCopies = k.TotalCopies,
                    availableCopies = k.AvailableCopies,
                    storageLocation = k.StorageLocation ?? "",
                    remarks = k.Remarks ?? ""
                })
                .ToListAsync();

            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetKeys failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/key/keys
    [HttpPost("keys")]
    public async Task<IActionResult> CreateKey([FromBody] CreateKeyRequest request)
    {
        try
        {
            using var db = CreateDbContext();

            var key = new Key
            {
                KeyNumber = request.KeyNumber,
                Name = request.Name,
                Description = request.Description,
                Location = request.Location,
                RoomNumber = request.RoomNumber,
                Status = KeyStatus.Available,
                TotalCopies = request.TotalCopies,
                AvailableCopies = request.TotalCopies,
                StorageLocation = request.StorageLocation,
                Remarks = request.Remarks,
                CreatedAt = DateTime.UtcNow
            };

            db.Keys.Add(key);
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "钥匙登记成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateKey failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/key/borrows
    [HttpGet("borrows")]
    public async Task<IActionResult> GetBorrows(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.KeyBorrows.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.Status.ToString() == status);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(b => b.BorrowDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new
                {
                    id = b.Id,
                    borrowNumber = b.BorrowNumber,
                    keyId = b.KeyId,
                    borrowerName = b.BorrowerName,
                    borrowerPhone = b.BorrowerPhone ?? "",
                    borrowerUnit = b.BorrowerUnit ?? "",
                    borrowDate = b.BorrowDate,
                    expectedReturnDate = b.ExpectedReturnDate,
                    actualReturnDate = b.ActualReturnDate,
                    status = b.Status.ToString(),
                    purpose = b.Purpose ?? "",
                    approver = b.Approver ?? "",
                    approvedDate = b.ApprovedDate,
                    keyReturned = b.KeyReturned,
                    keyConditionOk = b.KeyConditionOk
                })
                .ToListAsync();

            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBorrows failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/key/borrows
    [HttpPost("borrows")]
    public async Task<IActionResult> CreateBorrow([FromBody] CreateBorrowRequest request)
    {
        try
        {
            using var db = CreateDbContext();

            var borrowNumber = $"KB-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            var borrow = new KeyBorrow
            {
                BorrowNumber = borrowNumber,
                KeyId = request.KeyId,
                BorrowerName = request.BorrowerName,
                BorrowerPhone = request.BorrowerPhone,
                BorrowerUnit = request.BorrowerUnit,
                BorrowDate = DateTime.UtcNow,
                ExpectedReturnDate = request.ExpectedReturnDate,
                Status = BorrowStatus.Pending,
                Purpose = request.Purpose,
                CreatedAt = DateTime.UtcNow
            };

            db.KeyBorrows.Add(borrow);
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "借钥匙申请已提交", data = new { borrowNumber } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateBorrow failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // PUT /api/tenant/key/borrows/{id}/status
    [HttpPut("borrows/{id}/status")]
    public async Task<IActionResult> UpdateBorrowStatus(int id, [FromBody] UpdateBorrowStatusRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var borrow = await db.KeyBorrows.FindAsync(id);
            if (borrow == null)
                return NotFound(new { success = false, message = "借钥匙记录不存在" });

            if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<BorrowStatus>(request.Status, out var newStatus))
                borrow.Status = newStatus;
            if (!string.IsNullOrEmpty(request.Approver))
            {
                borrow.Approver = request.Approver;
                borrow.ApprovedDate = DateTime.UtcNow;
            }
            if (request.ActualReturnDate.HasValue)
                borrow.ActualReturnDate = request.ActualReturnDate;
            if (request.KeyReturned.HasValue)
            {
                borrow.KeyReturned = request.KeyReturned.Value;
                if (request.KeyReturned.Value)
                    borrow.Status = BorrowStatus.Returned;
            }

            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "状态更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateBorrowStatus failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

public class CreateKeyRequest
{
    public string KeyNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? RoomNumber { get; set; }
    public int TotalCopies { get; set; } = 1;
    public string? StorageLocation { get; set; }
    public string? Remarks { get; set; }
}

public class CreateBorrowRequest
{
    public int KeyId { get; set; }
    public string BorrowerName { get; set; } = "";
    public string? BorrowerPhone { get; set; }
    public string? BorrowerUnit { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public string? Purpose { get; set; }
}

public class UpdateBorrowStatusRequest
{
    public string? Status { get; set; }
    public string? Approver { get; set; }
    public DateTime? ActualReturnDate { get; set; }
    public bool? KeyReturned { get; set; }
}