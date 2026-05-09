namespace WO.Property.Shared.Constants;

/// <summary>
/// 系统常量
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// 默认页大小
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// 最大页大小
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// JWT Claim 类型
    /// </summary>
    public static class Claims
    {
        public const string UserId = "sub";
        public const string UserName = "name";
        public const string Role = "role";
        public const string ProjectCode = "project_code";
        public const string ProjectId = "project_id";
    }

    /// <summary>
    /// 状态常量
    /// </summary>
    public static class Status
    {
        public const string Active = "Active";
        public const string Inactive = "Inactive";
        public const string Deleted = "Deleted";
    }

    /// <summary>
    /// 工单状态
    /// </summary>
    public static class TicketStatus
    {
        public const string New = "New";
        public const string Open = "Open";
        public const string Processing = "Processing";
        public const string Pending = "Pending";
        public const string Resolved = "Resolved";
        public const string Closed = "Closed";
        public const string Rejected = "Rejected";
    }

    /// <summary>
    /// 工单优先级
    /// </summary>
    public static class TicketPriority
    {
        public const string Low = "Low";
        public const string Medium = "Medium";
        public const string High = "High";
        public const string Urgent = "Urgent";
    }

    /// <summary>
    /// 工单颜色（用于优先级可视化）
    /// </summary>
    public static class TicketColor
    {
        public const string Green = "green";
        public const string Blue = "blue";
        public const string Orange = "orange";
        public const string Red = "red";
    }

    /// <summary>
    /// 人员类型
    /// </summary>
    public static class PersonType
    {
        public const string Resident = "Resident";
        public const string Employee = "Employee";
        public const string Visitor = "Visitor";
        public const string Merchant = "Merchant";
        public const string Courier = "Courier";
    }

    /// <summary>
    /// 派单角色
    /// </summary>
    public static class DispatchRole
    {
        public const string Operator = "operator";
        public const string Supervisor = "supervisor";
        public const string Manager = "manager";
        public const string DepartmentHead = "department_head";
        public const string CompanyHead = "company_head";
    }

    /// <summary>
    /// 基础数据类型编码
    /// </summary>
    public static class MasterCategory
    {
        public const string Building = "building";
        public const string Room = "room";
        public const string JobType = "job_type";
        public const string Supplier = "supplier";
        public const string DeviceType = "device_type";
        public const string TicketType = "ticket_type";
        public const string Area = "area";
        public const string Department = "department";
    }

    /// <summary>
    /// HTTP Header 名称
    /// </summary>
    public static class Headers
    {
        public const string ProjectCode = "X-Project-Code";
        public const string ProjectId = "X-Project-Id";
        public const string InternalToken = "X-Internal-Service-Token";
    }
}
