using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using WO.Property.AccessControlService.Data;
using WO.Property.AccessControlService.Models;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5006");

builder.Services.AddDbContext<AccessDbContext>(options =>
{
    var connectionString = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4";
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
    options.UseMySql(new MySqlConnection(connectionString), serverVersion);
});


var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtKey = jwtSettings["SecretKey"] ?? JwtHelper.GetSecretKey();
var jwtIssuer = jwtSettings["Issuer"] ?? "wo-property-unified-auth";
var jwtAudience = jwtSettings["Audience"] ?? "wo-property-services";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true, ValidateAudience = true,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

builder.Services.ConfigureHttpJsonOptions(opts => {
    opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.PropertyNameCaseInsensitive = true;
    opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddCors(options => options.AddPolicy("AllowFrontend", policy => {
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins")
        .Get<string[]>() ?? new[] { "http://localhost:5173" };
    policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
}));

var app = builder.Build();


app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "AccessControlService", timestamp = DateTime.UtcNow }));

Console.WriteLine("===========================================");
Console.WriteLine("  WO Property Access Control Service");
Console.WriteLine("  Port: 5503");
Console.WriteLine("===========================================");

app.Run();

[ApiController]
[Route("api/access")]
public class AccessController : ControllerBase
{
    private readonly AccessDbContext _context;

    public AccessController(AccessDbContext context) => _context = context;

    // ---------- 门禁卡 ----------

    [HttpGet("cards")]
    public async Task<IActionResult> GetCards([FromQuery] string? ownerName = null, [FromQuery] string? status = null)
    {
        var q = _context.AccessCards.AsQueryable();
        if (!string.IsNullOrEmpty(ownerName)) q = q.Where(c => c.OwnerName.Contains(ownerName));
        if (!string.IsNullOrEmpty(status)) q = q.Where(c => c.Status == status);
        var cards = await q.Include(c => c.Building).OrderByDescending(c => c.IsActive).ThenBy(c => c.OwnerName).ToListAsync();
        return Ok(new { success = true, data = cards });
    }

    [HttpPost("cards")]
    public async Task<IActionResult> CreateCard([FromBody] AccessCard card)
    {
        _context.AccessCards.Add(card);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = card });
    }

    [HttpPut("cards/{id}")]
    public async Task<IActionResult> UpdateCard(int id, [FromBody] AccessCard patch)
    {
        var card = await _context.AccessCards.FindAsync(id);
        if (card == null) return NotFound();
        card.OwnerName = patch.OwnerName;
        card.OwnerPhone = patch.OwnerPhone;
        card.Type = patch.Type;
        card.Status = patch.Status;
        card.BuildingId = patch.BuildingId;
        card.Floor = patch.Floor;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = card });
    }

    [HttpDelete("cards/{id}")]
    public async Task<IActionResult> DeleteCard(int id)
    {
        var card = await _context.AccessCards.FindAsync(id);
        if (card == null) return NotFound();
        card.Status = "cancelled";
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // ---------- 临时通行 ----------

    [HttpGet("temp-codes")]
    public async Task<IActionResult> GetTempCodes([FromQuery] int? buildingId = null)
    {
        var q = _context.TempAccessCodes.AsQueryable();
        if (buildingId.HasValue) q = q.Where(c => c.BuildingId == buildingId);
        q = q.Where(c => c.ExpiresAt > DateTime.UtcNow && c.Status == "active");
        var codes = await q.Include(c => c.Building).OrderByDescending(c => c.CreatedAt).ToListAsync();
        return Ok(new { success = true, data = codes });
    }

    [HttpPost("temp-codes")]
    public async Task<IActionResult> CreateTempCode([FromBody] TempAccessCode code)
    {
        code.Code = GenerateCode(6);
        code.Status = "active";
        code.CreatedAt = DateTime.UtcNow;
        _context.TempAccessCodes.Add(code);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = code });
    }

    [HttpDelete("temp-codes/{id}")]
    public async Task<IActionResult> DeleteTempCode(int id)
    {
        var code = await _context.TempAccessCodes.FindAsync(id);
        if (code == null) return NotFound();
        code.Status = "expired";
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // ---------- 通行记录 ----------

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? cardId = null,
        [FromQuery] int? buildingId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var q = _context.AccessLogs.AsQueryable();
        if (!string.IsNullOrEmpty(cardId)) q = q.Where(l => l.CardId == cardId);
        if (buildingId.HasValue) q = q.Where(l => l.BuildingId == buildingId);
        if (startDate.HasValue) q = q.Where(l => l.AccessTime >= startDate.Value);
        if (endDate.HasValue) q = q.Where(l => l.AccessTime <= endDate.Value);

        var total = await q.CountAsync();
        var logs = await q
            .Include(l => l.Building)
            .OrderByDescending(l => l.AccessTime)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return Ok(new { success = true, total, page, pageSize, data = logs });
    }

    /// <summary>刷卡通行记录（设备调用）</summary>
    [HttpPost("logs")]
    public async Task<IActionResult> CreateLog([FromBody] AccessLog log)
    {
        log.AccessTime = DateTime.UtcNow;
        _context.AccessLogs.Add(log);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = log });
    }

    // ---------- 楼栋门禁 ----------

    [HttpGet("doors")]
    public async Task<IActionResult> GetDoors([FromQuery] int? buildingId = null)
    {
        var q = _context.AccessDoors.AsQueryable();
        if (buildingId.HasValue) q = q.Where(d => d.BuildingId == buildingId);
        var doors = await q.ToListAsync();
        return Ok(new { success = true, data = doors });
    }

    [HttpPost("doors")]
    public async Task<IActionResult> CreateDoor([FromBody] AccessDoor door)
    {
        _context.AccessDoors.Add(door);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = door });
    }

    [HttpPut("doors/{id}")]
    public async Task<IActionResult> UpdateDoor(int id, [FromBody] AccessDoor patch)
    {
        var door = await _context.AccessDoors.FindAsync(id);
        if (door == null) return NotFound();
        door.Name = patch.Name;
        door.Location = patch.Location;
        door.Status = patch.Status;
        door.LastOnlineAt = patch.LastOnlineAt;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = door });
    }

    // ---------- 统计 ----------

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] int? buildingId = null)
    {
        var q = _context.AccessLogs.AsQueryable();
        if (buildingId.HasValue) q = q.Where(l => l.BuildingId == buildingId);

        var today = DateTime.UtcNow.Date;
        var todayLogs = await q.CountAsync(l => l.AccessTime >= today);
        var totalCards = await _context.AccessCards.CountAsync(c => c.Status == "active");
        var alertCount = await _context.AccessLogs.CountAsync(l => l.AccessType == "alert" && l.AccessTime >= today);

        return Ok(new { success = true, data = new { todayLogs, totalCards, alertCount } });
    }

    private string GenerateCode(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
