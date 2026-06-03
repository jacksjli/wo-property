namespace WO.Property.DispatchService;

public interface IDispatcher
{
    string Category { get; }
    Task<DispatchResult> DispatchAsync(DispatchRequest request);
}

public class DispatchResult
{
    public bool Success { get; set; }
    public int? AssignedPersonId { get; set; }
    public string? Message { get; set; }
    public string Status { get; set; } = "pending";
}

public class DispatchRequest
{
    public int TicketId { get; set; }
    public string Category { get; set; } = "property";
    public int? TicketTypeId { get; set; }
    public int? JobTypeId { get; set; }
    public int? AreaId { get; set; }
    public int? BuildingId { get; set; }
    public string Priority { get; set; } = "Normal";
}

public class DispatcherRegistry
{
    private static readonly Dictionary<string, IDispatcher> _dispatchers = new();

    public static void Register(IDispatcher dispatcher)
    {
        _dispatchers[dispatcher.Category] = dispatcher;
        Console.WriteLine($"[DispatcherRegistry] Registered: {dispatcher.Category}");
    }

    public static IDispatcher? GetDispatcher(string category)
    {
        _dispatchers.TryGetValue(category, out var dispatcher);
        return dispatcher;
    }

    public static List<string> GetRegisteredCategories()
    {
        return _dispatchers.Keys.ToList();
    }
}

// ============ 物业工单派单器 ============
public class PropertyDispatcher : IDispatcher
{
    public string Category => "property";

    public async Task<DispatchResult> DispatchAsync(DispatchRequest request)
    {
        Console.WriteLine($"[PropertyDispatcher] DispatchAsync TicketId={request.TicketId}, JobTypeId={request.JobTypeId}, AreaId={request.AreaId}, BuildingId={request.BuildingId}");

        try
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5018");
            client.Timeout = TimeSpan.FromSeconds(5);

            var personsResponse = await client.GetAsync("/api/persons");
            if (!personsResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"[PropertyDispatcher] PersonService returned {personsResponse.StatusCode}");
                return new DispatchResult { Success = false, Status = "manual", Message = "无法获取人员列表" };
            }

            var personsJson = await personsResponse.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(personsJson);
            var dataRoot = doc.RootElement.GetProperty("data");
            var dataElement = dataRoot.TryGetProperty("items", out var ipe) ? ipe : dataRoot;

            var candidates = new List<PersonCandidate>();

            foreach (var item in dataElement.EnumerateArray())
            {
                var specialtyIds = new List<int>();
                if ((item.TryGetProperty("specialty_ids", out var specialtyEl) || item.TryGetProperty("SpecialtyIds", out specialtyEl)) && specialtyEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var sid in specialtyEl.EnumerateArray())
                        if (sid.TryGetInt32(out var sidVal)) specialtyIds.Add(sidVal);
                }

