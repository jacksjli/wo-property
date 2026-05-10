using MySqlConnector;

namespace WO.Property.PersonService.Data;

public class DbInitializer
{
    public async Task InitializeAsync(MySqlConnection connection)
    {
        // The Personnel table already exists in wo_property database
        // Check if it needs seeding
        await using var checkCmd = new MySqlCommand(
            "SELECT COUNT(*) FROM Personnel", connection);
        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

        if (count == 0)
        {
            var seedSql = @"
            INSERT INTO Personnel (StaffId, Name, Gender, Phone, Email, Department, Role, Status, PersonType) VALUES
            ('EMP-001', '系统管理员', '男', '13800138000', 'admin@example.com', '行政部', 'company_head', '在职', '员工');
            INSERT INTO Personnel (StaffId, Name, Gender, Phone, Email, Department, Role, Status, PersonType) VALUES
            ('EMP-002', '李师傅', '男', '13800138001', 'li@example.com', '工程部', 'operator', '在职', '员工');
            INSERT INTO Personnel (StaffId, Name, Gender, Phone, Email, Department, Role, Status, PersonType) VALUES
            ('EMP-003', '王师傅', '男', '13800138002', 'wang@example.com', '工程部', 'operator', '在职', '员工');
            INSERT INTO Personnel (StaffId, Name, Gender, Phone, Email, Department, Role, Status, PersonType) VALUES
            ('EMP-004', '张客服', '女', '13800138003', 'zhang@example.com', '客服部', 'supervisor', '在职', '员工');
            INSERT INTO Personnel (StaffId, Name, Gender, Phone, Email, Department, Role, Status, PersonType) VALUES
            ('EMP-005', '刘保安', '男', '13800138004', 'liu@example.com', '安保部', 'operator', '在职', '员工');
            ";
            await using var cmd = new MySqlCommand(seedSql, connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}