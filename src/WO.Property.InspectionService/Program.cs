using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.InspectionService.Data;
using WO.Property.InspectionService.Models;
using InspectionTaskStatus = WO.Property.InspectionService.Models.TaskStatus;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 配置端口 - 使用5010端口
builder.WebHost.UseUrls("http://0.0.0.0:5010");

// 添加数据库
builder.Services.AddDbContext<InspectionDbContext>(options =>
    options.UseMySql("Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4", ServerVersion.Parse("8.0.35")));

// JWT 配置 - 使用统一认证配置
var jwtIssuer = "wo-property-unified-auth";
var jwtAudience = "wo-property-services";
var jwtSecretKey = JwtHelper.GetSecretKey();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
        };
    });

builder.Services.AddAuthorization();

// 添加控制器
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

var app = builder.Build();

// 数据库初始化

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 健康检查
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "InspectionService", timestamp = DateTime.UtcNow }));

Console.WriteLine("===========================================");
Console.WriteLine("  WO Property Inspection Service");
Console.WriteLine("  Port: 5010");
Console.WriteLine("===========================================");

app.Run();

// API 控制器
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlansController : ControllerBase
{
    private readonly InspectionDbContext _context;
    
    public PlansController(InspectionDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetPlans([FromQuery] bool activeOnly = true)
    {
        var query = _context.Plans.AsQueryable();
        if (activeOnly)
            query = query.Where(p => p.IsActive);
        
        var plans = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return Ok(new { success = true, data = plans });
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPlan(int id)
    {
        var plan = await _context.Plans.FindAsync(id);
        if (plan == null)
            return NotFound(new { success = false, message = "巡检计划不存在" });
        return Ok(new { success = true, data = plan });
    }
    
    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] InspectionPlan plan)
    {
        plan.CreatedAt = DateTime.UtcNow;
        plan.UpdatedAt = DateTime.UtcNow;
        _context.Plans.Add(plan);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "巡检计划创建成功", data = plan });
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePlan(int id, [FromBody] InspectionPlan plan)
    {
        var existing = await _context.Plans.FindAsync(id);
        if (existing == null)
            return NotFound(new { success = false, message = "巡检计划不存在" });
        
        existing.Name = plan.Name;
        existing.Description = plan.Description;
        existing.Type = plan.Type;
        existing.Area = plan.Area;
        existing.TargetItems = plan.TargetItems;
        existing.IntervalDays = plan.IntervalDays;
        existing.NextExecutionDate = plan.NextExecutionDate;
        existing.AssignedTo = plan.AssignedTo;
        existing.EstimatedMinutes = plan.EstimatedMinutes;
        existing.IsActive = plan.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "巡检计划更新成功", data = existing });
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly InspectionDbContext _context;
    
    public TasksController(InspectionDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetTasks(
        [FromQuery] InspectionTaskStatus? status = null,
        [FromQuery] InspectionType? type = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Tasks.Include(t => t.Plan).AsQueryable();
        
        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);
        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);
        if (fromDate.HasValue)
            query = query.Where(t => t.ScheduledDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(t => t.ScheduledDate <= toDate.Value);
        
        var total = await query.CountAsync();
        var tasks = await query
            .OrderByDescending(t => t.ScheduledDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return Ok(new { success = true, total, page, pageSize, data = tasks });
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTask(int id)
    {
        var task = await _context.Tasks.Include(t => t.Plan).FirstOrDefaultAsync(t => t.Id == id);
        if (task == null)
            return NotFound(new { success = false, message = "巡检任务不存在" });
        return Ok(new { success = true, data = task });
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] InspectionTask task)
    {
        if (string.IsNullOrEmpty(task.TaskNumber))
        {
            var year = DateTime.Now.Year;
            var count = await _context.Tasks.CountAsync() + 1;
            task.TaskNumber = $"INS-{year}-{count:D4}";
        }
        
        task.CreatedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "巡检任务创建成功", data = task });
    }
    
    [HttpPut("{id}/execute")]
    public async Task<IActionResult> ExecuteTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
            return NotFound(new { success = false, message = "巡检任务不存在" });
        
        task.Status = InspectionTaskStatus.InProgress;
        task.StartTime = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "巡检任务已开始", data = task });
    }
    
    [HttpPut("{id}/complete")]
    public async Task<IActionResult> CompleteTask(int id, [FromBody] CompleteTaskRequest request)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
            return NotFound(new { success = false, message = "巡检任务不存在" });
        
        task.Status = InspectionTaskStatus.Completed;
        task.EndTime = DateTime.UtcNow;
        task.CompletedBy = request.CompletedBy;
        task.Remarks = request.Remarks;
        task.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "巡检任务已完成", data = task });
    }
    
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekLater = today.AddDays(7);
        
        var stats = new
        {
            total = await _context.Tasks.CountAsync(),
            pending = await _context.Tasks.CountAsync(t => t.Status == InspectionTaskStatus.Pending),
            inProgress = await _context.Tasks.CountAsync(t => t.Status == InspectionTaskStatus.InProgress),
            completed = await _context.Tasks.CountAsync(t => t.Status == InspectionTaskStatus.Completed),
            overdue = await _context.Tasks.CountAsync(t => t.Status == InspectionTaskStatus.Overdue),
            todayTasks = await _context.Tasks.CountAsync(t => t.ScheduledDate.Date == today),
            weekTasks = await _context.Tasks.CountAsync(t => t.ScheduledDate >= today && t.ScheduledDate <= weekLater),
            openIssues = await _context.Issues.CountAsync(i => i.Status == IssueStatus.Open || i.Status == IssueStatus.InProgress)
        };
        
        return Ok(new { success = true, data = stats });
    }
}

