using System.Text.Json;
using WO.Property.MasterDataService.Models;

namespace WO.Property.MasterDataService.Data;

/// <summary>
/// 字段定义初始数据种子
/// </summary>
public static class FieldDefinitionSeedData
{
    /// <summary>
    /// 获取所有需要预置的字段定义
    /// </summary>
    public static List<FieldDefinition> GetSeedData()
    {
        var fields = new List<FieldDefinition>();

        // ==================== 共享基础字段 ====================

        // name (姓名)
        fields.Add(CreateField("name", "姓名", "text", "PersonService", true, null, null, null, false, 100, 1));

        // phone (联系电话)
        fields.Add(CreateField("phone", "联系电话", "text", "PersonService", true, null, null, null, false, 100, 2));

        // email (邮箱)
        fields.Add(CreateField("email", "邮箱", "text", "PersonService", true, null, null, null, false, 150, 3));

        // idCard (身份证号)
        fields.Add(CreateField("idCard", "身份证号", "text", "PersonService", true, null, null, null, false, 150, 4));

        // status (状态)
        fields.Add(CreateField("status", "状态", "select", "System", true, null,
            JsonSerializer.Serialize(new[] { "Active", "Inactive" }), "Active", false, 80, 5));

        // remark (备注)
        fields.Add(CreateField("remark", "备注", "textarea", "System", true, null, null, null, false, 200, 6));

        // createdAt (创建时间)
        fields.Add(CreateField("createdAt", "创建时间", "date", "System", true, null, null, null, false, 120, 7));

        // updatedAt (更新时间)
        fields.Add(CreateField("updatedAt", "更新时间", "date", "System", true, null, null, null, false, 120, 8));

        // ==================== 共享业务字段（来自 PersonService） ====================

        // department (部门)
        fields.Add(CreateField("department", "部门", "select", "PersonService", true, null,
            JsonSerializer.Serialize(new[] { "工程部", "客服部", "安保部", "保洁部", "行政部", "财务部" }),
            null, false, 100, 9));

        // role (职位)
        fields.Add(CreateField("role", "职位", "select", "PersonService", true, null,
            JsonSerializer.Serialize(new[] { "operator", "supervisor", "manager", "director" }),
            null, false, 100, 10));

        // gender (性别)
        fields.Add(CreateField("gender", "性别", "select", "PersonService", true, null,
            JsonSerializer.Serialize(new[] { "男", "女" }),
            null, false, 60, 11));

        // joinDate (入职日期)
        fields.Add(CreateField("joinDate", "入职日期", "date", "PersonService", true, null, null, null, false, 120, 12));

        // emergencyContact (紧急联系人)
        fields.Add(CreateField("emergencyContact", "紧急联系人", "text", "PersonService", true, null, null, null, false, 100, 13));

        // emergencyPhone (紧急联系电话)
        fields.Add(CreateField("emergencyPhone", "紧急联系电话", "text", "PersonService", true, null, null, null, false, 120, 14));

        // ==================== 共享业务字段（来自 MasterDataService） ====================

        // roomNo (房号)
        fields.Add(CreateField("roomNo", "房号", "text", "MasterDataService", true, null, null, null, false, 80, 15));

        // buildingId (楼栋)
        fields.Add(CreateField("buildingId", "楼栋", "select", "MasterDataService", true, null,
            JsonSerializer.Serialize(new[] { "1栋", "2栋", "3栋", "4栋", "5栋", "A栋", "B栋", "C栋" }),
            null, false, 80, 16));

        // floor (楼层)
        fields.Add(CreateField("floor", "楼层", "text", "MasterDataService", true, null, null, null, false, 60, 17));

        // location (位置)
        fields.Add(CreateField("location", "位置", "text", "MasterDataService", true, null, null, null, false, 150, 18));

        // type (类型)
        fields.Add(CreateField("type", "类型", "select", "MasterDataService", true, null,
            JsonSerializer.Serialize(new[] { "日常", "紧急", "计划", "临时" }),
            null, false, 80, 19));

        // priority (优先级)
        fields.Add(CreateField("priority", "优先级", "select", "MasterDataService", true, null,
            JsonSerializer.Serialize(new[] { "Low", "Medium", "High", "Urgent" }),
            "Medium", false, 80, 20));

        // category (分类)
        fields.Add(CreateField("category", "分类", "select", "MasterDataService", true, null,
            JsonSerializer.Serialize(new[] { "设备维修", "设施维护", "投诉建议", "咨询服务", "其他" }),
            null, false, 100, 21));

        // level (等级)
        fields.Add(CreateField("level", "等级", "select", "MasterDataService", true, null,
            JsonSerializer.Serialize(new[] { "一级", "二级", "三级", "四级", "五级" }),
            null, false, 60, 22));

        // unit (单位)
        fields.Add(CreateField("unit", "单位", "select", "MasterDataService", true, null,
            JsonSerializer.Serialize(new[] { "个", "台", "套", "平方米", "米", "公斤" }),
            null, false, 60, 23));

        // cycle (周期)
        fields.Add(CreateField("cycle", "周期", "select", "MasterDataService", true, null,
            JsonSerializer.Serialize(new[] { "每日", "每周", "每月", "每季度", "每年" }),
            null, false, 80, 24));

        // ==================== 私有字段（工单模块 ticket） ====================

        // ticketNo (工单编号)
        fields.Add(CreateField("ticketNo", "工单编号", "text", "TicketService", false, "ticket", null, null, false, 120, 25));

        // title (工单标题)
        fields.Add(CreateField("title", "工单标题", "text", "TicketService", false, "ticket", null, null, false, 200, 26));

        // description (工单描述)
        fields.Add(CreateField("description", "工单描述", "textarea", "TicketService", false, "ticket", null, null, false, 300, 27));

        // createTime (创建时间) - 工单私有版本
        fields.Add(CreateField("createTime", "创建时间", "date", "System", false, "ticket", null, null, false, 120, 28));

        // assigneeName (指派人)
        fields.Add(CreateField("assigneeName", "指派人", "text", "PersonService", false, "ticket", null, null, false, 100, 29));

        // handleTime (处理时间)
        fields.Add(CreateField("handleTime", "处理时间", "date", "TicketService", false, "ticket", null, null, false, 120, 30));

        // completeTime (完成时间)
        fields.Add(CreateField("completeTime", "完成时间", "date", "TicketService", false, "ticket", null, null, false, 120, 31));

        // contactName (联系人)
        fields.Add(CreateField("contactName", "联系人", "text", "PersonService", false, "ticket", null, null, false, 100, 32));

        // contactPhone (联系电话) - 工单私有版本
        fields.Add(CreateField("contactPhone", "联系电话", "text", "PersonService", false, "ticket", null, null, false, 120, 33));

        return fields;
    }

