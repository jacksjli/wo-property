using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Text.Json;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 人员管理API
/// </summary>
[ApiController]
[Route("api/personnel")]
public class PersonnelController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<PersonnelController> _logger;

    public PersonnelController(MySqlConnection db, ILogger<PersonnelController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        [FromQuery] int? departmentId = null,
        [FromQuery] string? keyword = null)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(role))
        {
            conditions.Add("Role = @role");
            parameters.Add(new MySqlParameter("@role", role));
        }

        if (!string.IsNullOrEmpty(status))
        {
            conditions.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        if (departmentId.HasValue)
        {
            conditions.Add("DepartmentId = @departmentId");
            parameters.Add(new MySqlParameter("@departmentId", departmentId.Value));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(Name LIKE @keyword OR EmployeeNo LIKE @keyword OR Phone LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM Personnel {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT * FROM Personnel
            {whereClause}
            ORDER BY FIELD(Role, 'company_head', 'department_head', 'manager', 'supervisor', 'operator'), Name
            LIMIT @offset, @pageSize";

        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<PersonnelItem>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapPersonnel(reader));

        return Ok(new
        {
            success = true,
            data = items,
            pagination = new { page, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        using var cmd = new MySqlCommand("SELECT * FROM Personnel WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return Ok(new { success = true, data = MapPersonnel(reader) });
        return NotFound(new { success = false, message = "人员不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PersonnelItem req)
    {
        if (string.IsNullOrEmpty(req.Name))
            return BadRequest(new { success = false, message = "姓名不能为空" });
        if (string.IsNullOrEmpty(req.Phone))
            return BadRequest(new { success = false, message = "联系电话不能为空" });

        // 检查手机号是否已存在
        using (var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Personnel WHERE Phone = @phone", _db))
        {
            checkCmd.Parameters.AddWithValue("@phone", req.Phone);
            if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0)
                return BadRequest(new { success = false, message = "该手机号已存在" });
        }

        var sql = @"INSERT INTO Personnel
            (EmployeeNo, Name, Avatar, Gender, Birthday, IdCard, Phone, Email, Address, Education, GraduateSchool, Major,
             Role, DepartmentId, DepartmentName, Position, EmploymentType, HireDate, ContractStart, ContractEnd, Salary,
             BankAccount, SocialSecurityNo, Status, Specialties, Backups, EmergencyContactName, EmergencyContactRelationship,
             EmergencyContactPhone, AttendanceCount, OvertimeHours, LeaveDays, PerformanceScore, TrainingCount, Remark, CreatedAt)
            VALUES
            (@EmployeeNo, @Name, @Avatar, @Gender, @Birthday, @IdCard, @Phone, @Email, @Address, @Education, @GraduateSchool, @Major,
             @Role, @DepartmentId, @DepartmentName, @Position, @EmploymentType, @HireDate, @ContractStart, @ContractEnd, @Salary,
             @BankAccount, @SocialSecurityNo, @Status, @Specialties, @Backups, @EmergencyContactName, @EmergencyContactRelationship,
             @EmergencyContactPhone, @AttendanceCount, @OvertimeHours, @LeaveDays, @PerformanceScore, @TrainingCount, @Remark, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@EmployeeNo", req.EmployeeNo);
        cmd.Parameters.AddWithValue("@Name", req.Name);
        cmd.Parameters.AddWithValue("@Avatar", (object)req.Avatar ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Gender", req.Gender ?? "male");
        cmd.Parameters.AddWithValue("@Birthday", (object)req.Birthday ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IdCard", (object)req.IdCard ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Phone", req.Phone);
        cmd.Parameters.AddWithValue("@Email", (object)req.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Address", (object)req.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Education", req.Education ?? "bachelor");
        cmd.Parameters.AddWithValue("@GraduateSchool", (object)req.GraduateSchool ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Major", (object)req.Major ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Role", req.Role ?? "operator");
        cmd.Parameters.AddWithValue("@DepartmentId", (object)req.DepartmentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DepartmentName", (object)req.DepartmentName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Position", (object)req.Position ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EmploymentType", req.EmploymentType ?? "full_time");
        cmd.Parameters.AddWithValue("@HireDate", (object)req.HireDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ContractStart", (object)req.ContractStart ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ContractEnd", (object)req.ContractEnd ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Salary", (object)req.Salary ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@BankAccount", (object)req.BankAccount ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SocialSecurityNo", (object)req.SocialSecurityNo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", req.Status ?? "probation");
        cmd.Parameters.AddWithValue("@Specialties", (object)req.Specialties ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Backups", (object)req.Backups ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EmergencyContactName", (object)req.EmergencyContactName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EmergencyContactRelationship", (object)req.EmergencyContactRelationship ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EmergencyContactPhone", (object)req.EmergencyContactPhone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AttendanceCount", req.AttendanceCount);
        cmd.Parameters.AddWithValue("@OvertimeHours", req.OvertimeHours);
        cmd.Parameters.AddWithValue("@LeaveDays", req.LeaveDays);
        cmd.Parameters.AddWithValue("@PerformanceScore", (object)req.PerformanceScore ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TrainingCount", req.TrainingCount);
        cmd.Parameters.AddWithValue("@Remark", (object)req.Remark ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建人员: {Id} - {Name}", id, req.Name);
        return CreatedAtAction(nameof(GetById), new { id }, new { success = true, message = "人员创建成功", data = new { id } });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PersonnelItem req)
    {
        using var checkCmd = new MySqlCommand("SELECT * FROM Personnel WHERE Id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        using var reader = await checkCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFound(new { success = false, message = "人员不存在" });
        reader.Close(); // 关闭 reader 才能执行下一个命令

        var updates = new List<string> { "UpdatedAt = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id), new MySqlParameter("@updatedAt", DateTime.UtcNow) };

        if (!string.IsNullOrEmpty(req.Name)) { updates.Add("Name = @name"); parameters.Add(new MySqlParameter("@name", req.Name)); }
        if (!string.IsNullOrEmpty(req.Gender)) { updates.Add("Gender = @gender"); parameters.Add(new MySqlParameter("@gender", req.Gender)); }
        if (!string.IsNullOrEmpty(req.Birthday)) { updates.Add("Birthday = @birthday"); parameters.Add(new MySqlParameter("@birthday", req.Birthday)); }
        if (!string.IsNullOrEmpty(req.IdCard)) { updates.Add("IdCard = @idCard"); parameters.Add(new MySqlParameter("@idCard", req.IdCard)); }
        if (!string.IsNullOrEmpty(req.Phone)) { updates.Add("Phone = @phone"); parameters.Add(new MySqlParameter("@phone", req.Phone)); }
        if (!string.IsNullOrEmpty(req.Email)) { updates.Add("Email = @email"); parameters.Add(new MySqlParameter("@email", req.Email)); }
        if (!string.IsNullOrEmpty(req.Address)) { updates.Add("Address = @address"); parameters.Add(new MySqlParameter("@address", req.Address)); }
        if (!string.IsNullOrEmpty(req.Education)) { updates.Add("Education = @education"); parameters.Add(new MySqlParameter("@education", req.Education)); }
        if (!string.IsNullOrEmpty(req.GraduateSchool)) { updates.Add("GraduateSchool = @graduateSchool"); parameters.Add(new MySqlParameter("@graduateSchool", req.GraduateSchool)); }
        if (!string.IsNullOrEmpty(req.Major)) { updates.Add("Major = @major"); parameters.Add(new MySqlParameter("@major", req.Major)); }
        if (!string.IsNullOrEmpty(req.Role)) { updates.Add("Role = @role"); parameters.Add(new MySqlParameter("@role", req.Role)); }
        if (req.DepartmentId.HasValue) { updates.Add("DepartmentId = @departmentId"); parameters.Add(new MySqlParameter("@departmentId", req.DepartmentId.Value)); }
        if (!string.IsNullOrEmpty(req.DepartmentName)) { updates.Add("DepartmentName = @departmentName"); parameters.Add(new MySqlParameter("@departmentName", req.DepartmentName)); }
        if (!string.IsNullOrEmpty(req.Position)) { updates.Add("Position = @position"); parameters.Add(new MySqlParameter("@position", req.Position)); }
        if (!string.IsNullOrEmpty(req.EmploymentType)) { updates.Add("EmploymentType = @employmentType"); parameters.Add(new MySqlParameter("@employmentType", req.EmploymentType)); }
        if (!string.IsNullOrEmpty(req.HireDate)) { updates.Add("HireDate = @hireDate"); parameters.Add(new MySqlParameter("@hireDate", req.HireDate)); }
        if (!string.IsNullOrEmpty(req.ContractStart)) { updates.Add("ContractStart = @contractStart"); parameters.Add(new MySqlParameter("@contractStart", req.ContractStart)); }
        if (!string.IsNullOrEmpty(req.ContractEnd)) { updates.Add("ContractEnd = @contractEnd"); parameters.Add(new MySqlParameter("@contractEnd", req.ContractEnd)); }
        if (req.Salary.HasValue) { updates.Add("Salary = @salary"); parameters.Add(new MySqlParameter("@salary", req.Salary.Value)); }
        if (!string.IsNullOrEmpty(req.Status)) { updates.Add("Status = @status"); parameters.Add(new MySqlParameter("@status", req.Status)); }
        if (req.Specialties != null) { updates.Add("Specialties = @specialties"); parameters.Add(new MySqlParameter("@specialties", req.Specialties)); }
        if (req.Backups != null) { updates.Add("Backups = @backups"); parameters.Add(new MySqlParameter("@backups", req.Backups)); }
        if (!string.IsNullOrEmpty(req.Remark)) { updates.Add("Remark = @remark"); parameters.Add(new MySqlParameter("@remark", req.Remark)); }

        var sql = $"UPDATE Personnel SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新人员: {Id}", id);
        return Ok(new { success = true, message = "人员更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM Personnel WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { success = false, message = "人员不存在" });
        _logger.LogInformation("删除人员: {Id}", id);
        return Ok(new { success = true, message = "人员已删除" });
    }

    [HttpPost("{id}/backups")]
    public async Task<IActionResult> AddBackup(int id, [FromBody] BackupRequest req)
    {
        using var checkCmd = new MySqlCommand("SELECT Backups FROM Personnel WHERE Id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        using var reader = await checkCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFound(new { success = false, message = "人员不存在" });
        reader.Close(); // 关闭 reader 才能执行下一个命令

        var existingBackups = reader["Backups"] as string;
        var backups = string.IsNullOrEmpty(existingBackups) ? new List<BackupItem>() : JsonSerializer.Deserialize<List<BackupItem>>(existingBackups) ?? new List<BackupItem>();

        backups.Add(new BackupItem { StaffId = req.StaffId, StaffName = req.StaffName });
        var updatedJson = JsonSerializer.Serialize(backups);

        using var updateCmd = new MySqlCommand("UPDATE Personnel SET Backups = @backups, UpdatedAt = @updatedAt WHERE Id = @id", _db);
        updateCmd.Parameters.AddWithValue("@backups", updatedJson);
        updateCmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
        updateCmd.Parameters.AddWithValue("@id", id);
        await updateCmd.ExecuteNonQueryAsync();

        return Ok(new { success = true, message = "备份人员已添加" });
    }

    [HttpDelete("{id}/backups/{staffId}")]
    public async Task<IActionResult> RemoveBackup(int id, int staffId)
    {
        using var checkCmd = new MySqlCommand("SELECT Backups FROM Personnel WHERE Id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        using var reader = await checkCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFound(new { success = false, message = "人员不存在" });
        reader.Close(); // 关闭 reader 才能执行下一个命令

        var existingBackups = reader["Backups"] as string;
        if (string.IsNullOrEmpty(existingBackups))
            return Ok(new { success = true, message = "没有备份人员" });

        var backups = JsonSerializer.Deserialize<List<BackupItem>>(existingBackups) ?? new List<BackupItem>();
        backups.RemoveAll(b => b.StaffId == staffId);
        var updatedJson = JsonSerializer.Serialize(backups);

        using var updateCmd = new MySqlCommand("UPDATE Personnel SET Backups = @backups, UpdatedAt = @updatedAt WHERE Id = @id", _db);
        updateCmd.Parameters.AddWithValue("@backups", updatedJson);
        updateCmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
        updateCmd.Parameters.AddWithValue("@id", id);
        await updateCmd.ExecuteNonQueryAsync();

        return Ok(new { success = true, message = "备份人员已移除" });
    }

    private static PersonnelItem MapPersonnel(MySqlDataReader r) => new()
    {
        Id = Convert.ToInt32(r["Id"]),
        EmployeeNo = r["EmployeeNo"].ToString() ?? "",
        Name = r["Name"].ToString() ?? "",
        Avatar = r["Avatar"] as string,
        Gender = r["Gender"].ToString() ?? "male",
        Birthday = r["Birthday"] == DBNull.Value ? null : Convert.ToDateTime(r["Birthday"]).ToString("yyyy-MM-dd"),
        IdCard = r["IdCard"] as string,
        Phone = r["Phone"].ToString() ?? "",
        Email = r["Email"] as string,
        Address = r["Address"] as string,
        Education = r["Education"].ToString() ?? "bachelor",
        GraduateSchool = r["GraduateSchool"] as string,
        Major = r["Major"] as string,
        Role = r["Role"].ToString() ?? "operator",
        DepartmentId = r["DepartmentId"] == DBNull.Value ? null : Convert.ToInt32(r["DepartmentId"]),
        DepartmentName = r["DepartmentName"] as string,
        Position = r["Position"] as string,
        EmploymentType = r["EmploymentType"].ToString() ?? "full_time",
        HireDate = r["HireDate"] == DBNull.Value ? null : Convert.ToDateTime(r["HireDate"]).ToString("yyyy-MM-dd"),
        ContractStart = r["ContractStart"] == DBNull.Value ? null : Convert.ToDateTime(r["ContractStart"]).ToString("yyyy-MM-dd"),
        ContractEnd = r["ContractEnd"] == DBNull.Value ? null : Convert.ToDateTime(r["ContractEnd"]).ToString("yyyy-MM-dd"),
        Salary = r["Salary"] == DBNull.Value ? null : Convert.ToDecimal(r["Salary"]),
        BankAccount = r["BankAccount"] as string,
        SocialSecurityNo = r["SocialSecurityNo"] as string,
        Status = r["Status"].ToString() ?? "probation",
        Specialties = r["Specialties"] as string,
        Backups = r["Backups"] as string,
        EmergencyContactName = r["EmergencyContactName"] as string,
        EmergencyContactRelationship = r["EmergencyContactRelationship"] as string,
        EmergencyContactPhone = r["EmergencyContactPhone"] as string,
        AttendanceCount = r["AttendanceCount"] == DBNull.Value ? 0 : Convert.ToInt32(r["AttendanceCount"]),
        OvertimeHours = r["OvertimeHours"] == DBNull.Value ? 0 : Convert.ToDecimal(r["OvertimeHours"]),
        LeaveDays = r["LeaveDays"] == DBNull.Value ? 0 : Convert.ToInt32(r["LeaveDays"]),
        PerformanceScore = r["PerformanceScore"] == DBNull.Value ? null : Convert.ToDecimal(r["PerformanceScore"]),
        TrainingCount = r["TrainingCount"] == DBNull.Value ? 0 : Convert.ToInt32(r["TrainingCount"]),
        Remark = r["Remark"] as string,
        CreatedAt = Convert.ToDateTime(r["CreatedAt"]),
        UpdatedAt = r["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(r["UpdatedAt"])
    };
}

public class PersonnelItem
{
    public int Id { get; set; }
    public string EmployeeNo { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Avatar { get; set; }
    public string Gender { get; set; } = "male";
    public string? Birthday { get; set; }
    public string? IdCard { get; set; }
    public string Phone { get; set; } = "";
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string Education { get; set; } = "bachelor";
    public string? GraduateSchool { get; set; }
    public string? Major { get; set; }
    public string Role { get; set; } = "operator";
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? Position { get; set; }
    public string EmploymentType { get; set; } = "full_time";
    public string? HireDate { get; set; }
    public string? ContractStart { get; set; }
    public string? ContractEnd { get; set; }
    public decimal? Salary { get; set; }
    public string? BankAccount { get; set; }
    public string? SocialSecurityNo { get; set; }
    public string Status { get; set; } = "probation";
    public string? Specialties { get; set; }
    public string? Backups { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public int AttendanceCount { get; set; }
    public decimal OvertimeHours { get; set; }
    public int LeaveDays { get; set; }
    public decimal? PerformanceScore { get; set; }
    public int TrainingCount { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class BackupRequest
{
    public int StaffId { get; set; }
    public string StaffName { get; set; } = "";
}

public class BackupItem
{
    public int StaffId { get; set; }
    public string StaffName { get; set; } = "";
}