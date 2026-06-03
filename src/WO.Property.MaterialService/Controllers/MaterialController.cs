using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.MaterialService.Data;
using WO.Property.MaterialService.Models;
using Microsoft.AspNetCore.Authorization;

namespace WO.Property.MaterialService.Controllers;

[ApiController]
[Authorize]
[Route("api/tenant/materials")]
public class MaterialsController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ILogger<MaterialsController> _logger;

    public MaterialsController(IDbContextFactory<TenantDbContext> dbFactory, ILogger<MaterialsController> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    // GET /api/tenant/materials — 物资列表（分页+筛选）
    [HttpGet]
    public async Task<IActionResult> GetMaterials(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? category = null,
        [FromQuery] string? name = null,
        [FromQuery] string? status = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Materials.AsQueryable();

            if (!string.IsNullOrEmpty(category)) query = query.Where(m => m.Category == category);
            if (!string.IsNullOrEmpty(name)) query = query.Where(m => m.Name.Contains(name));
            if (!string.IsNullOrEmpty(status)) query = query.Where(m => m.Status == status);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    id = m.Id,
                    materialNo = m.MaterialNo,
                    name = m.Name,
                    category = m.Category,
                    spec = m.Spec ?? "",
                    unit = m.Unit,
                    quantity = m.Quantity,
                    minQuantity = m.MinQuantity,
                    price = m.Price,
                    location = m.Location ?? "",
                    status = m.Status,
                    supplier = m.Supplier ?? "",
                    remark = m.Remark ?? "",
                    createdAt = m.CreatedAt
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

    // POST /api/tenant/materials — 添加物资
    [HttpPost]
    public async Task<IActionResult> CreateMaterial([FromBody] CreateMaterialRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var material = new Material
            {
                MaterialNo = request.MaterialNo,
                Name = request.Name,
                Category = request.Category,
                Spec = request.Spec,
                Unit = request.Unit,
                Quantity = request.Quantity,
                MinQuantity = request.MinQuantity,
                Price = request.Price,
                Location = request.Location,
                Status = request.Status ?? "normal",
                Supplier = request.Supplier,
                Remark = request.Remark,
                CreatedAt = DateTime.UtcNow
            };
            db.Materials.Add(material);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "物料创建成功", data = new { id = material.Id, materialNo = material.MaterialNo } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateMaterial failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/materials/{id} — 物资详情
    [HttpGet("{id}")]
    public async Task<IActionResult> GetMaterial(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var m = await db.Materials.FindAsync(id);
            if (m == null) return Ok(new { success = false, message = "物料不存在" });
            return Ok(new
            {
                success = true,
                data = new
                {
                    id = m.Id,
                    materialNo = m.MaterialNo,
                    name = m.Name,
                    category = m.Category,
                    spec = m.Spec ?? "",
                    unit = m.Unit,
                    quantity = m.Quantity,
                    minQuantity = m.MinQuantity,
                    price = m.Price,
                    location = m.Location ?? "",
                    status = m.Status,
                    supplier = m.Supplier ?? "",
                    remark = m.Remark ?? "",
                    createdAt = m.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMaterial failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // PUT /api/tenant/materials/{id} — 更新物资
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMaterial(int id, [FromBody] UpdateMaterialRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var material = await db.Materials.FindAsync(id);
            if (material == null) return Ok(new { success = false, message = "物料不存在" });

            if (!string.IsNullOrEmpty(request.MaterialNo)) material.MaterialNo = request.MaterialNo;
            if (!string.IsNullOrEmpty(request.Name)) material.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Category)) material.Category = request.Category;
            if (request.Spec != null) material.Spec = request.Spec;
            if (!string.IsNullOrEmpty(request.Unit)) material.Unit = request.Unit;
            if (request.Quantity.HasValue) material.Quantity = request.Quantity.Value;
            if (request.MinQuantity.HasValue) material.MinQuantity = request.MinQuantity.Value;
            if (request.Price.HasValue) material.Price = request.Price.Value;
            if (request.Location != null) material.Location = request.Location;
            if (!string.IsNullOrEmpty(request.Status)) material.Status = request.Status;
            if (request.Supplier != null) material.Supplier = request.Supplier;
            if (request.Remark != null) material.Remark = request.Remark;

            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "物料更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateMaterial failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // DELETE /api/tenant/materials/{id} — 删除物资
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMaterial(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var material = await db.Materials.FindAsync(id);
            if (material == null) return Ok(new { success = false, message = "物料不存在" });

            db.Materials.Remove(material);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "物料删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteMaterial failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/materials/{id}/instock — 入库
    [HttpPost("{id}/instock")]
    public async Task<IActionResult> InStock(int id, [FromBody] StockOperationRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var material = await db.Materials.FindAsync(id);
            if (material == null) return Ok(new { success = false, message = "物料不存在" });

            material.Quantity += request.Quantity;
            var transaction = new StockTransaction
            {
                MaterialId = id,
                TransactionType = "In",
                Quantity = request.Quantity,
                UnitPrice = material.Price,
                TotalAmount = request.Quantity * material.Price,
                Operator = request.Operator,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };
            db.StockTransactions.Add(transaction);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "入库成功", newStock = material.Quantity });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InStock failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/materials/{id}/outstock — 出库
    [HttpPost("{id}/outstock")]
    public async Task<IActionResult> OutStock(int id, [FromBody] StockOperationRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var material = await db.Materials.FindAsync(id);
            if (material == null) return Ok(new { success = false, message = "物料不存在" });
            if (material.Quantity < request.Quantity)
                return Ok(new { success = false, message = "库存不足" });

            material.Quantity -= request.Quantity;
            var transaction = new StockTransaction
            {
                MaterialId = id,
                TransactionType = "Out",
                Quantity = request.Quantity,
                UnitPrice = material.Price,
                TotalAmount = request.Quantity * material.Price,
                Operator = request.Operator,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };
            db.StockTransactions.Add(transaction);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "出库成功", newStock = material.Quantity });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OutStock failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/materials/stock-record — 进出库记录
    [HttpGet("stock-record")]
    public async Task<IActionResult> GetStockRecords(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? materialId = null,
        [FromQuery] string? transactionType = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.StockTransactions.AsQueryable();

            if (materialId.HasValue) query = query.Where(t => t.MaterialId == materialId.Value);
            if (!string.IsNullOrEmpty(transactionType)) query = query.Where(t => t.TransactionType == transactionType);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return Ok(new { success = true, total, page, pageSize, data = items });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetStockRecords failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/materials/low-stock — 库存预警
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockMaterials()
    {
        try
        {
            using var db = CreateDbContext();
            var items = await db.Materials
                .Where(m => m.Quantity <= m.MinQuantity)
                .Select(m => new
                {
                    id = m.Id,
                    materialNo = m.MaterialNo,
                    name = m.Name,
                    category = m.Category,
                    unit = m.Unit,
                    quantity = m.Quantity,
                    minQuantity = m.MinQuantity,
                    price = m.Price
                })
                .ToListAsync();
            return Ok(new { success = true, data = items, total = items.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetLowStockMaterials failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

// ============ Request DTOs ============
public class CreateMaterialRequest
{
    public string MaterialNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int MinQuantity { get; set; }
    public decimal Price { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
    public string? Supplier { get; set; }
    public string? Remark { get; set; }
}

public class UpdateMaterialRequest
{
    public string? MaterialNo { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? Spec { get; set; }
    public string? Unit { get; set; }
    public int? Quantity { get; set; }
    public int? MinQuantity { get; set; }
    public decimal? Price { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
    public string? Supplier { get; set; }
    public string? Remark { get; set; }
}

public class StockOperationRequest
{
    public int Quantity { get; set; }
    public string? Operator { get; set; }
    public string? Notes { get; set; }
}