public class CompleteTaskRequest
{
    public string? CompletedBy { get; set; }
    public string? Remarks { get; set; }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecordsController : ControllerBase
{
    private readonly InspectionDbContext _context;
    
    public RecordsController(InspectionDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetRecords(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Records.Include(r => r.Task).AsQueryable();
        
        if (fromDate.HasValue)
            query = query.Where(r => r.InspectionDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(r => r.InspectionDate <= toDate.Value);
        
        var total = await query.CountAsync();
        var records = await query
            .OrderByDescending(r => r.InspectionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return Ok(new { success = true, total, page, pageSize, data = records });
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRecord(int id)
    {
        var record = await _context.Records.Include(r => r.Task).FirstOrDefaultAsync(r => r.Id == id);
        if (record == null)
            return NotFound(new { success = false, message = "巡检记录不存在" });
        
        var issues = await _context.Issues.Where(i => i.RecordId == id).ToListAsync();
        
        return Ok(new { success = true, data = new { record, issues } });
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] CreateRecordRequest request)
    {
        var year = DateTime.Now.Year;
        var count = await _context.Records.CountAsync() + 1;
        
        var record = new InspectionRecord
        {
            TaskId = request.TaskId,
            RecordNumber = $"REC-{year}-{count:D4}",
            InspectionDate = request.InspectionDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Inspector = request.Inspector,
            TotalItems = request.TotalItems,
            PassedItems = request.PassedItems,
            FailedItems = request.FailedItems,
            Findings = request.Findings,
            Suggestions = request.Suggestions,
            IsPassed = request.IsPassed,
            Attachments = request.Attachments,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Records.Add(record);
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "巡检记录创建成功", data = record });
    }
}

public class CreateRecordRequest
{
    public int TaskId { get; set; }
    public DateTime InspectionDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Inspector { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int PassedItems { get; set; }
    public int FailedItems { get; set; }
    public string? Findings { get; set; }
    public string? Suggestions { get; set; }
    public bool IsPassed { get; set; }
    public string? Attachments { get; set; }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IssuesController : ControllerBase
{
    private readonly InspectionDbContext _context;
    
    public IssuesController(InspectionDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetIssues(
        [FromQuery] IssueStatus? status = null,
        [FromQuery] IssueSeverity? severity = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Issues.AsQueryable();
        
        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);
        if (severity.HasValue)
            query = query.Where(i => i.Severity == severity.Value);
        
        var total = await query.CountAsync();
        var issues = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return Ok(new { success = true, total, page, pageSize, data = issues });
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetIssue(int id)
    {
        var issue = await _context.Issues.FindAsync(id);
        if (issue == null)
            return NotFound(new { success = false, message = "巡检问题不存在" });
        return Ok(new { success = true, data = issue });
    }
    
    [HttpPut("{id}/resolve")]
    public async Task<IActionResult> ResolveIssue(int id, [FromBody] ResolveIssueRequest request)
    {
        var issue = await _context.Issues.FindAsync(id);
        if (issue == null)
            return NotFound(new { success = false, message = "巡检问题不存在" });
        
        issue.Status = IssueStatus.Resolved;
        issue.Solution = request.Solution;
        issue.ResolvedDate = DateTime.UtcNow;
        issue.ResolvedBy = request.ResolvedBy;
        issue.Remarks = request.Remarks;
        issue.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "问题已解决", data = issue });
    }
}

public class ResolveIssueRequest
{
    public string Solution { get; set; } = string.Empty;
    public string? ResolvedBy { get; set; }
    public string? Remarks { get; set; }
}
