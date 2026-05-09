using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WO.Property.Shared.Models;

namespace WO.Property.KeyService.Models;

public enum KeyStatus { Available, Borrowed, Lost, Damaged, Retired }
public enum BorrowStatus { Pending, Approved, Borrowed, Returned, Rejected, Cancelled }

public class Key : BaseEntity
{
    [Required][MaxLength(50)] public string KeyNumber { get; set; } = string.Empty;
    [Required][MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(100)] public string? Description { get; set; }
    [MaxLength(100)] public string? Location { get; set; }
    [MaxLength(50)] public string? RoomNumber { get; set; }
    public KeyStatus Status { get; set; } = KeyStatus.Available;
    public int TotalCopies { get; set; } = 1;
    public int AvailableCopies { get; set; } = 1;
    [MaxLength(100)] public string? StorageLocation { get; set; }
    [MaxLength(200)] public string? Remarks { get; set; }
}

public class KeyBorrow : BaseEntity
{
    [Required][MaxLength(50)] public string BorrowNumber { get; set; } = string.Empty;
    [Required] public int KeyId { get; set; }
    [ForeignKey(nameof(KeyId))] public Key? Key { get; set; }
    [Required][MaxLength(100)] public string BorrowerName { get; set; } = string.Empty;
    [MaxLength(50)] public string? BorrowerPhone { get; set; }
    [MaxLength(100)] public string? BorrowerUnit { get; set; }
    public DateTime BorrowDate { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public DateTime? ActualReturnDate { get; set; }
    public BorrowStatus Status { get; set; } = BorrowStatus.Pending;
    [MaxLength(100)] public string? Purpose { get; set; }
    [MaxLength(100)] public string? Approver { get; set; }
    public DateTime? ApprovedDate { get; set; }
    [MaxLength(200)] public string? ApprovedRemarks { get; set; }
    [MaxLength(100)] public string? ReturnReceiver { get; set; }
    [MaxLength(200)] public string? ReturnRemarks { get; set; }
    public bool KeyReturned { get; set; } = false;
    public bool KeyConditionOk { get; set; } = true;
    [MaxLength(200)] public string? KeyConditionRemarks { get; set; }
}

public class KeyUsageLog : BaseEntity
{
    [Required] public int KeyId { get; set; }
    [ForeignKey(nameof(KeyId))] public Key? Key { get; set; }
    public int? BorrowId { get; set; }
    [Required][MaxLength(50)] public string Action { get; set; } = string.Empty;
    [MaxLength(100)] public string Operator { get; set; } = string.Empty;
    [MaxLength(200)] public string? Remarks { get; set; }
    public DateTime ActionTime { get; set; } = DateTime.UtcNow;
}