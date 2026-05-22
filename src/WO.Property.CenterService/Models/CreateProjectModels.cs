namespace WO.Property.CenterService.Models;

public class CreateProjectRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? ContactPhone { get; set; }
    public string? Config { get; set; }
}

public class CreateProjectResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public ProjectDto? Project { get; set; }
}