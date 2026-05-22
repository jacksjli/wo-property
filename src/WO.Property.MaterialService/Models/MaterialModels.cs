using WO.Property.Shared.Models;

namespace WO.Property.MaterialService.Models;

public enum StockStatus { Normal, Low, OutOfStock, Overstock }

public class Material : BaseEntity
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
    public DateTime? PurchaseDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? Remark { get; set; }
}

public class MaterialCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class StockTransaction : BaseEntity
{
    public int MaterialId { get; set; }
    public string TransactionType { get; set; } = string.Empty; // In, Out, Adjust
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Operator { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierContact { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Approved, Received, Cancelled
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public string? Notes { get; set; }
}