                var role = item.TryGetProperty("Role", out var r) ? r.GetString() ?? "" : "";
                var status = item.TryGetProperty("Status", out var s) ? s.GetString() ?? "" : "";
                var name = item.TryGetProperty("Name", out var n) ? n.GetString() ?? "" : "";
                var id = item.GetProperty("Id").GetInt32();
                var areaIds = new List<int>();
                if ((item.TryGetProperty("area_ids", out var areaEl) || item.TryGetProperty("AreaIds", out areaEl)) && areaEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var aid in areaEl.EnumerateArray())
                        if (aid.TryGetInt32(out var aidVal)) areaIds.Add(aidVal);
                }

                // 读取楼栋IDs
                var buildingIds = new List<int>();
                if ((item.TryGetProperty("building_ids", out var bEl) || item.TryGetProperty("BuildingIds", out bEl)) && bEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var bid in bEl.EnumerateArray())
                        if (bid.TryGetInt32(out var bidVal)) buildingIds.Add(bidVal);
                }

                if (status != "active") continue;

                bool jobTypeMatched = specialtyIds.Contains(request.JobTypeId ?? 0);
                bool areaMatched = request.AreaId.HasValue && areaIds.Contains(request.AreaId.Value);
                bool buildingMatched = request.BuildingId.HasValue && buildingIds.Contains(request.BuildingId.Value);
                bool isSupervisor = role.ToLower() == "supervisor";

                candidates.Add(new PersonCandidate
                {
                    Id = id,
                    Name = name,
                    Role = role,
                    JobTypeMatched = jobTypeMatched,
                    AreaMatched = areaMatched,
                    BuildingMatched = buildingMatched,
                    IsSupervisor = isSupervisor,
                    PendingCount = 0
                });
            }

            // ========== 三级匹配规则 ==========
            // 第1级：工种+楼栋匹配
            var jobAndBuilding = candidates.Where(c => c.JobTypeMatched && c.BuildingMatched).ToList();
            
            // 第2级：工种+区域匹配（如果第1级无匹配）
            var jobAndArea = candidates.Where(c => c.JobTypeMatched && c.AreaMatched).ToList();
            
            // 第3级：主管（如果第2级也无匹配）
            var supervisors = candidates.Where(c => c.IsSupervisor).ToList();

            // 选择候选集：优先第1级 > 第2级 > 第3级
            List<PersonCandidate> selectedCandidates;
            string matchLevel;
            if (jobAndBuilding.Any())
            {
                selectedCandidates = jobAndBuilding;
                matchLevel = "工种+楼栋";
            }
            else if (jobAndArea.Any())
            {
                selectedCandidates = jobAndArea;
                matchLevel = "工种+区域";
            }
            else if (supervisors.Any())
            {
                selectedCandidates = supervisors;
                matchLevel = "主管";
            }
            else
            {
                Console.WriteLine($"[PropertyDispatcher] No candidates found for TicketId={request.TicketId}");
                return new DispatchResult { Success = false, Status = "manual", Message = "没有找到可派单的人员，请手动指派" };
            }

            Console.WriteLine($"[PropertyDispatcher] Match level: {matchLevel}, candidates: {selectedCandidates.Count}");

            // 查询每个人员的待处理工单数
            try
            {
                using var dispatchClient = new HttpClient();
                dispatchClient.BaseAddress = new Uri("http://localhost:5241");
                dispatchClient.Timeout = TimeSpan.FromSeconds(5);

                foreach (var c in selectedCandidates)
                {
                    try
                    {
                        var workloadResponse = await dispatchClient.GetAsync($"/api/tenant/dispatch/workload/{c.Id}");
                        if (workloadResponse.IsSuccessStatusCode)
                        {
                            var workloadJson = await workloadResponse.Content.ReadAsStringAsync();
                            using var wDoc = System.Text.Json.JsonDocument.Parse(workloadJson);
                            if (wDoc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.TryGetProperty("pendingCount", out var pcEl))
                            {
                                c.PendingCount = pcEl.GetInt32();
                            }
                        }
                    }
                    catch { /* ignore individual failures */ }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PropertyDispatcher] Failed to get workload: {ex.Message}");
            }

            // 按负载排序（待处理工单数最少优先）
            var sorted = selectedCandidates.OrderBy(c => c.PendingCount).ToList();
            var selected = sorted.First();

            Console.WriteLine($"[PropertyDispatcher] Selected PersonId={selected.Id}, Name={selected.Name}, Role={selected.Role}, Match={matchLevel}, Pending={selected.PendingCount}");

            return new DispatchResult
            {
                Success = true,
                AssignedPersonId = selected.Id,
                Status = "assigned",
                Message = $"工单已派给 {selected.Name}（{selected.Role}）[{matchLevel}]"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PropertyDispatcher] Exception: {ex.Message} | Inner: {ex.InnerException?.Message} | Stack: {ex.StackTrace}");
            return new DispatchResult { Success = false, Status = "manual", Message = $"派单异常: {ex.Message}" };
        }
    }
}

public class PersonCandidate
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public bool JobTypeMatched { get; set; }
    public bool AreaMatched { get; set; }
    public bool BuildingMatched { get; set; }
    public bool IsSupervisor { get; set; }
    public int PendingCount { get; set; }
}

// ============ HR 工单派单器 ============
public class HrDispatcher : IDispatcher
{
    public string Category => "hr";

    public async Task<DispatchResult> DispatchAsync(DispatchRequest request)
    {
        Console.WriteLine($"[HrDispatcher] DispatchAsync TicketId={request.TicketId}, JobTypeId={request.JobTypeId}, AreaId={request.AreaId}");

        try
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5018");
            client.Timeout = TimeSpan.FromSeconds(5);

            var personsResponse = await client.GetAsync("/api/persons");
            if (!personsResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"[HrDispatcher] PersonService returned {personsResponse.StatusCode}");
                return new DispatchResult { Success = false, Status = "manual", Message = "无法获取人员列表" };
            }

            var personsJson = await personsResponse.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(personsJson);
            var dataRoot = doc.RootElement.GetProperty("data");
            var dataElement = dataRoot.TryGetProperty("items", out var ipe) ? ipe : dataRoot;

            var candidates = new List<HrCandidate>();

            foreach (var item in dataElement.EnumerateArray())
            {
                var role = item.TryGetProperty("Role", out var r) ? r.GetString() ?? "" : "";
                var deptName = item.TryGetProperty("DepartmentName", out var dn) ? dn.GetString() ?? "" : "";
                var status = item.TryGetProperty("Status", out var s) ? s.GetString() ?? "" : "";
                var id = item.GetProperty("Id").GetInt32();
                var name = item.TryGetProperty("Name", out var n) ? n.GetString() ?? "" : "";

                if (status != "active") continue;

                bool isHrRelated = role.ToLower().Contains("hr") ||
                                  role.ToLower().Contains("human") ||
                                  deptName.ToLower().Contains("hr") ||
                                  deptName.ToLower().Contains("human") ||
                                  role.ToLower() == "manager" ||
                                  role.ToLower() == "supervisor";

                if (!isHrRelated) continue;

                int priority = role.ToLower() switch
                {
                    var lr when lr.Contains("hr") && lr.Contains("manager") => 1,
                    var lr when lr.Contains("hr") && lr.Contains("supervisor") => 2,
                    var lr when lr.Contains("hr") => 3,
                    "manager" => 4,
                    "supervisor" => 5,
                    _ => 6
                };

                candidates.Add(new HrCandidate
                {
                    Id = id,
                    Name = name,
                    Role = role,
                    DepartmentName = deptName,
                    Priority = priority
                });
            }

