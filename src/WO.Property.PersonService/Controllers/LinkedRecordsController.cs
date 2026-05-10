using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace WO.Property.PersonService.Controllers;

/// <summary>
/// Cross-module association query (based on equivalent field mapping)
/// Query all module records by standard field keys like phone_number / person_name
/// </summary>
[ApiController]
[Route("api/linked")]
public class LinkedRecordsController : ControllerBase
{
    private readonly string _connectionString;

    public LinkedRecordsController(IConfiguration config)
    {
        _connectionString = "Server=localhost;Port=3306;Database=wo_property;User=woproperty;Password=WOProperty2026!;CharSet=utf8mb4;Pooling=true;Minimum Pool Size=2;Maximum Pool Size=20;Connection Timeout=10;";
    }

    private const string PhoneRequired = "phone cannot be empty";
    private const string NameRequired = "name cannot be empty";

    /// <summary>
    /// Query by phone number (phone_number equivalence group)
    /// Supported modules: Personnel, Ticket, Visitor, Delivery, Renovation
    /// </summary>
    [HttpGet("by-phone/{phone}")]
    public async Task<IActionResult> GetByPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest(new { success = false, message = PhoneRequired });

        phone = phone.Trim();
        var results = new List<LinkedRecordGroup>();

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        // 1. Personnel (Phone / EmergencyContactPhone)
        await using var personCmd = new MySqlCommand(
            @"SELECT Id, EmployeeNo, Name, Phone, EmergencyContactPhone, DepartmentName, Role
              FROM Personnel
              WHERE Phone = @phone OR EmergencyContactPhone = @phone
              LIMIT 1", conn);
        personCmd.Parameters.AddWithValue("@phone", phone);
        await using var personReader = await personCmd.ExecuteReaderAsync();
        if (await personReader.ReadAsync())
        {
            var personRecords = new List<LinkedRecord>
            {
                new() { Id = personReader.GetInt32("Id"), FieldKey = "name", Value = personReader.GetString("Name"), Label = "Name" },
                new() { Id = personReader.GetInt32("Id"), FieldKey = "employeeNo", Value = personReader.GetString("EmployeeNo"), Label = "EmployeeNo" },
                new() { Id = personReader.GetInt32("Id"), FieldKey = "phone", Value = personReader.GetString("Phone"), Label = "Phone" }
            };
            if (!personReader.IsDBNull(personReader.GetOrdinal("EmergencyContactPhone")))
                personRecords.Add(new() { Id = personReader.GetInt32("Id"), FieldKey = "emergencyPhone", Value = personReader.GetString("EmergencyContactPhone"), Label = "EmergencyPhone" });
            if (!personReader.IsDBNull(personReader.GetOrdinal("DepartmentName")))
                personRecords.Add(new() { Id = personReader.GetInt32("Id"), FieldKey = "department", Value = personReader.GetString("DepartmentName"), Label = "Department" });

            results.Add(new LinkedRecordGroup { Module = "personnel", ModuleName = "Personnel", Records = personRecords });
        }
        await personReader.CloseAsync();

        // 2a. Tickets (contactPhone / reporterPhone)
        await using var ticketCmd = new MySqlCommand(
            @"SELECT Id, TicketNumber, Title, Status, CreatedAt
              FROM Tickets
              WHERE ContactPhone = @phone OR ReporterPhone = @phone
              ORDER BY CreatedAt DESC LIMIT 5", conn);
        ticketCmd.Parameters.AddWithValue("@phone", phone);
        await using var ticketReader = await ticketCmd.ExecuteReaderAsync();
        var ticketRecords = new List<LinkedRecord>();
        while (await ticketReader.ReadAsync())
        {
            ticketRecords.Add(new LinkedRecord
            {
                Id = ticketReader.GetInt32("Id"),
                FieldKey = "ticketCode",
                Value = ticketReader.GetString("TicketNumber"),
                Label = "TicketNumber"
            });
        }
        await ticketReader.CloseAsync();
        if (ticketRecords.Count > 0)
            results.Add(new LinkedRecordGroup { Module = "ticket", ModuleName = "Ticket", Records = ticketRecords });

        // 2b. Visitors (visitorPhone / hostPhone)
        await using var visitorCmd = new MySqlCommand(
            @"SELECT Id, VisitorName, VisitorPhone, HostName
              FROM Visitors
              WHERE VisitorPhone = @phone OR HostPhone = @phone
              ORDER BY CreatedAt DESC LIMIT 5", conn);
        visitorCmd.Parameters.AddWithValue("@phone", phone);
        await using var visitorReader = await visitorCmd.ExecuteReaderAsync();
        var visitorRecords = new List<LinkedRecord>();
        while (await visitorReader.ReadAsync())
        {
            visitorRecords.Add(new LinkedRecord
            {
                Id = visitorReader.GetInt32("Id"),
                FieldKey = "visitorName",
                Value = visitorReader.IsDBNull(visitorReader.GetOrdinal("VisitorName")) ? "" : visitorReader.GetString("VisitorName"),
                Label = "VisitorName"
            });
        }
        await visitorReader.CloseAsync();
        if (visitorRecords.Count > 0)
            results.Add(new LinkedRecordGroup { Module = "visitor", ModuleName = "Visitor", Records = visitorRecords });

        // 2c. DeliveryRequests (residentPhone)
        await using var deliveryCmd = new MySqlCommand(
            @"SELECT Id, ResidentName, DeliveryCompany
              FROM DeliveryRequests
              WHERE ResidentPhone = @phone
              ORDER BY CreatedAt DESC LIMIT 5", conn);
        deliveryCmd.Parameters.AddWithValue("@phone", phone);
        await using var deliveryReader = await deliveryCmd.ExecuteReaderAsync();
        var deliveryRecords = new List<LinkedRecord>();
        while (await deliveryReader.ReadAsync())
        {
            deliveryRecords.Add(new LinkedRecord
            {
                Id = deliveryReader.GetInt32("Id"),
                FieldKey = "residentName",
                Value = deliveryReader.IsDBNull(deliveryReader.GetOrdinal("ResidentName")) ? "" : deliveryReader.GetString("ResidentName"),
                Label = "ResidentName"
            });
        }
        await deliveryReader.CloseAsync();
        if (deliveryRecords.Count > 0)
            results.Add(new LinkedRecordGroup { Module = "delivery", ModuleName = "Delivery", Records = deliveryRecords });

        // 2d. RenovationRequests (applicantPhone)
        await using var renoCmd = new MySqlCommand(
            @"SELECT Id, ApplicantName, Status
              FROM RenovationRequests
              WHERE ApplicantPhone = @phone
              ORDER BY CreatedAt DESC LIMIT 5", conn);
        renoCmd.Parameters.AddWithValue("@phone", phone);
        await using var renoReader = await renoCmd.ExecuteReaderAsync();
        var renoRecords = new List<LinkedRecord>();
        while (await renoReader.ReadAsync())
        {
            renoRecords.Add(new LinkedRecord
            {
                Id = renoReader.GetInt32("Id"),
                FieldKey = "applicantName",
                Value = renoReader.IsDBNull(renoReader.GetOrdinal("ApplicantName")) ? "" : renoReader.GetString("ApplicantName"),
                Label = "ApplicantName"
            });
        }
        await renoReader.CloseAsync();
        if (renoRecords.Count > 0)
            results.Add(new LinkedRecordGroup { Module = "renovation", ModuleName = "Renovation", Records = renoRecords });

        return Ok(new
        {
            success = true,
            data = new { phone, totalModules = results.Count, groups = results }
        });
    }

    /// <summary>
    /// Query by name (person_name equivalence group)
    /// Supported modules: Personnel, Ticket, Visitor, Delivery, Renovation, Complaints, Resident
    /// </summary>
    [HttpGet("by-name/{name}")]
    public async Task<IActionResult> GetByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { success = false, message = NameRequired });

        name = name.Trim();
        var results = new List<LinkedRecordGroup>();

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        // 1. Personnel (Name)
        await using var personCmd = new MySqlCommand(
            @"SELECT Id, EmployeeNo, Name, Phone, DepartmentName
              FROM Personnel
              WHERE Name LIKE @name
              LIMIT 10", conn);
        personCmd.Parameters.AddWithValue("@name", $"%{name}%");
        await using var personReader = await personCmd.ExecuteReaderAsync();
        var personRecords = new List<LinkedRecord>();
        while (await personReader.ReadAsync())
        {
            personRecords.Add(new LinkedRecord
            {
                Id = personReader.GetInt32("Id"),
                FieldKey = "name",
                Value = personReader.GetString("Name"),
                Label = "Name"
            });
        }
        await personReader.CloseAsync();
        if (personRecords.Count > 0)
            results.Add(new LinkedRecordGroup { Module = "personnel", ModuleName = "Personnel", Records = personRecords });

        // 2. Tickets (contactPersonName / assigneeName / reporterName)
        await using var ticketCmd = new MySqlCommand(
            @"SELECT Id, TicketNumber, Title, Status, ContactPersonName, AssignedTo, ReporterName, CreatedAt
              FROM Tickets
              WHERE ContactPersonName LIKE @name OR AssignedTo LIKE @name OR ReporterName LIKE @name
              ORDER BY CreatedAt DESC LIMIT 5", conn);
        ticketCmd.Parameters.AddWithValue("@name", $"%{name}%");
        await using var ticketReader = await ticketCmd.ExecuteReaderAsync();
        var ticketRecords = new List<LinkedRecord>();
        while (await ticketReader.ReadAsync())
        {
            ticketRecords.Add(new LinkedRecord
            {
                Id = ticketReader.GetInt32("Id"),
                FieldKey = "ticketCode",
                Value = ticketReader.GetString("TicketNumber"),
                Label = "Ticket"
            });
        }
        await ticketReader.CloseAsync();
        if (ticketRecords.Count > 0)
            results.Add(new LinkedRecordGroup { Module = "ticket", ModuleName = "Ticket", Records = ticketRecords });

        // 3. Visitors (visitorName / hostName)
        await using var visitorCmd = new MySqlCommand(
            @"SELECT Id, VisitorName, VisitorPhone, HostName
              FROM Visitors
              WHERE VisitorName LIKE @name OR HostName LIKE @name
              ORDER BY CreatedAt DESC LIMIT 5", conn);
        visitorCmd.Parameters.AddWithValue("@name", $"%{name}%");
        await using var visitorReader = await visitorCmd.ExecuteReaderAsync();
        var visitorRecords = new List<LinkedRecord>();
        while (await visitorReader.ReadAsync())
        {
            visitorRecords.Add(new LinkedRecord
            {
                Id = visitorReader.GetInt32("Id"),
                FieldKey = "visitorName",
                Value = visitorReader.IsDBNull(visitorReader.GetOrdinal("VisitorName")) ? "" : visitorReader.GetString("VisitorName"),
                Label = "VisitorName"
            });
        }
        await visitorReader.CloseAsync();
        if (visitorRecords.Count > 0)
            results.Add(new LinkedRecordGroup { Module = "visitor", ModuleName = "Visitor", Records = visitorRecords });

        // 4. DeliveryRequests (residentName)
        await using var deliveryCmd = new MySqlCommand(
            @"SELECT Id, ResidentName, DeliveryCompany
              FROM DeliveryRequests
              WHERE ResidentName LIKE @name
              ORDER BY CreatedAt DESC LIMIT 5", conn);
        deliveryCmd.Parameters.AddWithValue("@name", $"%{name}%");
        await using var deliveryReader = await deliveryCmd.ExecuteReaderAsync();
        var deliveryRecords = new List<LinkedRecord>();
        while (await deliveryReader.ReadAsync())
        {
            deliveryRecords.Add(new LinkedRecord
            {
                Id = deliveryReader.GetInt32("Id"),
                FieldKey = "residentName",
                Value = deliveryReader.IsDBNull(deliveryReader.GetOrdinal("ResidentName")) ? "" : deliveryReader.GetString("ResidentName"),
                Label = "ResidentName"
            });
        }
        await deliveryReader.CloseAsync();
        if (deliveryRecords.Count > 0)
            results.Add(new LinkedRecordGroup { Module = "delivery", ModuleName = "Delivery", Records = deliveryRecords });

        // 5. RenovationRequests (applicantName)
        await using var renoCmd = new MySqlCommand(
            @"SELECT Id, ApplicantName, Status
              FROM RenovationRequests
              WHERE ApplicantName LIKE @name
              ORDER BY CreatedAt DESC LIMIT 5", conn);
        renoCmd.Parameters.AddWithValue("@name", $"%{name}%");
        await using var renoReader = await renoCmd.ExecuteReaderAsync();
        var renoRecords = new List<LinkedRecord>();
        while (await renoReader.ReadAsync())
        {
            renoRecords.Add(new LinkedRecord
            {
                Id = renoReader.GetInt32("Id"),
                FieldKey = "applicantName",
                Value = renoReader.IsDBNull(renoReader.GetOrdinal("ApplicantName")) ? "" : renoReader.GetString("ApplicantName"),
                Label = "ApplicantName"
            });
        }
        await renoReader.CloseAsync();
        if (renoRecords.Count > 0)
            results.Add(new LinkedRecordGroup { Module = "renovation", ModuleName = "Renovation", Records = renoRecords });

        // 6. Complaints (complainantName)
        await using var compCmd = new MySqlCommand(
            @"SELECT Id, ComplaintNo, ComplainantName, HandleStatus, CreatedAt
              FROM Complaints
              WHERE ComplainantName LIKE @name
              ORDER BY CreatedAt DESC LIMIT 5", conn);
        compCmd.Parameters.AddWithValue("@name", $"%{name}%");
        await using var compReader = await compCmd.ExecuteReaderAsync();
        var compRecords = new List<LinkedRecord>();
        while (await compReader.ReadAsync())
        {
            compRecords.Add(new LinkedRecord
            {
                Id = compReader.GetInt32("Id"),
                FieldKey = "complaintCode",
                Value = compReader.GetString("ComplaintCode"),
                Label = "ComplainantName"
            });
        }
        await compReader.CloseAsync();
        if (compRecords.Count > 0)
            results.Add(new LinkedRecordGroup { Module = "complaints", ModuleName = "Complaint", Records = compRecords });

        // 7. Residents (name)
        await using var resCmd = new MySqlCommand(
            @"SELECT Id, Name, Phone
              FROM Residents
              WHERE Name LIKE @name
              ORDER BY CreatedAt DESC LIMIT 5", conn);
        resCmd.Parameters.AddWithValue("@name", $"%{name}%");
        await using var resReader = await resCmd.ExecuteReaderAsync();
        var resRecords = new List<LinkedRecord>();
        while (await resReader.ReadAsync())
        {
            resRecords.Add(new LinkedRecord
            {
                Id = resReader.GetInt32("Id"),
                FieldKey = "residentName",
                Value = resReader.IsDBNull(resReader.GetOrdinal("Name")) ? "" : resReader.GetString("Name"),
                Label = "Name"
            });
        }
        await resReader.CloseAsync();
        if (resRecords.Count > 0)
            results.Add(new LinkedRecordGroup { Module = "resident", ModuleName = "Resident", Records = resRecords });

        return Ok(new
        {
            success = true,
            data = new { name, totalModules = results.Count, groups = results }
        });
    }
}

public class LinkedRecordGroup
{
    public string Module { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public List<LinkedRecord> Records { get; set; } = new();
}

public class LinkedRecord
{
    public int Id { get; set; }
    public string FieldKey { get; set; } = "";
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
}
