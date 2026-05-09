using Microsoft.EntityFrameworkCore;
using WO.Property.PersonService.Models;

namespace WO.Property.PersonService.Data;

public class DbInitializer
{
    public void Initialize(PersonDbContext context)
    {
        // Create database and tables if not exist
        context.Database.EnsureCreated();

        // Seed Departments
        if (!context.Departments.Any())
        {
            var departments = new[]
            {
                new Department { Name = "工程部", Code = "ENGINEERING", SortOrder = 1 },
                new Department { Name = "客服部", Code = "CUSTOMER_SERVICE", SortOrder = 2 },
                new Department { Name = "安保部", Code = "SECURITY", SortOrder = 3 },
                new Department { Name = "保洁部", Code = "CLEANING", SortOrder = 4 },
                new Department { Name = "绿化部", Code = "LANDSCAPING", SortOrder = 5 },
                new Department { Name = "行政部", Code = "ADMIN", SortOrder = 6 },
                new Department { Name = "财务部", Code = "FINANCE", SortOrder = 7 },
                new Department { Name = "人事部", Code = "HR", SortOrder = 8 }
            };
            context.Departments.AddRange(departments);
            context.SaveChanges();
        }

        // Seed Roles
        if (!context.Roles.Any())
        {
            var roles = new[]
            {
                new Role { Name = "操作员", Code = "operator", Level = 1, Description = "一线操作人员" },
                new Role { Name = "主管", Code = "supervisor", Level = 2, Description = "小组主管" },
                new Role { Name = "经理", Code = "manager", Level = 3, Description = "部门经理" },
                new Role { Name = "部门负责人", Code = "department_head", Level = 4, Description = "部门负责人" },
                new Role { Name = "公司负责人", Code = "company_head", Level = 5, Description = "公司负责人" }
            };
            context.Roles.AddRange(roles);
            context.SaveChanges();
        }

        // Seed Persons
        if (!context.Persons.Any())
        {
            var persons = new[]
            {
                new Person { StaffId = "EMP-001", Name = "系统管理员", Gender = "男", Phone = "13800138000", Email = "admin@example.com", Department = "行政部", Role = "company_head", Status = "在职", PersonType = "员工" },
                new Person { StaffId = "EMP-002", Name = "李师傅", Gender = "男", Phone = "13800138001", Email = "li@example.com", Department = "工程部", Role = "operator", Status = "在职", PersonType = "员工" },
                new Person { StaffId = "EMP-003", Name = "王师傅", Gender = "男", Phone = "13800138002", Email = "wang@example.com", Department = "工程部", Role = "operator", Status = "在职", PersonType = "员工" },
                new Person { StaffId = "EMP-004", Name = "张客服", Gender = "女", Phone = "13800138003", Email = "zhang@example.com", Department = "客服部", Role = "supervisor", Status = "在职", PersonType = "员工" },
                new Person { StaffId = "EMP-005", Name = "刘保安", Gender = "男", Phone = "13800138004", Email = "liu@example.com", Department = "安保部", Role = "operator", Status = "在职", PersonType = "员工" }
            };
            context.Persons.AddRange(persons);
            context.SaveChanges();
        }
    }
}