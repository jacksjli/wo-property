using Microsoft.EntityFrameworkCore;
using WO.Property.InspectionService.Models;

namespace WO.Property.InspectionService.Data;

public class InspectionDbContext : DbContext
{
    public InspectionDbContext(DbContextOptions<InspectionDbContext> options) : base(options)
    {
    }
    
    public DbSet<InspectionPlan> Plans { get; set; }
    public DbSet<InspectionTask> Tasks { get; set; }
    public DbSet<InspectionRecord> Records { get; set; }
    public DbSet<InspectionIssue> Issues { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // InspectionPlan 配置
        modelBuilder.Entity<InspectionPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<string>();
        });
        
        // InspectionTask 配置
        modelBuilder.Entity<InspectionTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TaskNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ScheduledDate);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            
            entity.HasOne(e => e.Plan)
                .WithMany()
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        
        // InspectionRecord 配置
        modelBuilder.Entity<InspectionRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RecordNumber).IsUnique();
            entity.HasIndex(e => e.InspectionDate);
            
            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // InspectionIssue 配置
        modelBuilder.Entity<InspectionIssue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IssueNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.Severity).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            
            entity.HasOne(e => e.Record)
                .WithMany()
                .HasForeignKey(e => e.RecordId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // 种子数据
        SeedData(modelBuilder);
    }
    
    private void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;
        
        // 巡检计划
        modelBuilder.Entity<InspectionPlan>().HasData(
            new InspectionPlan
            {
                Id = 1,
                Name = "A栋日常巡检",
                Description = "A栋公共区域日常巡检，包括大堂、电梯、走廊等",
                Type = InspectionType.Daily,
                Area = "A栋公共区域",
                TargetItems = "电梯运行状态,公共照明,消防设备,卫生状况",
                IntervalDays = 1,
                NextExecutionDate = now.Date.AddDays(1),
                AssignedTo = "张保安",
                EstimatedMinutes = 45,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new InspectionPlan
            {
                Id = 2,
                Name = "消防设备月度巡检",
                Description = "每月对所有消防设备进行全面检查",
                Type = InspectionType.Monthly,
                Area = "全部区域",
                TargetItems = "灭火器压力,消防栓,烟雾报警器,应急照明,疏散指示",
                IntervalDays = 30,
                NextExecutionDate = now.Date.AddDays(15),
                AssignedTo = "李师傅",
                EstimatedMinutes = 120,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new InspectionPlan
            {
                Id = 3,
                Name = "电梯设备周巡检",
                Description = "每周对所有电梯进行安全检查",
                Type = InspectionType.Weekly,
                Area = "A栋,B栋,C栋",
                TargetItems = "电梯运行,门机系统,安全钳,限速器,轿厢环境",
                IntervalDays = 7,
                NextExecutionDate = now.Date.AddDays(3),
                AssignedTo = "王师傅",
                EstimatedMinutes = 90,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new InspectionPlan
            {
                Id = 4,
                Name = "空调系统季度巡检",
                Description = "每季度对中央空调进行全面检查和维护",
                Type = InspectionType.Quarterly,
                Area = "机房,公共区域",
                TargetItems = "主机运行,冷凝水系统,过滤网,出风口,温控器",
                IntervalDays = 90,
                NextExecutionDate = now.Date.AddDays(45),
                AssignedTo = "刘工程师",
                EstimatedMinutes = 180,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        );
        
        // 巡检任务
        modelBuilder.Entity<InspectionTask>().HasData(
            new InspectionTask
            {
                Id = 1,
                TaskNumber = "INS-2026-0001",
                PlanId = 1,
                Title = "A栋日常巡检 - 2026年4月20日",
                Description = "按照日常巡检计划执行",
                Type = InspectionType.Daily,
                Area = "A栋公共区域",
                ScheduledDate = now.Date,
                StartTime = now.Date.AddHours(9),
                EndTime = now.Date.AddHours(10),
                Status = Models.TaskStatus.Completed,
                AssignedTo = "张保安",
                CompletedBy = "张保安",
                Remarks = "巡检完成，一切正常",
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now
            },
            new InspectionTask
            {
                Id = 2,
                TaskNumber = "INS-2026-0002",
                PlanId = 3,
                Title = "电梯周巡检 - 2026年4月第3周",
                Description = "对A栋、B栋电梯进行周检",
                Type = InspectionType.Weekly,
                Area = "A栋,B栋",
                ScheduledDate = now.Date.AddDays(3),
                Status = Models.TaskStatus.Pending,
                AssignedTo = "王师傅",
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now
            },
            new InspectionTask
            {
                Id = 3,
                TaskNumber = "INS-2026-0003",
                PlanId = 2,
                Title = "消防设备月度巡检 - 4月",
                Description = "对所有消防设备进行月度检查",
                Type = InspectionType.Monthly,
                Area = "全部区域",
                ScheduledDate = now.Date.AddDays(15),
                Status = Models.TaskStatus.Pending,
                AssignedTo = "李师傅",
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now
            },
            new InspectionTask
            {
                Id = 4,
                TaskNumber = "INS-2026-0004",
                PlanId = 1,
                Title = "A栋日常巡检 - 2026年4月19日",
                Description = "按照日常巡检计划执行",
                Type = InspectionType.Daily,
                Area = "A栋公共区域",
                ScheduledDate = now.Date.AddDays(-1),
                StartTime = now.Date.AddDays(-1).AddHours(9),
                EndTime = now.Date.AddDays(-1).AddHours(9).AddMinutes(50),
                Status = Models.TaskStatus.Completed,
                AssignedTo = "张保安",
                CompletedBy = "张保安",
                Remarks = "巡检正常",
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-1)
            }
        );
        
        // 巡检记录
        modelBuilder.Entity<InspectionRecord>().HasData(
            new InspectionRecord
            {
                Id = 1,
                TaskId = 1,
                RecordNumber = "REC-2026-0001",
                InspectionDate = now.Date,
                StartTime = now.Date.AddHours(9),
                EndTime = now.Date.AddHours(10),
                Inspector = "张保安",
                TotalItems = 10,
                PassedItems = 9,
                FailedItems = 1,
                Findings = "3楼走廊照明有一盏日光灯不亮",
                Suggestions = "建议更换损坏的日光灯",
                IsPassed = true,
                CreatedAt = now
            },
            new InspectionRecord
            {
                Id = 2,
                TaskId = 4,
                RecordNumber = "REC-2026-0002",
                InspectionDate = now.Date.AddDays(-1),
                StartTime = now.Date.AddDays(-1).AddHours(9),
                EndTime = now.Date.AddDays(-1).AddHours(9).AddMinutes(50),
                Inspector = "张保安",
                TotalItems = 10,
                PassedItems = 10,
                FailedItems = 0,
                Findings = "所有设备运行正常",
                Suggestions = "继续保持",
                IsPassed = true,
                CreatedAt = now.AddDays(-1)
            }
        );
        
        // 巡检问题
        modelBuilder.Entity<InspectionIssue>().HasData(
            new InspectionIssue
            {
                Id = 1,
                RecordId = 1,
                IssueNumber = "ISS-2026-0001",
                Title = "3楼走廊日光灯损坏",
                Description = "3楼走廊靠窗位置的日光灯不亮，影响照明",
                Severity = IssueSeverity.Low,
                Status = IssueStatus.Open,
                Location = "A栋3楼走廊",
                Category = "设施损坏",
                AssignedTo = "维修组",
                DueDate = now.Date.AddDays(3),
                Remarks = "已报修，等待维修",
                CreatedAt = now,
                UpdatedAt = now
            },
            new InspectionIssue
            {
                Id = 2,
                RecordId = null,
                IssueNumber = "ISS-2026-0002",
                Title = "B栋电梯门机异响",
                Description = "B栋2号电梯在关门时存在异常噪音",
                Severity = IssueSeverity.High,
                Status = IssueStatus.InProgress,
                Location = "B栋2号电梯",
                Category = "设备故障",
                Solution = "需要更换门机皮带",
                AssignedTo = "电梯维修单位",
                DueDate = now.Date.AddDays(7),
                Remarks = "已联系电梯维修单位，预计本周内处理",
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-2)
            }
        );
    }
}
