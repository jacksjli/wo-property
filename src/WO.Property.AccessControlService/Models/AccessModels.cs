using System.ComponentModel.DataAnnotations;
using WO.Property.Shared.Models;

namespace WO.Property.AccessControlService.Models;

public class AccessCard : BaseEntity
{
    [Required, MaxLength(50)]
    public string CardNo { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? CardUid { get; set; }

    [MaxLength(100)]
    public string? OwnerName { get; set; }

    [MaxLength(30)]
    public string? OwnerPhone { get; set; }

    [MaxLength(20)]
    public string Type { get; set; } = "owner";

    [MaxLength(20)]
    public string Status { get; set; } = "active";

    public int? BuildingId { get; set; }

    [MaxLength(20)]
    public string? Floor { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? IssueDate { get; set; }
    public DateTime? ExpireDate { get; set; }

    public int ProjectId { get; set; } = 1;

    public Building? Building { get; set; }
}

public class Building : BaseEntity
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int ProjectId { get; set; } = 1;
}

public class AccessDoor : BaseEntity
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int? BuildingId { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "online";

    [MaxLength(100)]
    public string? DeviceModel { get; set; }

    public DateTime? LastOnlineAt { get; set; }
}

public class TempAccessCode : BaseEntity
{
    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? VisitorName { get; set; }

    public int? BuildingId { get; set; }

    [MaxLength(50)]
    public string? IssuedTo { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime ExpiresAt { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "active";

    public int UseCount { get; set; } = 0;

    public Building? Building { get; set; }
}

public class AccessLog : BaseEntity
{
    [MaxLength(50)]
    public string? CardId { get; set; }

    [MaxLength(100)]
    public string? CardNo { get; set; }

    [MaxLength(100)]
    public string? PersonName { get; set; }

    public int? DoorId { get; set; }
    public int? BuildingId { get; set; }

    [MaxLength(20)]
    public string AccessType { get; set; } = "entry";

    [MaxLength(20)]
    public string Result { get; set; } = "success";

    [MaxLength(200)]
    public string? Remark { get; set; }

    public DateTime AccessTime { get; set; } = DateTime.UtcNow;

    public AccessDoor? Door { get; set; }
    public Building? Building { get; set; }
}