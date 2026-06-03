using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using WO.Property.StatisticsService.Tenant;

namespace WO.Property.StatisticsService.Controllers;

/// <summary>
/// 统计报表 API
/// 聚合数据，不写核心业务
/// </summary>
[ApiController]
[Route("api/tenant/statistics")]
[Authorize]
public class StatisticsController : ControllerBase
{
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<StatisticsController> _logger;
    private readonly string _defaultConnectionString;

    public StatisticsController(
        ITenantDbFactory tenantDbFactory,
        ILogger<StatisticsController> logger,
        IConfiguration configuration)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
        _defaultConnectionString = configuration.GetConnectionString("Default")
            ?? "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4";
    }

    private string GetProjectCode()
    {
        return Request.Headers["X-Project"].FirstOrDefault() ?? "wo_property";
    }

    private async Task<MySqlConnection> CreateConnectionAsync()
    {
        var projectCode = GetProjectCode();
        var connStr = _tenantDbFactory.GetTenantConnectionString(projectCode);
        var conn = new MySqlConnection(connStr);
        await conn.OpenAsync();
        return conn;
    }

    /// <summary>
    /// 运营概览 - 今日统计数据
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        try
        {
            await using var conn = await CreateConnectionAsync();

            var today = DateTime.Today;
            var startOfDay = today.ToString("yyyy-MM-dd 00:00:00");
            var endOfDay = today.ToString("yyyy-MM-dd 23:59:59");

            // 今日工单统计
            var ticketStats = await GetTicketStatsAsync(conn, startOfDay, endOfDay);

            // 今日投诉统计
            var complaintStats = await GetComplaintStatsAsync(conn, startOfDay, endOfDay);

            // 今日巡检统计
            var inspectionStats = await GetInspectionStatsAsync(conn, startOfDay, endOfDay);

            // 催单数量
            var urgentCount = await GetUrgentCountAsync(conn, startOfDay, endOfDay);

            // 工程师在线数量
            var engineerOnlineCount = await GetEngineerOnlineCountAsync(conn);

            var result = new
            {
                date = today.ToString("yyyy-MM-dd"),
                tickets = ticketStats,
                complaints = complaintStats,
                inspections = inspectionStats,
                urgentCount,
                engineerOnlineCount,
                timestamp = DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get overview");
            return StatusCode(500, new { error = "获取运营概览失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 工单统计 - 按时段/类型/状态/人员统计工单量
    /// </summary>
    [HttpGet("tickets")]
    public async Task<IActionResult> GetTicketStatistics(
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] string? ticketType = null,
        [FromQuery] string? status = null,
        [FromQuery] int? assigneeId = null,
        [FromQuery] string? groupBy = "day")
    {
        try
        {
            await using var conn = await CreateConnectionAsync();

            var start = string.IsNullOrEmpty(startDate)
                ? DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd")
                : startDate;
            var end = string.IsNullOrEmpty(endDate)
                ? DateTime.Today.ToString("yyyy-MM-dd")
                : endDate;

            var sql = @"
                SELECT 
                    DATE(CreatedAt) as stat_date,
                    TicketType,
                    Status,
                    assignee_id,
                    COUNT(*) as total_count,
                    SUM(CASE WHEN Status = 'completed' THEN 1 ELSE 0 END) as completed_count,
                    SUM(CASE WHEN Status = 'pending' THEN 1 ELSE 0 END) as pending_count,
                    SUM(CASE WHEN Status = 'processing' THEN 1 ELSE 0 END) as processing_count,
                    AVG(TIMESTAMPDIFF(HOUR, CreatedAt, COALESCE(completed_at, NOW()))) as avg_handle_hours
                FROM tickets
                WHERE CreatedAt >= @startDate AND CreatedAt <= @endDate
                AND (@ticketType IS NULL OR TicketType = @ticketType)
                AND (@status IS NULL OR Status = @status)
                AND (@assigneeId IS NULL OR assignee_id = @assigneeId)
                GROUP BY DATE(CreatedAt), TicketType, Status, assignee_id
                ORDER BY stat_date DESC";

            var result = new List<Dictionary<string, object?>>();
            await using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@startDate", start);
                cmd.Parameters.AddWithValue("@endDate", end + " 23:59:59");
                cmd.Parameters.AddWithValue("@ticketType", ticketType ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@status", status ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@assigneeId", assigneeId ?? (object)DBNull.Value);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new Dictionary<string, object?>
                    {
                        ["date"] = reader["stat_date"],
                        ["ticketType"] = reader["TicketType"],
                        ["status"] = reader["Status"],
                        ["assigneeId"] = reader["assignee_id"],
                        ["totalCount"] = reader["total_count"],
                        ["completedCount"] = reader["completed_count"],
                        ["pendingCount"] = reader["pending_count"],
                        ["processingCount"] = reader["processing_count"],
                        ["avgHandleHours"] = reader["avg_handle_hours"]
                    });
                }
            }

            // 按天聚合
            var dailySummary = await GetDailyTicketSummaryAsync(conn, start, end + " 23:59:59");

            // 按类型统计
            var typeSummary = await GetTicketTypeSummaryAsync(conn, start, end + " 23:59:59");

            return Ok(new
            {
                period = new { start, end },
                groupBy,
                details = result,
                dailySummary,
                typeSummary,
                totalCount = result.Sum(r => Convert.ToInt64(r["totalCount"])),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ticket statistics");
            return StatusCode(500, new { error = "获取工单统计失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 工程师工作量 - 维修工接单数量/完成率
    /// </summary>
    [HttpGet("engineers")]
    public async Task<IActionResult> GetEngineerWorkload(
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] int? top = 20)
    {
        try
        {
            await using var conn = await CreateConnectionAsync();

            var start = string.IsNullOrEmpty(startDate)
                ? DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd")
                : startDate;
            var end = string.IsNullOrEmpty(endDate)
                ? DateTime.Today.ToString("yyyy-MM-dd")
                : endDate;

            var sql = @"
                SELECT 
                    p.Id,
                    p.Name,
                    p.EmployeeNo,
                    p.Role,
                    p.Specialties,
                    COUNT(DISTINCT t.Id) as total_tickets,
                    SUM(CASE WHEN t.Status = 'completed' THEN 1 ELSE 0 END) as completed_tickets,
                    SUM(CASE WHEN t.Status = 'pending' THEN 1 ELSE 0 END) as pending_tickets,
                    SUM(CASE WHEN t.Status = 'processing' THEN 1 ELSE 0 END) as processing_tickets,
                    SUM(CASE WHEN t.Status = 'rejected' THEN 1 ELSE 0 END) as rejected_tickets,
                    ROUND(SUM(CASE WHEN t.Status = 'completed' THEN 1 ELSE 0 END) * 100.0 / NULLIF(COUNT(t.Id), 0), 2) as completion_rate,
                    AVG(TIMESTAMPDIFF(HOUR, t.assigned_at, t.completed_at)) as avg_completion_hours,
                    AVG(t.rating) as avg_rating,
                    SUM(CASE WHEN t.rating >= 4 THEN 1 ELSE 0 END) * 100.0 / NULLIF(SUM(CASE WHEN t.rating > 0 THEN 1 ELSE 0 END), 0) as satisfaction_rate
                FROM personnel p
                LEFT JOIN tickets t ON t.assignee_id = p.Id 
                    AND t.assigned_at >= @startDate AND t.assigned_at <= @endDate
                WHERE p.Role IN ('engineer', 'technician', 'operator', 'maintenance')
                GROUP BY p.Id, p.Name, p.EmployeeNo, p.Role, p.Specialties
                ORDER BY total_tickets DESC
                LIMIT @top";

            var result = new List<Dictionary<string, object?>>();
            await using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@startDate", start);
                cmd.Parameters.AddWithValue("@endDate", end + " 23:59:59");
                cmd.Parameters.AddWithValue("@top", top ?? 20);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new Dictionary<string, object?>
                    {
                        ["id"] = reader["Id"],
                        ["name"] = reader["Name"],
                        ["employeeNo"] = reader["EmployeeNo"],
                        ["role"] = reader["Role"],
                        ["specialties"] = reader["Specialties"],
                        ["totalTickets"] = reader["total_tickets"],
                        ["completedTickets"] = reader["completed_tickets"],
                        ["pendingTickets"] = reader["pending_tickets"],
                        ["processingTickets"] = reader["processing_tickets"],
                        ["rejectedTickets"] = reader["rejected_tickets"],
                        ["completionRate"] = reader["completion_rate"],
                        ["avgCompletionHours"] = reader["avg_completion_hours"],
                        ["avgRating"] = reader["avg_rating"],
                        ["satisfactionRate"] = reader["satisfaction_rate"]
                    });
                }
            }

            return Ok(new
            {
                period = new { start, end },
                top = top ?? 20,
                engineers = result,
                totalEngineers = result.Count,
                totalTickets = result.Sum(r => Convert.ToInt64(r["totalTickets"])),
                avgCompletionRate = result.Count > 0 ? Math.Round(result.Average(r => Convert.ToDouble(r["completionRate"] ?? 0)), 2) : 0,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get engineer workload");
            return StatusCode(500, new { error = "获取工程师工作量失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 项目对比 - 多项目数据对比
    /// </summary>
    [HttpGet("projects")]
    public async Task<IActionResult> GetProjectComparison(
        [FromQuery] string? projects = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        try
        {
            // 获取要进行对比的项目列表
            var projectList = string.IsNullOrEmpty(projects)
                ? new[] { "YGHY001", "YGXY001" }
                : projects.Split(',', StringSplitOptions.RemoveEmptyEntries);

            var start = string.IsNullOrEmpty(startDate)
                ? DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd")
                : startDate;
            var end = string.IsNullOrEmpty(endDate)
                ? DateTime.Today.ToString("yyyy-MM-dd")
                : endDate;

            var results = new List<Dictionary<string, object?>>();

            foreach (var projectCode in projectList)
            {
                var connStr = _tenantDbFactory.GetTenantConnectionString(projectCode.Trim());
                await using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();

                var stats = await GetProjectStatsAsync(conn, start, end + " 23:59:59");
                stats["projectCode"] = projectCode.Trim();
                results.Add(stats);
            }

            return Ok(new
            {
                period = new { start, end },
                projects = results,
                comparison = new
                {
                    totalTickets = results.Sum(r => Convert.ToInt64(r["totalTickets"])),
                    completedTickets = results.Sum(r => Convert.ToInt64(r["completedTickets"])),
                    avgCompletionRate = results.Count > 0 ? Math.Round(results.Average(r => Convert.ToDouble(r["completionRate"] ?? 0)), 2) : 0,
                    avgSatisfactionRate = results.Count > 0 ? Math.Round(results.Average(r => Convert.ToDouble(r["satisfactionRate"] ?? 0)), 2) : 0
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get project comparison");
            return StatusCode(500, new { error = "获取项目对比失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 趋势数据 - 指定天数内的数据趋势
    /// </summary>
    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends([FromQuery] int days = 30)
    {
        try
        {
            await using var conn = await CreateConnectionAsync();

            var start = DateTime.Today.AddDays(-days).ToString("yyyy-MM-dd");
            var end = DateTime.Today.ToString("yyyy-MM-dd");

            // 工单趋势
            var ticketTrend = await GetTicketTrendAsync(conn, start, end + " 23:59:59");

            // 投诉趋势
            var complaintTrend = await GetComplaintTrendAsync(conn, start, end + " 23:59:59");

            // 巡检趋势
            var inspectionTrend = await GetInspectionTrendAsync(conn, start, end + " 23:59:59");

            // 收入趋势 (从 metric_snapshots)
            var revenueTrend = await GetRevenueTrendAsync(conn, start, end);

            // 满员率趋势
            var occupancyTrend = await GetOccupancyTrendAsync(conn, start, end);

            return Ok(new
            {
                period = new { start, end, days },
                trends = new
                {
                    tickets = ticketTrend,
                    complaints = complaintTrend,
                    inspections = inspectionTrend,
                    revenue = revenueTrend,
                    occupancy = occupancyTrend
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get trends");
            return StatusCode(500, new { error = "获取趋势数据失败", message = ex.Message });
        }
    }

    // ==================== 私有辅助方法 ====================

    private async Task<Dictionary<string, object?>> GetTicketStatsAsync(MySqlConnection conn, string startOfDay, string endOfDay)
    {
        var result = new Dictionary<string, object?>();

        var sql = @"
            SELECT 
                COUNT(*) as total,
                SUM(CASE WHEN Status = 'completed' THEN 1 ELSE 0 END) as completed,
                SUM(CASE WHEN Status = 'pending' THEN 1 ELSE 0 END) as pending,
                SUM(CASE WHEN Status = 'processing' THEN 1 ELSE 0 END) as processing,
                SUM(CASE WHEN Status = 'rejected' THEN 1 ELSE 0 END) as rejected
            FROM tickets
            WHERE CreatedAt >= @start AND CreatedAt <= @end";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@start", startOfDay);
        cmd.Parameters.AddWithValue("@end", endOfDay);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            result["total"] = reader["total"];
            result["completed"] = reader["completed"];
            result["pending"] = reader["pending"];
            result["processing"] = reader["processing"];
            result["rejected"] = reader["rejected"];
            result["completionRate"] = reader["total"] is 0 or DBNull ? 0 : Math.Round(Convert.ToDecimal(reader["completed"]) * 100 / Convert.ToDecimal(reader["total"]), 2);
        }

        return result;
    }

    private async Task<Dictionary<string, object?>> GetComplaintStatsAsync(MySqlConnection conn, string startOfDay, string endOfDay)
    {
        var result = new Dictionary<string, object?>();

        var sql = @"
            SELECT 
                COUNT(*) as total,
                SUM(CASE WHEN Status = 'resolved' THEN 1 ELSE 0 END) as resolved,
                SUM(CASE WHEN Status = 'pending' THEN 1 ELSE 0 END) as pending,
                SUM(CASE WHEN Status = 'processing' THEN 1 ELSE 0 END) as processing
            FROM complaints
            WHERE CreatedAt >= @start AND CreatedAt <= @end";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@start", startOfDay);
        cmd.Parameters.AddWithValue("@end", endOfDay);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            result["total"] = reader["total"];
            result["resolved"] = reader["resolved"];
            result["pending"] = reader["pending"];
            result["processing"] = reader["processing"];
            result["resolutionRate"] = reader["total"] is 0 or DBNull ? 0 : Math.Round(Convert.ToDecimal(reader["resolved"]) * 100 / Convert.ToDecimal(reader["total"]), 2);
        }

        return result;
    }

    private async Task<Dictionary<string, object?>> GetInspectionStatsAsync(MySqlConnection conn, string startOfDay, string endOfDay)
    {
        var result = new Dictionary<string, object?>();

        var sql = @"
            SELECT 
                COUNT(*) as total,
                SUM(CASE WHEN Status = 'passed' THEN 1 ELSE 0 END) as passed,
                SUM(CASE WHEN Status = 'failed' THEN 1 ELSE 0 END) as failed
            FROM InspectionRecords
            WHERE CreatedAt >= @start AND CreatedAt <= @end";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@start", startOfDay);
        cmd.Parameters.AddWithValue("@end", endOfDay);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            result["total"] = reader["total"];
            result["passed"] = reader["passed"];
            result["failed"] = reader["failed"];
            result["passRate"] = reader["total"] is 0 or DBNull ? 0 : Math.Round(Convert.ToDecimal(reader["passed"]) * 100 / Convert.ToDecimal(reader["total"]), 2);
        }

        return result;
    }

    private async Task<int> GetUrgentCountAsync(MySqlConnection conn, string startOfDay, string endOfDay)
    {
        var sql = @"
            SELECT COUNT(*) FROM tickets
            WHERE CreatedAt >= @start AND CreatedAt <= @end
            AND (escalation_level > 0 OR Priority = 'urgent' OR Priority = 'high')";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@start", startOfDay);
        cmd.Parameters.AddWithValue("@end", endOfDay);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private async Task<int> GetEngineerOnlineCountAsync(MySqlConnection conn)
    {
        var sql = @"SELECT COUNT(*) FROM personnel WHERE Role IN ('engineer', 'technician', 'operator', 'maintenance') AND Status = 'active'";

        await using var cmd = new MySqlCommand(sql, conn);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private async Task<List<Dictionary<string, object?>>> GetDailyTicketSummaryAsync(MySqlConnection conn, string start, string end)
    {
        var result = new List<Dictionary<string, object?>>();

        var sql = @"
            SELECT 
                DATE(CreatedAt) as stat_date,
                COUNT(*) as total,
                SUM(CASE WHEN Status = 'completed' THEN 1 ELSE 0 END) as completed,
                SUM(CASE WHEN Status = 'pending' THEN 1 ELSE 0 END) as pending,
                SUM(CASE WHEN Status = 'processing' THEN 1 ELSE 0 END) as processing
            FROM tickets
            WHERE CreatedAt >= @start AND CreatedAt <= @end
            GROUP BY DATE(CreatedAt)
            ORDER BY stat_date DESC";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@start", start);
        cmd.Parameters.AddWithValue("@end", end);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new Dictionary<string, object?>
            {
                ["date"] = reader["stat_date"],
                ["total"] = reader["total"],
                ["completed"] = reader["completed"],
                ["pending"] = reader["pending"],
                ["processing"] = reader["processing"]
            });
        }

        return result;
    }

    private async Task<List<Dictionary<string, object?>>> GetTicketTypeSummaryAsync(MySqlConnection conn, string start, string end)
    {
        var result = new List<Dictionary<string, object?>>();

        var sql = @"
            SELECT 
                COALESCE(TicketType, 'unknown') as ticket_type,
                COUNT(*) as total,
                SUM(CASE WHEN Status = 'completed' THEN 1 ELSE 0 END) as completed
            FROM tickets
            WHERE CreatedAt >= @start AND CreatedAt <= @end
            GROUP BY TicketType
            ORDER BY total DESC";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@start", start);
        cmd.Parameters.AddWithValue("@end", end);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new Dictionary<string, object?>
            {
                ["type"] = reader["ticket_type"],
                ["total"] = reader["total"],
                ["completed"] = reader["completed"],
                ["rate"] = reader["total"] is 0 or DBNull ? 0 : Math.Round(Convert.ToDecimal(reader["completed"]) * 100 / Convert.ToDecimal(reader["total"]), 2)
            });
        }

        return result;
    }

    private async Task<Dictionary<string, object?>> GetProjectStatsAsync(MySqlConnection conn, string start, string end)
    {
        var result = new Dictionary<string, object?>();

        // 工单统计
        var ticketSql = @"
            SELECT 
                COUNT(*) as total_tickets,
                SUM(CASE WHEN Status = 'completed' THEN 1 ELSE 0 END) as completed_tickets,
                ROUND(SUM(CASE WHEN Status = 'completed' THEN 1 ELSE 0 END) * 100.0 / NULLIF(COUNT(*), 0), 2) as completion_rate
            FROM tickets
            WHERE CreatedAt >= @start AND CreatedAt <= @end";

        await using var ticketCmd = new MySqlCommand(ticketSql, conn);
        ticketCmd.Parameters.AddWithValue("@start", start);
        ticketCmd.Parameters.AddWithValue("@end", end);

        await using var ticketReader = await ticketCmd.ExecuteReaderAsync();
        if (await ticketReader.ReadAsync())
        {
            result["totalTickets"] = ticketReader["total_tickets"];
            result["completedTickets"] = ticketReader["completed_tickets"];
            result["completionRate"] = ticketReader["completion_rate"];
        }

        await ticketReader.CloseAsync();

        // 投诉统计
        var complaintSql = @"
            SELECT 
                COUNT(*) as total_complaints,
                SUM(CASE WHEN Status = 'resolved' THEN 1 ELSE 0 END) as resolved_complaints
            FROM complaints
            WHERE CreatedAt >= @start AND CreatedAt <= @end";

        await using var complaintCmd = new MySqlCommand(complaintSql, conn);
        complaintCmd.Parameters.AddWithValue("@start", start);
        complaintCmd.Parameters.AddWithValue("@end", end);

        await using var complaintReader = await complaintCmd.ExecuteReaderAsync();
        if (await complaintReader.ReadAsync())
        {
            result["totalComplaints"] = complaintReader["total_complaints"];
            result["resolvedComplaints"] = complaintReader["resolved_complaints"];
        }

        return result;
    }

    private async Task<List<Dictionary<string, object?>>> GetTicketTrendAsync(MySqlConnection conn, string start, string end)
    {
        var result = new List<Dictionary<string, object?>>();

        var sql = @"
            SELECT 
                DATE(CreatedAt) as stat_date,
                COUNT(*) as total,
                SUM(CASE WHEN Status = 'completed' THEN 1 ELSE 0 END) as completed
            FROM tickets
            WHERE CreatedAt >= @start AND CreatedAt <= @end
            GROUP BY DATE(CreatedAt)
            ORDER BY stat_date";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@start", start);
        cmd.Parameters.AddWithValue("@end", end);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new Dictionary<string, object?>
            {
                ["date"] = reader["stat_date"],
                ["total"] = reader["total"],
                ["completed"] = reader["completed"]
            });
        }

        return result;
    }

    private async Task<List<Dictionary<string, object?>>> GetComplaintTrendAsync(MySqlConnection conn, string start, string end)
    {
        var result = new List<Dictionary<string, object?>>();

        var sql = @"
            SELECT 
                DATE(CreatedAt) as stat_date,
                COUNT(*) as total,
                SUM(CASE WHEN Status = 'resolved' THEN 1 ELSE 0 END) as resolved
            FROM complaints
            WHERE CreatedAt >= @start AND CreatedAt <= @end
            GROUP BY DATE(CreatedAt)
            ORDER BY stat_date";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@start", start);
        cmd.Parameters.AddWithValue("@end", end);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new Dictionary<string, object?>
            {
                ["date"] = reader["stat_date"],
                ["total"] = reader["total"],
                ["resolved"] = reader["resolved"]
            });
        }

        return result;
    }

    private async Task<List<Dictionary<string, object?>>> GetInspectionTrendAsync(MySqlConnection conn, string start, string end)
    {
        var result = new List<Dictionary<string, object?>>();

        var sql = @"
            SELECT 
                DATE(CreatedAt) as stat_date,
                COUNT(*) as total,
                SUM(CASE WHEN Status = 'passed' THEN 1 ELSE 0 END) as passed
            FROM InspectionRecords
            WHERE CreatedAt >= @start AND CreatedAt <= @end
            GROUP BY DATE(CreatedAt)
            ORDER BY stat_date";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@start", start);
        cmd.Parameters.AddWithValue("@end", end);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new Dictionary<string, object?>
            {
                ["date"] = reader["stat_date"],
                ["total"] = reader["total"],
                ["passed"] = reader["passed"]
            });
        }

        return result;
    }

    private async Task<List<Dictionary<string, object?>>> GetRevenueTrendAsync(MySqlConnection conn, string start, string end)
    {
        var result = new List<Dictionary<string, object?>>();

        var sql = @"
            SELECT 
                snapshot_date as stat_date,
                total_revenue,
                property_fee_revenue,
                other_revenue,
                collection_rate
            FROM metric_snapshots
            WHERE snapshot_date >= @start AND snapshot_date <= @end
            ORDER BY snapshot_date";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@start", start);
        cmd.Parameters.AddWithValue("@end", end);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new Dictionary<string, object?>
            {
                ["date"] = reader["stat_date"],
                ["totalRevenue"] = reader["total_revenue"],
                ["propertyFeeRevenue"] = reader["property_fee_revenue"],
                ["otherRevenue"] = reader["other_revenue"],
                ["collectionRate"] = reader["collection_rate"]
            });
        }

        return result;
    }

    private async Task<List<Dictionary<string, object?>>> GetOccupancyTrendAsync(MySqlConnection conn, string start, string end)
    {
        var result = new List<Dictionary<string, object?>>();

        var sql = @"
            SELECT 
                snapshot_date as stat_date,
                occupancy_rate
            FROM metric_snapshots
            WHERE snapshot_date >= @start AND snapshot_date <= @end
            ORDER BY snapshot_date";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@start", start);
        cmd.Parameters.AddWithValue("@end", end);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new Dictionary<string, object?>
            {
                ["date"] = reader["stat_date"],
                ["occupancyRate"] = reader["occupancy_rate"]
            });
        }

        return result;
    }
}