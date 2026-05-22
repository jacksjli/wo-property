using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WO.Property.Shared.Models;

namespace WO.Property.StatisticsService.Models;

public enum ReportType { Daily, Weekly, Monthly, Quarterly, Yearly }
public enum ReportCategory { Operation, Financial, Maintenance, Security, Customer, Comprehensive }

public class Report : BaseEntity
{
    [Required][MaxLength(100)] public string ReportNumber { get; set; } = string.Empty;
    [Required][MaxLength(200)] public string Title { get; set; } = string.Empty;
    public ReportType Type { get; set; }
    public ReportCategory Category { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    [Required] public string Data { get; set; } = "{}";
    [MaxLength(2000)] public string? Summary { get; set; }
    [Required][MaxLength(100)] public string GeneratedBy { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(100)] public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    [MaxLength(500)] public string? Remarks { get; set; }
}

public class MetricSnapshot : BaseEntity
{
    public DateTime SnapshotDate { get; set; }
    public int TotalProperties { get; set; }
    public int TotalUnits { get; set; }
    public int OccupiedUnits { get; set; }
    public decimal OccupancyRate { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal PropertyFeeRevenue { get; set; }
    public decimal OtherRevenue { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal CollectionRate { get; set; }
    public int TotalComplaints { get; set; }
    public int ResolvedComplaints { get; set; }
    public decimal ComplaintResolveRate { get; set; }
    public int TotalSuggestions { get; set; }
    public int TotalTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int TotalInspections { get; set; }
    public int PassedInspections { get; set; }
    public int IssuesFound { get; set; }
    public int IssuesResolved { get; set; }
    public int TotalVisitors { get; set; }
    public int ActiveVisitors { get; set; }
    public int TotalKeys { get; set; }
    public int BorrowedKeys { get; set; }
}

public class DashboardConfig : BaseEntity
{
    [Required][MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Required] public string Layout { get; set; } = "[]";
    [Required] public string Metrics { get; set; } = "[]";
}

public class TrendRecord : BaseEntity
{
    [Required][MaxLength(50)] public string MetricName { get; set; } = string.Empty;
    public DateTime RecordDate { get; set; }
    public decimal Value { get; set; }
    [MaxLength(100)] public string? Category { get; set; }
    [MaxLength(200)] public string? Remarks { get; set; }
}
public class MetricSnapshotDto
{
    public int TotalProperties { get; set; }
    public int TotalUnits { get; set; }
    public decimal OccupancyRate { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal PropertyFeeRevenue { get; set; }
    public decimal CollectionRate { get; set; }
    public decimal TotalExpense { get; set; }
    public int TotalComplaints { get; set; }
    public int ResolvedComplaints { get; set; }
    public decimal ComplaintResolveRate { get; set; }
    public int TotalTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int TotalInspections { get; set; }
    public int PassedInspections { get; set; }
    public int IssuesFound { get; set; }
    public int IssuesResolved { get; set; }
    public int TotalVisitors { get; set; }
    public int ActiveVisitors { get; set; }
}
