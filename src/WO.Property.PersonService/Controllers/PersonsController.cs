using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.PersonService.Data;
using WO.Property.PersonService.DTOs;
using WO.Property.PersonService.Models;
using System.Security.Cryptography;
using System.Text;

namespace WO.Property.PersonService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PersonsController : ControllerBase
{
    private readonly PersonDbContext _context;

    public PersonsController(PersonDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<PersonDto>>> GetPersons([FromQuery] PersonQueryDto query)
    {
        var queryable = _context.Persons.AsQueryable();

        if (!string.IsNullOrEmpty(query.Name))
            queryable = queryable.Where(p => p.Name.Contains(query.Name));
        if (!string.IsNullOrEmpty(query.Phone))
            queryable = queryable.Where(p => p.Phone.Contains(query.Phone));
        if (!string.IsNullOrEmpty(query.Department))
            queryable = queryable.Where(p => p.Department == query.Department);
        if (!string.IsNullOrEmpty(query.Role))
            queryable = queryable.Where(p => p.Role == query.Role);
        if (!string.IsNullOrEmpty(query.Status))
            queryable = queryable.Where(p => p.Status == query.Status);
        if (!string.IsNullOrEmpty(query.PersonType))
            queryable = queryable.Where(p => p.PersonType == query.PersonType);

        var totalCount = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new PersonDto
            {
                Id = p.Id,
                StaffId = p.StaffId,
                Name = p.Name,
                Gender = p.Gender,
                Phone = p.Phone,
                Email = p.Email,
                Department = p.Department,
                Role = p.Role,
                JoinDate = p.JoinDate,
                Status = p.Status,
                IdCard = p.IdCard,
                EmergencyContact = p.EmergencyContact,
                EmergencyPhone = p.EmergencyPhone,
                PersonType = p.PersonType,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return Ok(new PagedResultDto<PersonDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PersonDto>> GetPerson(int id)
    {
        var person = await _context.Persons.FindAsync(id);
        if (person == null)
            return NotFound();

        return Ok(new PersonDto
        {
            Id = person.Id,
            StaffId = person.StaffId,
            Name = person.Name,
            Gender = person.Gender,
            Phone = person.Phone,
            Email = person.Email,
            Department = person.Department,
            Role = person.Role,
            JoinDate = person.JoinDate,
            Status = person.Status,
            IdCard = person.IdCard,
            EmergencyContact = person.EmergencyContact,
            EmergencyPhone = person.EmergencyPhone,
            PersonType = person.PersonType,
            CreatedAt = person.CreatedAt,
            UpdatedAt = person.UpdatedAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<PersonDto>> CreatePerson([FromBody] CreatePersonDto dto)
    {
        if (await _context.Persons.AnyAsync(p => p.StaffId == dto.StaffId))
            return BadRequest(new { message = "员工编号已存在" });
        if (await _context.Persons.AnyAsync(p => p.Phone == dto.Phone))
            return BadRequest(new { message = "联系电话已存在" });

        var person = new Person
        {
            StaffId = dto.StaffId,
            Name = dto.Name,
            Gender = dto.Gender,
            Phone = dto.Phone,
            Email = dto.Email,
            Department = dto.Department,
            Role = dto.Role,
            JoinDate = dto.JoinDate,
            Status = dto.Status,
            IdCard = dto.IdCard,
            EmergencyContact = dto.EmergencyContact,
            EmergencyPhone = dto.EmergencyPhone,
            PasswordHash = !string.IsNullOrEmpty(dto.Password) ? HashPassword(dto.Password) : null,
            PersonType = dto.PersonType,
            CreatedAt = DateTime.UtcNow
        };

        _context.Persons.Add(person);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPerson), new { id = person.Id }, new PersonDto
        {
            Id = person.Id,
            StaffId = person.StaffId,
            Name = person.Name,
            Gender = person.Gender,
            Phone = person.Phone,
            Email = person.Email,
            Department = person.Department,
            Role = person.Role,
            JoinDate = person.JoinDate,
            Status = person.Status,
            IdCard = person.IdCard,
            EmergencyContact = person.EmergencyContact,
            EmergencyPhone = person.EmergencyPhone,
            PersonType = person.PersonType,
            CreatedAt = person.CreatedAt,
            UpdatedAt = person.UpdatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PersonDto>> UpdatePerson(int id, [FromBody] UpdatePersonDto dto)
    {
        var person = await _context.Persons.FindAsync(id);
        if (person == null)
            return NotFound();

        if (!string.IsNullOrEmpty(dto.Phone) && dto.Phone != person.Phone)
        {
            if (await _context.Persons.AnyAsync(p => p.Phone == dto.Phone && p.Id != id))
                return BadRequest(new { message = "联系电话已存在" });
            person.Phone = dto.Phone;
        }

        if (!string.IsNullOrEmpty(dto.Name)) person.Name = dto.Name;
        if (dto.Gender != null) person.Gender = dto.Gender;
        if (dto.Email != null) person.Email = dto.Email;
        if (!string.IsNullOrEmpty(dto.Department)) person.Department = dto.Department;
        if (!string.IsNullOrEmpty(dto.Role)) person.Role = dto.Role;
        if (dto.JoinDate.HasValue) person.JoinDate = dto.JoinDate;
        if (!string.IsNullOrEmpty(dto.Status)) person.Status = dto.Status;
        if (dto.IdCard != null) person.IdCard = dto.IdCard;
        if (dto.EmergencyContact != null) person.EmergencyContact = dto.EmergencyContact;
        if (dto.EmergencyPhone != null) person.EmergencyPhone = dto.EmergencyPhone;
        if (!string.IsNullOrEmpty(dto.Password)) person.PasswordHash = HashPassword(dto.Password);
        if (!string.IsNullOrEmpty(dto.PersonType)) person.PersonType = dto.PersonType;

        person.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new PersonDto
        {
            Id = person.Id,
            StaffId = person.StaffId,
            Name = person.Name,
            Gender = person.Gender,
            Phone = person.Phone,
            Email = person.Email,
            Department = person.Department,
            Role = person.Role,
            JoinDate = person.JoinDate,
            Status = person.Status,
            IdCard = person.IdCard,
            EmergencyContact = person.EmergencyContact,
            EmergencyPhone = person.EmergencyPhone,
            PersonType = person.PersonType,
            CreatedAt = person.CreatedAt,
            UpdatedAt = person.UpdatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePerson(int id)
    {
        var person = await _context.Persons.FindAsync(id);
        if (person == null)
            return NotFound();

        // Soft delete
        person.Status = "已删除";
        person.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("by-role/{role}")]
    public async Task<ActionResult<List<PersonDto>>> GetPersonsByRole(string role)
    {
        var persons = await _context.Persons
            .Where(p => p.Role == role)
            .Select(p => new PersonDto
            {
                Id = p.Id,
                StaffId = p.StaffId,
                Name = p.Name,
                Gender = p.Gender,
                Phone = p.Phone,
                Email = p.Email,
                Department = p.Department,
                Role = p.Role,
                JoinDate = p.JoinDate,
                Status = p.Status,
                PersonType = p.PersonType,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return Ok(persons);
    }

    [HttpGet("by-department/{dept}")]
    public async Task<ActionResult<List<PersonDto>>> GetPersonsByDepartment(string dept)
    {
        var persons = await _context.Persons
            .Where(p => p.Department == dept)
            .Select(p => new PersonDto
            {
                Id = p.Id,
                StaffId = p.StaffId,
                Name = p.Name,
                Gender = p.Gender,
                Phone = p.Phone,
                Email = p.Email,
                Department = p.Department,
                Role = p.Role,
                JoinDate = p.JoinDate,
                Status = p.Status,
                PersonType = p.PersonType,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return Ok(persons);
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<PersonDto>>> SearchPersons([FromQuery] string? name, [FromQuery] string? phone)
    {
        var queryable = _context.Persons.AsQueryable();

        if (!string.IsNullOrEmpty(name))
            queryable = queryable.Where(p => p.Name.Contains(name));
        if (!string.IsNullOrEmpty(phone))
            queryable = queryable.Where(p => p.Phone.Contains(phone));

        var persons = await queryable
            .Take(50)
            .Select(p => new PersonDto
            {
                Id = p.Id,
                StaffId = p.StaffId,
                Name = p.Name,
                Gender = p.Gender,
                Phone = p.Phone,
                Email = p.Email,
                Department = p.Department,
                Role = p.Role,
                JoinDate = p.JoinDate,
                Status = p.Status,
                PersonType = p.PersonType,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return Ok(persons);
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<PersonStatisticsDto>> GetStatistics()
    {
        var allPersons = await _context.Persons.ToListAsync();

        var stats = new PersonStatisticsDto
        {
            Total = allPersons.Count,
            ActiveCount = allPersons.Count(p => p.Status == "在职"),
            InactiveCount = allPersons.Count(p => p.Status != "在职"),
            ByDepartment = allPersons.GroupBy(p => p.Department).ToDictionary(g => g.Key, g => g.Count()),
            ByRole = allPersons.GroupBy(p => p.Role).ToDictionary(g => g.Key, g => g.Count()),
            ByPersonType = allPersons.GroupBy(p => p.PersonType).ToDictionary(g => g.Key, g => g.Count())
        };

        return Ok(stats);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}