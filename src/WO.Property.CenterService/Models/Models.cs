namespace WO.Property.CenterService.Models;

public class Project
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public string? Config { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? ContactPhone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public string Status { get; set; } = "active";
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProjectMember
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string Role { get; set; } = "viewer";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public Project? Project { get; set; }
    public User? User { get; set; }
}

public class ProjectModule
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string ModuleKey { get; set; } = string.Empty;
    public string? Config { get; set; }
    public int SortOrder { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // Navigation
    public Project? Project { get; set; }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public UserDto? User { get; set; }
    public List<ProjectDto>? Projects { get; set; }
    public string? DefaultProject { get; set; }
    public string? Message { get; set; }
}

public class UserDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
}

public class ProjectDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
}

public class SwitchProjectRequest
{
    public string ProjectCode { get; set; } = string.Empty;
}

public class SwitchProjectResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public ProjectDto? Project { get; set; }
    public string? Message { get; set; }
}