using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WO.Property.TicketService.Controllers;

[ApiController]
[Route("api/ticket-types")]
public class TicketTypesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<TicketTypesController> _logger;

    public TicketTypesController(AppDbContext db, ILogger<TicketTypesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有工单类型
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.TicketTypes
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Id)
            .ToListAsync();

        return Ok(new { success = true, data = items });
    }

    /// <summary>
    /// 获取单个工单类型
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.TicketTypes.FindAsync(id);
        if (item == null)
            return NotFound(new { success = false, message = "工单类型不存在" });
        return Ok(new { success = true, data = item });
    }

    /// <summary>
    /// 新增工单类型
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTicketTypeRequest request)
    {
        var maxId = await _db.TicketTypes.AnyAsync()
            ? await _db.TicketTypes.MaxAsync(t => t.Id) + 1
            : 1;

        var item = new TicketTypeEntity
        {
            Name = request.name,
            Code = request.code ?? request.name.ToLower().Replace(" ", "-"),
            Icon = request.icon ?? "Document",
            Color = request.color ?? "#409EFF",
            Status = request.status ?? "Active",
            SortOrder = request.sortOrder ?? 0
        };

        _db.TicketTypes.Add(item);
        await _db.SaveChangesAsync();

        _logger.LogInformation("创建工单类型: {Name}", request.name);

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, new
        {
            success = true,
            message = "工单类型创建成功",
            data = new { id = item.Id, name = item.Name }
        });
    }

    /// <summary>
    /// 更新工单类型
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTicketTypeRequest request)
    {
        var item = await _db.TicketTypes.FindAsync(id);
        if (item == null)
            return NotFound(new { success = false, message = "工单类型不存在" });

        if (!string.IsNullOrEmpty(request.name)) item.Name = request.name;
        if (request.code != null) item.Code = request.code;
        if (request.icon != null) item.Icon = request.icon;
        if (request.color != null) item.Color = request.color;
        if (request.status != null) item.Status = request.status;
        if (request.sortOrder.HasValue) item.SortOrder = request.sortOrder.Value;

        await _db.SaveChangesAsync();

        _logger.LogInformation("更新工单类型: {Id}", id);

        return Ok(new { success = true, message = "工单类型更新成功" });
    }

    /// <summary>
    /// 删除工单类型
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.TicketTypes.FindAsync(id);
        if (item == null)
            return NotFound(new { success = false, message = "工单类型不存在" });

        _db.TicketTypes.Remove(item);
        await _db.SaveChangesAsync();

        _logger.LogInformation("删除工单类型: {Id}", id);

        return Ok(new { success = true, message = "工单类型已删除" });
    }
}

public class CreateTicketTypeRequest
{
    public string name { get; set; } = string.Empty;
    public string? code { get; set; }
    public string? icon { get; set; }
    public string? color { get; set; }
    public string? status { get; set; }
    public int? sortOrder { get; set; }
}

public class UpdateTicketTypeRequest
{
    public string? name { get; set; }
    public string? code { get; set; }
    public string? icon { get; set; }
    public string? color { get; set; }
    public string? status { get; set; }
    public int? sortOrder { get; set; }
}