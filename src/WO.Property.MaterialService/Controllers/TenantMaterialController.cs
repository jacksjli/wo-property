using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.MaterialService.Data;
using WO.Property.MaterialService.Models;

namespace WO.Property.MaterialService.Controllers;

[ApiController]
[Route("api/tenant/material")]
public class TenantMaterialController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ILogger<TenantMaterialController> _logger;

    public TenantMaterialController(IDbContextFactory<TenantDbContext> dbFactory, ILogger<TenantMaterialController> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    // GET /api/tenant/material/materials
    [HttpGet("materials")]
    public async Task<IActionResult> GetMaterials([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? categoryId = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Materials.AsQueryable();
            if (categoryId.HasValue)
                query = query.Where(m => m.CategoryId == categoryId.Value);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    id = m.Id,
                    code = m.Code,
                    name = m.Name,
                    description = m.Description ?? "",
                    categoryId = m.CategoryId,
                    unit = m.Unit,
                    unitPrice = m.UnitPrice,
                    safetyStock = m.SafetyStock,
                    maxStock = m.MaxStock,
                    currentStock = m.CurrentStock
                })
                .ToListAsync();
            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMaterials failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/material/materials
    [HttpPost("materials")]
    public async Task<IActionResult> CreateMaterial([FromBody] CreateMaterialRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var material = new Material
            {
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                CategoryId = request.CategoryId,
                Unit = request.Unit,
                UnitPrice = request.UnitPrice,
                SafetyStock = request.SafetyStock,
                MaxStock = request.MaxStock,
                CurrentStock = request.CurrentStock,
                CreatedAt = DateTime.UtcNow
            };
            db.Materials.Add(material);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "物料创建成功", data = new { id = material.Id, code = material.Code } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateMaterial failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/material/stock-in
    [HttpPost("stock-in")]
    public async Task<IActionResult> StockIn([FromBody] StockOperationRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var material = await db.Materials.FindAsync(request.MaterialId);
            if (material == null) return NotFound(new { success = false, message = "物料不存在" });

            material.CurrentStock += request.Quantity;
            var transaction = new StockTransaction
            {
                MaterialId = request.MaterialId,
                TransactionType = "In",
                Quantity = request.Quantity,
                UnitPrice = material.UnitPrice,
                TotalAmount = request.Quantity * material.UnitPrice,
                Operator = request.Operator,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };
            db.StockTransactions.Add(transaction);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "入库成功", newStock = material.CurrentStock });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StockIn failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/material/stock-out
    [HttpPost("stock-out")]
    public async Task<IActionResult> StockOut([FromBody] StockOperationRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var material = await db.Materials.FindAsync(request.MaterialId);
            if (material == null) return NotFound(new { success = false, message = "物料不存在" });
            if (material.CurrentStock < request.Quantity)
                return Ok(new { success = false, message = "库存不足" });

            material.CurrentStock -= request.Quantity;
            var transaction = new StockTransaction
            {
                MaterialId = request.MaterialId,
                TransactionType = "Out",
                Quantity = request.Quantity,
                UnitPrice = material.UnitPrice,
                TotalAmount = request.Quantity * material.UnitPrice,
                Operator = request.Operator,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };
            db.StockTransactions.Add(transaction);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "出库成功", newStock = material.CurrentStock });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StockOut failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/material/transactions
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? materialId = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.StockTransactions.AsQueryable();
            if (materialId.HasValue)
                query = query.Where(t => t.MaterialId == materialId.Value);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    id = t.Id,
                    materialId = t.MaterialId,
                    transactionType = t.TransactionType,
                    quantity = t.Quantity,
                    unitPrice = t.UnitPrice,
                    totalAmount = t.TotalAmount,
                    @operator = t.Operator ?? "",
                    notes = t.Notes ?? "",
                    createdAt = t.CreatedAt
                })
                .ToListAsync();
            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTransactions failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

public class CreateMaterialRequest
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string Unit { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int SafetyStock { get; set; }
    public int MaxStock { get; set; }
    public int CurrentStock { get; set; }
}

public class StockOperationRequest
{
    public int MaterialId { get; set; }
    public int Quantity { get; set; }
    public string? Operator { get; set; }
    public string? Notes { get; set; }
}