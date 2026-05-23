using System.Text.Json.Serialization;

namespace WO.Property.PersonService.Models;

public class ImportPersonsRequest
{
    [JsonPropertyName("rows")]
    public List<ImportPersonRow> Rows { get; set; } = new();
}

public class ImportPersonRow
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("employeeNo")]
    public string? EmployeeNo { get; set; }

    [JsonPropertyName("departmentName")]
    public string? DepartmentName { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("hireDate")]
    public string? HireDate { get; set; }

    [JsonPropertyName("ticketTypeIds")]
    public string? TicketTypeIds { get; set; }

    [JsonPropertyName("specialtyIds")]
    public string? SpecialtyIds { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}