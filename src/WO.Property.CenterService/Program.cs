using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WO.Property.CenterService.Data;
using WO.Property.CenterService.Models;
using WO.Property.CenterService.Services;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

// CORS - 允许前端开发服务器访问
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=127.0.0.1;Port=3306;Database=project_center;User=root;Password=;CharSet=utf8mb4;";
builder.Services.AddDbContext<CenterDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// JWT
var jwtKey = JwtHelper.GetSecretKey();
var jwtIssuer = JwtHelper.GetIssuer();
var jwtAudience = JwtHelper.GetAudience();
builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<ProjectDatabaseService>();

var app = builder.Build();

app.UseCors("AllowFrontend");

app.MapGet("/health", () => new { service = "WO.Property.CenterService", status = "healthy" });

app.MapPost("/api/auth/login", async (LoginRequest request, CenterDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username && u.Status == "active");
    if (user == null)
    {
        return Results.Json(new LoginResponse { Success = false, Message = "用户不存在" });
    }

    // 验证密码 (临时明文验证，后续用 BCrypt)
    if (user.PasswordHash != request.Password)
    {
        return Results.Json(new LoginResponse { Success = false, Message = "密码错误" });
    }

    // 更新最后登录时间
    user.LastLoginAt = DateTime.Now;
    await db.SaveChangesAsync();

    // 获取用户的项目列表
    // admin 用户(id=1)可以看到所有项目，普通用户只看自己关联的项目
    List<ProjectDto> projects;
    if (user.Id == 1)  // admin 用户
    {
        projects = await db.Projects
            .Where(p => p.Status == "active")
            .Select(p => new ProjectDto
            {
                Code = p.Code,
                Name = p.Name,
                DatabaseName = p.DatabaseName,
                Status = p.Status
            })
            .ToListAsync();
    }
    else
    {
        projects = await db.ProjectMembers
            .Where(pm => pm.UserId == user.Id)
            .Include(pm => pm.Project)
            .Select(pm => new ProjectDto
            {
                Code = pm.Project!.Code,
                Name = pm.Project.Name,
                DatabaseName = pm.Project.DatabaseName,
                Status = pm.Project.Status
            })
            .ToListAsync();
    }
    
    // 如果用户没有任何项目关联，给出友好提示（针对非admin用户）
    if (projects.Count == 0 && user.Id != 1)
    {
        return Results.Json(new LoginResponse { Success = false, Message = "该账号没有可以访问的项目，请联系管理员分配" });
    }

    // 生成 JWT
    var claims = new[]
    {
        new Claim("user_id", user.Id.ToString()),
        new Claim("username", user.Username),
        new Claim("project_code", projects.FirstOrDefault()?.Code ?? "")
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: DateTime.Now.AddDays(7),
        signingCredentials: creds
    );

    return Results.Json(new LoginResponse
    {
        Success = true,
        Token = new JwtSecurityTokenHandler().WriteToken(token),
        User = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Phone = user.Phone
        },
        Projects = projects,
        DefaultProject = projects.FirstOrDefault()?.Code
    });
});

app.MapGet("/api/projects", async (CenterDbContext db) =>
{
    var projects = await db.Projects.Where(p => p.Status == "active").ToListAsync();
    return Results.Json(new { success = true, data = projects });
});

app.MapGet("/api/projects/{code}", async (string code, CenterDbContext db) =>
{
    var project = await db.Projects.FirstOrDefaultAsync(p => p.Code == code);
    if (project == null) return Results.NotFound();
    return Results.Json(new { success = true, data = project });
});

app.MapGet("/api/members/{userId}", async (long userId, CenterDbContext db) =>
{
    var members = await db.ProjectMembers
        .Where(pm => pm.UserId == userId)
        .Include(pm => pm.Project)
        .ToListAsync();
    return Results.Json(new { success = true, data = members });
});

