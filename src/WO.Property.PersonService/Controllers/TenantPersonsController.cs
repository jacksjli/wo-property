using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.PersonService.Data;
using WO.Property.PersonService.Models;

namespace WO.Property.PersonService.Controllers;

[ApiController]
[Route("api/tenant/persons")]
public class TenantPersonsController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ILogger<TenantPersonsController> _logger;

    public TenantPersonsController(
        IDbContextFactory<TenantDbContext> dbFactory,
        ILogger<TenantPersonsController> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    // GET /api/tenant/persons/persons
    [HttpGet]
    public async Task<IActionResult> GetPersons(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? name = null,
        [FromQuery] string? phone = null,
        [FromQuery] string? department = null,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Personnel.AsQueryable();

            if (!string.IsNullOrEmpty(name))
                query = query.Where(p => p.Name.Contains(name));
            if (!string.IsNullOrEmpty(phone))
                query = query.Where(p => p.Phone.Contains(phone));
            if (!string.IsNullOrEmpty(department))
                query = query.Where(p => p.DepartmentName == department);
            if (!string.IsNullOrEmpty(role))
                query = query.Where(p => p.Role == role);
            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);
            else
                query = query.Where(p => p.Status != "已删除");

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    id = p.Id,
                    employeeNo = p.EmployeeNo,
                    name = p.Name,
                    avatar = p.Avatar ?? "",
                    gender = p.Gender,
                    birthday = p.Birthday,
                    idCard = p.IdCard ?? "",
                    phone = p.Phone,
                    email = p.Email ?? "",
                    address = p.Address ?? "",
                    education = p.Education ?? "",
                    graduateSchool = p.GraduateSchool ?? "",
                    major = p.Major ?? "",
                    role = p.Role,
                    departmentId = p.DepartmentId,
                    departmentName = p.DepartmentName ?? "",
                    position = p.Position ?? "",
                    employmentType = p.EmploymentType ?? "",
                    hireDate = p.HireDate,
                    contractStart = p.ContractStart,
                    contractEnd = p.ContractEnd,
                    salary = p.Salary,
                    bankAccount = p.BankAccount ?? "",
                    socialSecurityNo = p.SocialSecurityNo ?? "",
                    status = p.Status,
                    specialties = p.Specialties ?? "",
                    backups = p.Backups ?? "",
                    emergencyContactName = p.EmergencyContactName ?? "",
                    emergencyContactRelationship = p.EmergencyContactRelationship ?? "",
                    emergencyContactPhone = p.EmergencyContactPhone ?? "",
                    attendanceCount = p.AttendanceCount,
                    overtimeHours = p.OvertimeHours,
                    leaveDays = p.LeaveDays,
                    performanceScore = p.PerformanceScore,
                    trainingCount = p.TrainingCount,
                    remark = p.Remark ?? "",
                    createdAt = p.CreatedAt,
                    updatedAt = p.UpdatedAt,
                    ticketTypeIds = p.TicketTypeIds ?? "",
                    specialtyIds = p.SpecialtyIds ?? "",
                    isSupervisor = p.IsSupervisor,
                    maxConcurrentTickets = p.MaxConcurrentTickets
                })
                .ToListAsync();

            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPersons failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/persons/persons/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPerson(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var p = await db.Personnel.FindAsync(id);
            if (p == null)
                return NotFound(new { success = false, message = "人员不存在" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = p.Id,
                    employeeNo = p.EmployeeNo,
                    name = p.Name,
                    avatar = p.Avatar ?? "",
                    gender = p.Gender,
                    birthday = p.Birthday,
                    idCard = p.IdCard ?? "",
                    phone = p.Phone,
                    email = p.Email ?? "",
                    address = p.Address ?? "",
                    education = p.Education ?? "",
                    graduateSchool = p.GraduateSchool ?? "",
                    major = p.Major ?? "",
                    role = p.Role,
                    departmentId = p.DepartmentId,
                    departmentName = p.DepartmentName ?? "",
                    position = p.Position ?? "",
                    employmentType = p.EmploymentType ?? "",
                    hireDate = p.HireDate,
                    contractStart = p.ContractStart,
                    contractEnd = p.ContractEnd,
                    salary = p.Salary,
                    bankAccount = p.BankAccount ?? "",
                    socialSecurityNo = p.SocialSecurityNo ?? "",
                    status = p.Status,
                    specialties = p.Specialties ?? "",
                    backups = p.Backups ?? "",
                    emergencyContactName = p.EmergencyContactName ?? "",
                    emergencyContactRelationship = p.EmergencyContactRelationship ?? "",
                    emergencyContactPhone = p.EmergencyContactPhone ?? "",
                    remark = p.Remark ?? "",
                    ticketTypeIds = p.TicketTypeIds ?? "",
                    specialtyIds = p.SpecialtyIds ?? "",
                    isSupervisor = p.IsSupervisor,
                    maxConcurrentTickets = p.MaxConcurrentTickets
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPerson {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/persons/persons
    [HttpPost]
    public async Task<IActionResult> CreatePerson([FromBody] CreatePersonnelRequest request)
    {
        try
        {
            using var db = CreateDbContext();

            // 检查手机号重复
            var exists = await db.Personnel.AnyAsync(p => p.Phone == request.Phone);
            if (exists)
                return BadRequest(new { success = false, message = "手机号已存在" });

            var personnel = new Personnel
            {
                EmployeeNo = string.IsNullOrEmpty(request.EmployeeNo)
                    ? "EMP" + DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()[5..]
                    : request.EmployeeNo,
                Name = request.Name,
                Avatar = request.Avatar,
                Gender = request.Gender,
                Birthday = request.Birthday,
                IdCard = request.IdCard,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address,
                Education = request.Education,
                GraduateSchool = request.GraduateSchool,
                Major = request.Major,
                Role = request.Role,
                DepartmentId = request.DepartmentId,
                DepartmentName = request.DepartmentName,
                Position = request.Position,
                EmploymentType = request.EmploymentType,
                HireDate = request.HireDate,
                ContractStart = request.ContractStart,
                ContractEnd = request.ContractEnd,
                Salary = request.Salary,
                BankAccount = request.BankAccount,
                SocialSecurityNo = request.SocialSecurityNo,
                Status = request.Status,
                Specialties = request.Specialties,
                Backups = request.Backups,
                EmergencyContactName = request.EmergencyContactName,
                EmergencyContactRelationship = request.EmergencyContactRelationship,
                EmergencyContactPhone = request.EmergencyContactPhone,
                Remark = request.Remark,
                TicketTypeIds = request.TicketTypeIds,
                SpecialtyIds = request.SpecialtyIds,
                AreaIds = request.AreaIds,
                IsSupervisor = request.IsSupervisor,
                MaxConcurrentTickets = request.MaxConcurrentTickets,
                CreatedAt = DateTime.UtcNow
            };

            db.Personnel.Add(personnel);
            await db.SaveChangesAsync();

            return Ok(new { success = true, data = new { id = personnel.Id }, message = "人员添加成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreatePerson failed");
            return Ok(new { success = false, message = ex.Message, detail = ex.InnerException?.Message });
        }
    }

    // PUT /api/tenant/persons/persons/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePerson(int id, [FromBody] UpdatePersonnelRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var personnel = await db.Personnel.FindAsync(id);
            if (personnel == null)
                return NotFound(new { success = false, message = "人员不存在" });

            // 检查手机号重复
            if (!string.IsNullOrEmpty(request.Phone))
            {
                var phoneExists = await db.Personnel.AnyAsync(p => p.Phone == request.Phone && p.Id != id);
                if (phoneExists)
                    return BadRequest(new { success = false, message = "手机号已被使用" });
            }

            // 更新字段（只更新非 null 的字段）
            if (request.Name != null) personnel.Name = request.Name;
            if (request.Avatar != null) personnel.Avatar = request.Avatar;
            if (request.Gender != null) personnel.Gender = request.Gender;
            if (request.Birthday.HasValue) personnel.Birthday = request.Birthday;
            if (request.IdCard != null) personnel.IdCard = request.IdCard;
            if (request.Phone != null) personnel.Phone = request.Phone;
            if (request.Email != null) personnel.Email = request.Email;
            if (request.Address != null) personnel.Address = request.Address;
            if (request.Education != null) personnel.Education = request.Education;
            if (request.GraduateSchool != null) personnel.GraduateSchool = request.GraduateSchool;
            if (request.Major != null) personnel.Major = request.Major;
            if (request.Role != null) personnel.Role = request.Role;
            if (request.DepartmentId.HasValue) personnel.DepartmentId = request.DepartmentId;
            if (request.DepartmentName != null) personnel.DepartmentName = request.DepartmentName;
            if (request.Position != null) personnel.Position = request.Position;
            if (request.EmploymentType != null) personnel.EmploymentType = request.EmploymentType;
            if (request.HireDate.HasValue) personnel.HireDate = request.HireDate;
            if (request.ContractStart.HasValue) personnel.ContractStart = request.ContractStart;
            if (request.ContractEnd.HasValue) personnel.ContractEnd = request.ContractEnd;
            if (request.Salary.HasValue) personnel.Salary = request.Salary;
            if (request.BankAccount != null) personnel.BankAccount = request.BankAccount;
            if (request.SocialSecurityNo != null) personnel.SocialSecurityNo = request.SocialSecurityNo;
            if (request.Status != null) personnel.Status = request.Status;
            if (request.Specialties != null) personnel.Specialties = request.Specialties;
            if (request.Backups != null) personnel.Backups = request.Backups;
            if (request.EmergencyContactName != null) personnel.EmergencyContactName = request.EmergencyContactName;
            if (request.EmergencyContactRelationship != null) personnel.EmergencyContactRelationship = request.EmergencyContactRelationship;
            if (request.EmergencyContactPhone != null) personnel.EmergencyContactPhone = request.EmergencyContactPhone;
            if (request.Remark != null) personnel.Remark = request.Remark;
            if (request.TicketTypeIds != null) personnel.TicketTypeIds = request.TicketTypeIds;
            if (request.SpecialtyIds != null) personnel.SpecialtyIds = request.SpecialtyIds;
            if (request.AreaIds != null) personnel.AreaIds = request.AreaIds;
            if (request.IsSupervisor == true) personnel.IsSupervisor = request.IsSupervisor.Value;
            if (request.MaxConcurrentTickets.HasValue) personnel.MaxConcurrentTickets = request.MaxConcurrentTickets.Value;

            personnel.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "人员更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdatePerson {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // DELETE /api/tenant/persons/persons/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePerson(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var personnel = await db.Personnel.FindAsync(id);
            if (personnel == null)
                return NotFound(new { success = false, message = "人员不存在" });

            personnel.Status = "已删除";
            personnel.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeletePerson {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }
}