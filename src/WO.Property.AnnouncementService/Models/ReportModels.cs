public class DeviceReport
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public string? MaintenanceType { get; set; }
    public decimal? MaintenanceCost { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TicketReport
{
    public long Id { get; set; }
    public string TicketNumber { get; set; } = "";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public string Priority { get; set; } = "";
    public int? AssignedTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class MaterialReport
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int CurrentStock { get; set; }
    public int SafetyStock { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SatisfactionSurvey
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public int? Rating { get; set; }
    public string? Comment { get; set; }
    public string? RespondentName { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public class PurchaseOrder
{
    public long Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public string Supplier { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class StockTransaction
{
    public long Id { get; set; }
    public int MaterialId { get; set; }
    public string TransactionType { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Operator { get; set; }
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class EnumDefinition
{
    public int Id { get; set; }
    public string Category { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class GeneralReport
{
    public int Id { get; set; }
    public string ReportNumber { get; set; } = "";
    public string? Title { get; set; }
    public string? Type { get; set; }
    public string? Category { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Data { get; set; }
    public string? Summary { get; set; }
    public string? GeneratedBy { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
