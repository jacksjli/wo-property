using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.TicketService.Data;

namespace WO.Property.TicketService.Controllers;

[ApiController]
[Route("api/timeout")]
[Authorize]
public class TimeoutController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ILogger<TimeoutController> _logger;

    public TimeoutController(IDbContextFactory<TenantDbContext> dbFactory, ILogger<TimeoutController> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    // 获取所有超时规则
    [HttpGet("rules")]
    public async Task<IActionResult> GetRules()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var rules = await db.TimeoutRules.OrderBy(r => r.Color).ThenBy(r => r.Role).ToListAsync();
        return Ok(new { success = true, data = rules });
    }

    // 更新超时规则
    [HttpPut("rules/{id}")]
    public async Task<IActionResult> UpdateRule(int id, [FromBody] UpdateTimeoutRuleRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var rule = await db.TimeoutRules.FindAsync(id);
        if (rule == null)
            return NotFound(new { success = false, message = "规则不存在" });

        if (request.Hours.HasValue)
            rule.Hours = request.Hours.Value;
        if (request.Enabled.HasValue)
            rule.Enabled = request.Enabled.Value;
        rule.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        _logger.LogInformation("Timeout rule updated: id={Id}, hours={Hours}, enabled={Enabled}", id, rule.Hours, rule.Enabled);

        return Ok(new { success = true, data = rule });
    }

    // 重置为默认配置
    [HttpPost("rules/reset")]
    public async Task<IActionResult> ResetRules()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // 删除现有规则
        var existingRules = await db.TimeoutRules.ToListAsync();
        db.TimeoutRules.RemoveRange(existingRules);

        // 默认配置
        var defaultRules = new List<TimeoutRule>
        {
            // green - 24小时
            new() { Color = "green", Role = "operator", Hours = 24, Enabled = true, CreatedAt = DateTime.UtcNow },
            new() { Color = "green", Role = "supervisor", Hours = 24, Enabled = true, CreatedAt = DateTime.UtcNow },
            new() { Color = "green", Role = "manager", Hours = 24, Enabled = true, CreatedAt = DateTime.UtcNow },
            new() { Color = "green", Role = "department_head", Hours = 24, Enabled = true, CreatedAt = DateTime.UtcNow },
            new() { Color = "green", Role = "company_head", Hours = 24, Enabled = true, CreatedAt = DateTime.UtcNow },
            // blue - 12小时
            new() { Color = "blue", Role = "operator", Hours = 12, Enabled = true, CreatedAt = DateTime.UtcNow },
            new() { Color = "blue", Role = "supervisor", Hours = 12, Enabled = true, CreatedAt = DateTime.UtcNow },
            new() { Color = "blue", Role = "manager", Hours = 12, Enabled = true, CreatedAt = DateTime.UtcNow },
            new() { Color = "blue", Role = "department_head", Hours = 12, Enabled = true, CreatedAt = DateTime.UtcNow },
            new() { Color = "blue", Role = "company_head", Hours = 12, Enabled = true, CreatedAt = DateTime.UtcNow },
            // orange - 6小时
            new() { Color = "orange", Role = "operator", Hours = 6, Enabled = true, CreatedAt = DateTime.UtcNow },
            new() { Color = "orange", Role = "supervisor", Hours = 6, Enabled = true, CreatedAt = DateTime.UtcNow },
            new() { Color = "orange", Role = "manager", Hours = 6, Enabled = true, CreatedAt = DateTime.UtcNow },
            new() { Color = "orange", Role = "department_head", Hours = 6, Enabled = true, CreatedAt = DateTime.UtcNow },
            new() { Color = "orange", Role = "company_head", Hours = 6, Enabled = true, CreatedAt = DateTime.UtcNow },
            // red - 2小时
            new() { Color = "red", Role = "operator", Hours = 2, Enabled = true, CreatedAt = DateTime.UtcNow },
            new() { Color = "red", Role = "supervisor", Hours = 2, Enabled = true, CreatedAt = DateTime.UtcNow },
            new() { Color = "red", Role = "manager", Hours = 2, Enabled = true, CreatedAt = DateTime.UtcNow },
            new() { Color = "red", Role = "department_head", Hours = 2, Enabled = true, CreatedAt = DateTime.UtcNow },
            new() { Color = "red", Role = "company_head", Hours = 2, Enabled = true, CreatedAt = DateTime.UtcNow },
        };

        db.TimeoutRules.AddRange(defaultRules);
        await db.SaveChangesAsync();

        _logger.LogInformation("Timeout rules reset to default");
        return Ok(new { success = true, message = "已重置为默认配置" });
    }
}

public class UpdateTimeoutRuleRequest
{
    public int? Hours { get; set; }
    public bool? Enabled { get; set; }
}
