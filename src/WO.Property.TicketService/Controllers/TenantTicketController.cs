using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.TicketService.Data;
using WO.Property.TicketService.Tenant;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WO.Property.TicketService.Controllers;

/// <summary>
/// Phase 0 多租户工单控制器
/// 使用 TenantDbContextFactory 动态切换租户库
/// </summary>
[ApiController]
[Route("api/tenant/tickets")]
public class TenantTicketController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TenantTicketController> _logger;

    public TenantTicketController(
        IDbContextFactory<TenantDbContext> dbFactory,
        ITenantDbFactory tenantDbFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<TenantTicketController> logger)
    {
        _dbFactory = dbFactory;
        _tenantDbFactory = tenantDbFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    private string? GetTenantCode()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return null;
        var token = authHeader.Substring("Bearer ".Length).Trim();
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == "tenant_code")?.Value;
        }
        catch { return null; }
    }

    private int? GetUserIdFromJwt()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return null;
        var token = authHeader.Substring("Bearer ".Length).Trim();
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var uidStr = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(uidStr, out var uid) ? uid : null;
        }
        catch { return null; }
    }

    // GET /api/tenant/tickets
    [HttpGet]
    public async Task<IActionResult> GetTickets([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, [FromQuery] int? projectId = null)
    {
        using var db = CreateDbContext();
        var query = db.Tickets.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);

        var total = await query.CountAsync();
        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = tickets,
            total,
            page,
            pageSize
        });
    }

    // GET /api/tenant/tickets/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicket(int id)
    {
        using var db = CreateDbContext();
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null)
            return NotFound(new { success = false, message = "工单不存在" });

        return Ok(new { success = true, data = ticket });
    }

    // POST /api/tenant/tickets
    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] TenantCreateTicketRequest request)
    {
        using var db = CreateDbContext();

        var year = DateTime.Now.Year;
        var count = await db.Tickets.CountAsync() + 1;
        var ticketNo = $"WO-{year}{DateTime.Now:MMdd}-{(1000 + count):D4}";

        var creatorId = GetUserIdFromJwt() ?? 0;

        var ticket = new Ticket
        {
            TicketCode = ticketNo,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category ?? "一般",
            Priority = request.Priority ?? "Medium",
            Status = "New",
            CreatorPersonId = creatorId,
            AssigneePersonId = null,
            ProjectId = request.ProjectId,
            CreatedAt = DateTime.UtcNow
        };

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        return Ok(new { success = true, data = ticket, message = "工单创建成功" });
    }

    // PUT /api/tenant/tickets/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTicket(int id, [FromBody] TenantUpdateTicketRequest request)
    {
        using var db = CreateDbContext();
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null)
            return NotFound(new { success = false, message = "工单不存在" });

        if (!string.IsNullOrEmpty(request.Title)) ticket.Title = request.Title;
        if (!string.IsNullOrEmpty(request.Description)) ticket.Description = request.Description;
        if (!string.IsNullOrEmpty(request.Status)) ticket.Status = request.Status;
        if (!string.IsNullOrEmpty(request.Priority)) ticket.Priority = request.Priority;
        if (!string.IsNullOrEmpty(request.Category)) ticket.Category = request.Category;

        ticket.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { success = true, data = ticket, message = "工单更新成功" });
    }

    // DELETE /api/tenant/tickets/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTicket(int id)
    {
        using var db = CreateDbContext();
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null)
            return NotFound(new { success = false, message = "工单不存在" });

        db.Tickets.Remove(ticket);
        await db.SaveChangesAsync();

        return Ok(new { success = true, message = "工单删除成功" });
    }
}

public class TenantCreateTicketRequest
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public string? Location { get; set; }
    public int ProjectId { get; set; }
    public List<string>? Images { get; set; }
}

public class TenantUpdateTicketRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Category { get; set; }
}