// POST /api/projects - 创建新项目
app.MapPost("/api/projects", async (CreateProjectRequest request, CenterDbContext db, ProjectDatabaseService dbService) =>
{
    // 验证请求
    if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.Json(new CreateProjectResponse { Success = false, Message = "项目代码和名称不能为空" });
    }

    // 检查项目代码是否已存在
    var existing = await db.Projects.FirstOrDefaultAsync(p => p.Code == request.Code);
    if (existing != null)
    {
        return Results.Json(new CreateProjectResponse { Success = false, Message = $"项目代码 {request.Code} 已存在" });
    }

    // 创建项目记录
    var project = new Project
    {
        Code = request.Code.ToLower(),
        Name = request.Name,
        DatabaseName = $"project_{request.Code.ToLower()}",
        Status = "active",
        Description = request.Description,
        Address = request.Address,
        ContactPhone = request.ContactPhone,
        Config = request.Config,
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now
    };

    db.Projects.Add(project);
    await db.SaveChangesAsync();

    // 创建项目数据库（异步，不阻塞响应）
    _ = Task.Run(async () =>
    {
        Console.WriteLine($"[ProjectDatabase] 开始创建数据库 project_{request.Code}");
        var (success, message) = await dbService.CreateProjectDatabaseAsync(request.Code);
        Console.WriteLine($"[ProjectDatabase] 结果: {message}");
    });

    return Results.Json(new CreateProjectResponse
    {
        Success = true,
        Message = $"项目 {request.Name} 创建成功，数据库正在初始化中",
        Project = new ProjectDto
        {
            Code = project.Code,
            Name = project.Name,
            DatabaseName = project.DatabaseName,
            Status = project.Status
        }
    });
});

app.MapControllers();

// GET /api/projects/{code}/modules - 获取项目模块配置
app.MapGet("/api/projects/{code}/modules", async (string code, CenterDbContext db) =>
{
    var project = await db.Projects.FirstOrDefaultAsync(p => p.Code == code);
    if (project == null) return Results.NotFound(new { success = false, message = "项目不存在" });
    
    var modules = await db.ProjectModules
        .Where(m => m.ProjectId == project.Id)
        .ToListAsync();
    
    return Results.Json(new { success = true, data = modules.Select(m => m.ModuleKey) });
});

// PUT /api/projects/{code}/modules - 更新项目模块配置
app.MapPut("/api/projects/{code}/modules", async (string code, List<string> moduleKeys, CenterDbContext db) =>
{
    var project = await db.Projects.FirstOrDefaultAsync(p => p.Code == code);
    if (project == null) return Results.NotFound(new { success = false, message = "项目不存在" });
    
    // 删除旧的模块配置
    var oldModules = await db.ProjectModules.Where(m => m.ProjectId == project.Id).ToListAsync();
    db.ProjectModules.RemoveRange(oldModules);
    
    // 添加新的模块配置
    int sortOrder = 0;
    foreach (var moduleKey in moduleKeys)
    {
        db.ProjectModules.Add(new ProjectModule
        {
            ProjectId = project.Id,
            ModuleKey = moduleKey,
            SortOrder = sortOrder++,
            Status = "active"
        });
    }
    
    await db.SaveChangesAsync();
    return Results.Json(new { success = true, message = "模块配置已更新" });
});

// DELETE /api/projects/{code} - 删除项目
app.MapDelete("/api/projects/{code}", async (string code, CenterDbContext db, ProjectDatabaseService dbService) =>
{
    var project = await db.Projects.FirstOrDefaultAsync(p => p.Code == code);
    if (project == null)
    {
        return Results.Json(new { success = false, message = "项目不存在" });
    }
    
    var dbName = project.DatabaseName;
    
    // 删除数据库（异步）
    _ = Task.Run(async () =>
    {
        Console.WriteLine($"[ProjectDatabase] 开始删除数据库 {dbName}");
        var (success, message) = await dbService.DeleteProjectDatabaseAsync(code);
        Console.WriteLine($"[ProjectDatabase] 删除结果: {message}");
    });
    
    // 删除项目记录
    db.Projects.Remove(project);
    await db.SaveChangesAsync();
    
    return Results.Json(new { success = true, message = $"项目 {project.Name} 删除成功，数据库 {dbName} 正在删除中" });
});


app.Run();