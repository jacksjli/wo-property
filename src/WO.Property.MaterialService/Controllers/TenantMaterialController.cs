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
    public async Task<IActionResult> GetMaterials([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? category = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Materials.AsQueryable();
            if (!string.IsNullOrEmpty(category))
                query = query.Where(m => m.Category == category);
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

    // POST /api/tenant/material/materials
    [HttpPost("materials")]
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
                Status = request.Status,
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

    // PUT /api/tenant/material/materials/{id}
    [HttpPut("materials/{id}")]
    public async Task<IActionResult> UpdateMaterial(int id, [FromBody] UpdateMaterialRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var material = await db.Materials.FindAsync(id);
            if (material == null)
                return Ok(new { success = false, message = "物料不存在" });

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

    // DELETE /api/tenant/material/materials/{id}
    [HttpDelete("materials/{id}")]
    public async Task<IActionResult> DeleteMaterial(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var material = await db.Materials.FindAsync(id);
            if (material == null)
                return Ok(new { success = false, message = "物料不存在" });

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

    // POST /api/tenant/material/stock-in
    [HttpPost("stock-in")]
    public async Task<IActionResult> StockIn([FromBody] StockOperationRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var material = await db.Materials.FindAsync(request.MaterialId);
            if (material == null) return NotFound(new { success = false, message = "物料不存在" });

            material.Quantity += request.Quantity;
            var transaction = new StockTransaction
            {
                MaterialId = request.MaterialId,
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
            if (material.Quantity < request.Quantity)
                return Ok(new { success = false, message = "库存不足" });

            material.Quantity -= request.Quantity;
            var transaction = new StockTransaction
            {
                MaterialId = request.MaterialId,
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
                .ToListAsync();
            return Ok(new { success = true, total, page, pageSize, data = items });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTransactions failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // ==================== 物料分类管理 ====================
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            using var db = CreateDbContext();
            var categories = await db.MaterialCategories
                .Where(c => c.IsDeleted == false)
                .OrderBy(c => c.Code)
                .ToListAsync();
            return Ok(new { success = true, data = categories });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCategories failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var category = new MaterialCategory
            {
                Name = request.Name,
                Code = request.Code,
                Description = request.Description ?? "",
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            };
            db.MaterialCategories.Add(category);
            await db.SaveChangesAsync();
            return Ok(new { success = true, data = category });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateCategory failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("categories/{id}")]
    public async Task<IActionResult> UpdateCategory(long id, [FromBody] UpdateCategoryRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var category = await db.MaterialCategories.FindAsync(id);
            if (category == null)
                return NotFound(new { success = false, message = "分类不存在" });
            
            if (!string.IsNullOrEmpty(request.Name)) category.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Code)) category.Code = request.Code;
            if (request.Description != null) category.Description = request.Description;
            category.UpdatedBy = "system";
            category.UpdatedAt = DateTime.UtcNow;
            
            await db.SaveChangesAsync();
            return Ok(new { success = true, data = category });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateCategory failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(long id)
    {
        try
        {
            using var db = CreateDbContext();
            var category = await db.MaterialCategories.FindAsync(id);
            if (category == null)
                return NotFound(new { success = false, message = "分类不存在" });
            
            category.IsDeleted = true;
            category.UpdatedBy = "system";
            category.UpdatedAt = DateTime.UtcNow;
            
            await db.SaveChangesAsync();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteCategory failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateCategoryRequest
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
}

public class StockOperationRequest
{
    public int MaterialId { get; set; }
    public int Quantity { get; set; }
    public string? Operator { get; set; }
    public string? Notes { get; set; }
}

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
    public string Status { get; set; } = "normal";
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
