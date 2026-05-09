using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using WO.Property.RenovationService.Data;
using WO.Property.RenovationService.Models;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5028");
builder.Services.AddDbContext<RenovationDbContext>(opts => opts.UseNpgsql("Host=postgres;Database=wo_property;Username=woproperty;Password=WOProperty2026!"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtKey = jwtSettings["SecretKey"] ?? JwtHelper.GetSecretKey();
var jwtIssuer = jwtSettings["Issuer"] ?? "wo-property-unified-auth";
var jwtAudience = jwtSettings["Audience"] ?? "wo-property-services";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opts => {
    opts.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)) };
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
using (var scope = app.Services.CreateScope()) scope.ServiceProvider.GetRequiredService<RenovationDbContext>().Database.EnsureCreated();
app.UseCors("AllowFrontend");
app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "RenovationService", timestamp = DateTime.UtcNow }));
Console.WriteLine("RenovationService Port: 5504");
app.Run();

[ApiController]
[Route("api/renovations")]
public class RenovationController : ControllerBase
{
    private readonly RenovationDbContext _context;
    public RenovationController(RenovationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null)
    {
        var q = _context.Applications.AsQueryable();
        if (!string.IsNullOrEmpty(status)) q = q.Where(a => a.Status == status);
        return Ok(new { success = true, data = await q.OrderByDescending(a => a.CreatedAt).ToListAsync() });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RenovationApplication app)
    {
        app.Status = "pending";
        app.CreatedAt = DateTime.UtcNow;
        _context.Applications.Add(app);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = app });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] RenovationApplication patch)
    {
        var r = await _context.Applications.FindAsync(id);
        if (r == null) return NotFound();
        r.Status = patch.Status;
        r.Remark = patch.Remark;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = r });
    }
}
