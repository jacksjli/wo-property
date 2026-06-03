using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using WO.Property.PaymentService.Data;
using WO.Property.PaymentService.Models;
using WO.Property.PaymentService.Tenant;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);
ServiceRunner.ConfigurePort(builder, "PaymentService", 5109);
// Tenant 支持
builder.Services.AddSingleton<TenantConfigLoader>();
builder.Services.AddSingleton<WO.Property.PaymentService.Tenant.ITenantDbFactory, WO.Property.PaymentService.Tenant.TenantDbFactory>();
builder.Services.AddScoped<IDbContextFactory<WO.Property.PaymentService.Data.TenantDbContext>, WO.Property.PaymentService.Data.TenantDbContextFactory>();

builder.Services.AddDbContext<PaymentDbContext>((sp, options) =>
{
    var factory = sp.GetRequiredService<ITenantDbFactory>();
    var tenantCode = factory.GetCurrentTenantCode() ?? "wo_property";
    var connStr = factory.GetTenantConnectionString(tenantCode);
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
    options.UseMySql(new MySqlConnection(connStr), serverVersion);
});
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtKey = jwtSettings["SecretKey"] ?? JwtHelper.GetSecretKey();
var jwtIssuer = jwtSettings["Issuer"] ?? "wo-property-unified-auth";
var jwtAudience = jwtSettings["Audience"] ?? "wo-property-services";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opts => {
    opts.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true, ValidIssuer = jwtIssuer, ValidAudience = jwtAudience, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)) };
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

builder.Services.AddHttpClient("Gateway", client =>
{
    client.BaseAddress = new Uri("http://localhost:5000");
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddCors(options => options.AddPolicy("AllowFrontend", policy => {
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins")
        .Get<string[]>() ?? new[] { "http://localhost:5173" };
    policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
}));
var app = builder.Build();
// EnsureCreated removed - tables already exist in MySQL (2026-05-15)
app.UseCors("AllowFrontend");
app.UseMiddleware<WO.Property.PaymentService.Middleware.TenantRoutingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "PaymentService", timestamp = DateTime.UtcNow }));
Console.WriteLine("PaymentService Port: 5007");
app.Run();

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly PaymentDbContext _context;
    public PaymentController(PaymentDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetBills(
        [FromQuery] string? unit = null,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var q = _context.Bills.AsQueryable();
        if (!string.IsNullOrEmpty(unit)) q = q.Where(b => b.Unit.Contains(unit));
        if (!string.IsNullOrEmpty(type)) q = q.Where(b => b.Type == type);
        if (!string.IsNullOrEmpty(status)) q = q.Where(b => b.Status == status);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(b => b.BillingMonth).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return Ok(new { success = true, total, page, pageSize, data = items });
    }

    [HttpGet("meters")]
    public async Task<IActionResult> GetMeters([FromQuery] string? unit = null)
    {
        var q = _context.MeterReadings.AsQueryable();
        if (!string.IsNullOrEmpty(unit)) q = q.Where(m => m.Unit.Contains(unit));
        var meters = await q.OrderByDescending(m => m.ReadingDate).Take(50).ToListAsync();
        return Ok(new { success = true, data = meters });
    }

    [HttpPost("meters")]
    public async Task<IActionResult> SubmitMeterReading([FromBody] MeterReading m)
    {
        m.ReadingDate = DateTime.UtcNow;
        _context.MeterReadings.Add(m);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = m });
    }

    [HttpPost("{id}/pay")]
    public async Task<IActionResult> PayBill(int id, [FromBody] PayRequest req)
    {
        var bill = await _context.Bills.FindAsync(id);
        if (bill == null) return NotFound();
        bill.Status = "paid";
        bill.PaidAt = DateTime.UtcNow;
        bill.PaymentMethod = req.Method;
        bill.TransactionId = "TXN-" + Guid.NewGuid().ToString()[..12].ToUpper();
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = bill });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] int? projectId = null)
    {
        var q = _context.Bills.AsQueryable();
        if (projectId.HasValue) q = q.Where(b => b.ProjectId == projectId.Value);

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var thisMonth = await q.Where(b => b.BillingMonth >= monthStart).ToListAsync();
        var totalRevenue = thisMonth.Where(b => b.Status == "paid").Sum(b => b.Amount);
        var pendingAmount = thisMonth.Where(b => b.Status == "unpaid").Sum(b => b.Amount);
        var paidCount = thisMonth.Count(b => b.Status == "paid");
        var totalCount = thisMonth.Count;

        return Ok(new { success = true, data = new {
            thisMonthRevenue = totalRevenue,
            pendingAmount,
            collectionRate = totalCount > 0 ? Math.Round(100.0 * paidCount / totalCount, 1) : 0,
            totalBills = totalCount,
            paidBills = paidCount
        }});
    }
}

public class PayRequest
{
    public string? Method { get; set; }
    public string? Remark { get; set; }
}
