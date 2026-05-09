using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.PersonService.Data;
using WO.Property.PersonService.DTOs;

namespace WO.Property.PersonService.Controllers;

[ApiController]
[Route("api/enums")]
[AllowAnonymous]
public class EnumsController : ControllerBase
{
    private readonly PersonDbContext _context;

    public EnumsController(PersonDbContext context)
    {
        _context = context;
    }

    [HttpGet("departments")]
    public async Task<ActionResult<List<DepartmentDto>>> GetDepartments()
    {
        var departments = await _context.Departments
            .OrderBy(d => d.SortOrder)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                Code = d.Code,
                Description = d.Description,
                SortOrder = d.SortOrder
            })
            .ToListAsync();

        return Ok(departments);
    }

    [HttpGet("roles")]
    public async Task<ActionResult<List<RoleDto>>> GetRoles()
    {
        var roles = await _context.Roles
            .OrderBy(r => r.Level)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Code = r.Code,
                Level = r.Level,
                Description = r.Description
            })
            .ToListAsync();

        return Ok(roles);
    }

    [HttpGet("person-types")]
    public ActionResult<List<string>> GetPersonTypes()
    {
        var types = new List<string> { "员工", "外包", "临时工", "访客" };
        return Ok(types);
    }
}