    private static FieldDefinition CreateField(
        string fieldKey,
        string displayName,
        string fieldType,
        string source,
        bool isShared,
        string? module,
        string? options,
        string? defaultValue,
        bool isRequired,
        int width,
        int sortOrder)
    {
        return new FieldDefinition
        {
            FieldKey = fieldKey,
            DisplayName = displayName,
            FieldType = fieldType,
            Source = source,
            IsShared = isShared,
            Module = module,
            Options = options,
            DefaultValue = defaultValue,
            IsRequired = isRequired,
            Width = width,
            SortOrder = sortOrder,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 获取工单模块的初始字段关联数据
    /// </summary>
    public static List<ModuleField> GetTicketModuleFields(List<FieldDefinition> fields)
    {
        var moduleFields = new List<ModuleField>();

        // 工单模块使用以下字段
        var ticketFields = new[]
        {
            "ticketNo", "title", "description", "createTime", "assigneeName",
            "handleTime", "completeTime", "contactName", "contactPhone",
            "priority", "status", "remark", "createdAt"
        };

        var fieldDict = fields.ToDictionary(f => f.FieldKey);

        int sortOrder = 0;
        foreach (var fieldKey in ticketFields)
        {
            if (fieldDict.TryGetValue(fieldKey, out var fieldDef))
            {
                moduleFields.Add(new ModuleField
                {
                    Module = "ticket",
                    FieldDefinitionId = fieldDef.Id,
                    IsVisible = true,
                    IsActive = true,
                    SortOrder = sortOrder++,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        return moduleFields;
    }
}