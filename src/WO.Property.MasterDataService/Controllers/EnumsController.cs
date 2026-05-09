using Microsoft.AspNetCore.Mvc;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 枚举值API - 提供字段相关的枚举选项
/// </summary>
[ApiController]
[Route("api/enums")]
public class EnumsController : ControllerBase
{
    /// <summary>
    /// 字段类型枚举
    /// </summary>
    [HttpGet("field-types")]
    public IActionResult GetFieldTypes()
    {
        var types = new[]
        {
            new { Value = "text", Label = "文本输入框" },
            new { Value = "number", Label = "数字输入框" },
            new { Value = "date", Label = "日期选择器" },
            new { Value = "select", Label = "下拉选择框" },
            new { Value = "textarea", Label = "多行文本框" }
        };

        return Ok(new { Success = true, Data = types });
    }

    /// <summary>
    /// 来源枚举
    /// </summary>
    [HttpGet("field-sources")]
    public IActionResult GetFieldSources()
    {
        var sources = new[]
        {
            new { Value = "System", Label = "系统内置" },
            new { Value = "PersonService", Label = "人员服务" },
            new { Value = "MasterDataService", Label = "主数据服务" },
            new { Value = "TicketService", Label = "工单服务" },
            new { Value = "ComplaintService", Label = "投诉服务" },
            new { Value = "DeviceService", Label = "设备服务" },
            new { Value = "ContractService", Label = "合同服务" },
            new { Value = "FinanceService", Label = "财务服务" },
            new { Value = "InspectionService", Label = "巡检服务" },
            new { Value = "NotificationService", Label = "通知服务" }
        };

        return Ok(new { Success = true, Data = sources });
    }

    /// <summary>
    /// 状态枚举
    /// </summary>
    [HttpGet("field-statuses")]
    public IActionResult GetFieldStatuses()
    {
        var statuses = new[]
        {
            new { Value = "Active", Label = "激活" },
            new { Value = "Inactive", Label = "停用" }
        };

        return Ok(new { Success = true, Data = statuses });
    }

    /// <summary>
    /// 工单类型枚举（来自 TicketService）
    /// </summary>
    [HttpGet("ticket-types")]
    public IActionResult GetTicketTypes()
    {
        var types = new[]
        {
            new { Value = "Repair", Label = "报修" },
            new { Value = "Cleaning", Label = "保洁" },
            new { Value = "Security", Label = "安保" },
            new { Value = "Gardening", Label = "绿化" },
            new { Value = "Other", Label = "其他" }
        };

        return Ok(new { Success = true, Data = types });
    }


    /// <summary>
    /// 优先级枚举（来自 TicketService）
    /// </summary>
    [HttpGet("priorities")]
    public IActionResult GetPriorities()
    {
        var priorities = new[]
        {
            new { Value = "Low", Label = "低" },
            new { Value = "Medium", Label = "中" },
            new { Value = "High", Label = "高" },
            new { Value = "Urgent", Label = "紧急" }
        };
        return Ok(new { Success = true, Data = priorities });
    }

    /// <summary>
    /// 工单状态枚举（来自 TicketService）
    /// </summary>
    [HttpGet("ticket-statuses")]
    public IActionResult GetTicketStatuses()
    {
        var statuses = new[]
        {
            new { Value = "Created", Label = "已创建" },
            new { Value = "Dispatched", Label = "已派单" },
            new { Value = "Accepted", Label = "已接单" },
            new { Value = "Processing", Label = "处理中" },
            new { Value = "Finished", Label = "已完成" },
            new { Value = "Confirmed", Label = "已确认" },
            new { Value = "Closed", Label = "已关闭" },
            new { Value = "Cancelled", Label = "已取消" }
        };
        return Ok(new { Success = true, Data = statuses });
    }
}