            var sorted = candidates.OrderBy(c => c.Priority).ToList();

            if (sorted.Count == 0)
            {
                Console.WriteLine($"[HrDispatcher] No HR staff found for TicketId={request.TicketId}");
                return new DispatchResult { Success = false, Status = "manual", Message = "没有找到 HR 人员" };
            }

            var selected = sorted.First();
            Console.WriteLine($"[HrDispatcher] Selected PersonId={selected.Id}, Role={selected.Role}");

            return new DispatchResult
            {
                Success = true,
                AssignedPersonId = selected.Id,
                Status = "assigned",
                Message = $"HR 工单已派给 {selected.Name}（{selected.Role}）"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HrDispatcher] Exception: {ex.Message} | Inner: {ex.InnerException?.Message} | Stack: {ex.StackTrace}");
            return new DispatchResult { Success = false, Status = "manual", Message = $"HR 派单异常: {ex.Message}" };
        }
    }
}

public class HrCandidate
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public int Priority { get; set; }
}

// ============ 财务工单派单器 ============
public class FinanceDispatcher : IDispatcher
{
    public string Category => "finance";

    public async Task<DispatchResult> DispatchAsync(DispatchRequest request)
    {
        Console.WriteLine($"[FinanceDispatcher] DispatchAsync TicketId={request.TicketId}, JobTypeId={request.JobTypeId}");

        try
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5018");
            client.Timeout = TimeSpan.FromSeconds(5);

            var personsResponse = await client.GetAsync("/api/persons");
            if (!personsResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"[FinanceDispatcher] PersonService returned {personsResponse.StatusCode}");
                return new DispatchResult { Success = false, Status = "manual", Message = "无法获取人员列表" };
            }

            var personsJson = await personsResponse.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(personsJson);
            var dataRoot = doc.RootElement.GetProperty("data");
            var dataElement = dataRoot.TryGetProperty("items", out var ipe) ? ipe : dataRoot;

            var amountThreshold = request.Priority?.ToLower() switch
            {
                "urgent" => 1000,
                "high" => 10000,
                "medium" => 50000,
                _ => 1000
            };

            var candidates = new List<FinanceCandidate>();

            foreach (var item in dataElement.EnumerateArray())
            {
                var role = item.TryGetProperty("Role", out var r) ? r.GetString() ?? "" : "";
                var deptName = item.TryGetProperty("DepartmentName", out var dn) ? dn.GetString() ?? "" : "";
                var status = item.TryGetProperty("Status", out var s) ? s.GetString() ?? "" : "";
                var id = item.GetProperty("Id").GetInt32();
                var name = item.TryGetProperty("Name", out var n) ? n.GetString() ?? "" : "";

                if (status != "active") continue;

                bool isFinance = role.ToLower().Contains("finance") ||
                                role.ToLower().Contains("财务") ||
                                deptName.ToLower().Contains("财务") ||
                                role.ToLower() == "manager" ||
                                role.ToLower() == "supervisor";

                if (!isFinance) continue;

                int priority = role.ToLower() switch
                {
                    var fr when fr.Contains("finance") && fr.Contains("manager") => 1,
                    var fr when fr.Contains("finance") && fr.Contains("supervisor") => 2,
                    var fr when fr.Contains("finance") => 3,
                    "manager" => 4,
                    "supervisor" => 5,
                    _ => 6
                };

                if (amountThreshold > 10000 && priority > 2)
                    continue;

                candidates.Add(new FinanceCandidate
                {
                    Id = id,
                    Name = name,
                    Role = role,
                    DepartmentName = deptName,
                    Priority = priority
                });
            }

            var sorted = candidates.OrderBy(c => c.Priority).ToList();

            if (sorted.Count == 0)
            {
                Console.WriteLine($"[FinanceDispatcher] No finance staff found for TicketId={request.TicketId}, threshold={amountThreshold}");
                return new DispatchResult { Success = false, Status = "manual", Message = "没有找到财务人员" };
            }

            var selected = sorted.First();
            Console.WriteLine($"[FinanceDispatcher] Selected PersonId={selected.Id}, Role={selected.Role}, AmountThreshold={amountThreshold}");

            return new DispatchResult
            {
                Success = true,
                AssignedPersonId = selected.Id,
                Status = "assigned",
                Message = $"财务工单已派给 {selected.Name}（{selected.Role}），审批额度 {amountThreshold}"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FinanceDispatcher] Exception: {ex.Message} | Inner: {ex.InnerException?.Message} | Stack: {ex.StackTrace}");
            return new DispatchResult { Success = false, Status = "manual", Message = $"财务派单异常: {ex.Message}" };
        }
    }
}

public class FinanceCandidate
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public int Priority { get; set; }
}

// ============ 人工指派 ============
public class ManualAssignRequest
{
    public int TicketId { get; set; }
    public int PersonId { get; set; }
    public string? Reason { get; set; }
}