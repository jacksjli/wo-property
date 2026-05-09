namespace WO.Property.MasterDataService;

public class FinanceSeedRecord
{
    public string RecordNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal Amount { get; set; }
    public decimal Balance { get; set; }
    public string? PaymentMethod { get; set; }
    public string? RecordDate { get; set; }
    public string? Handler { get; set; }
    public string? RelatedParty { get; set; }
    public string? ContractNo { get; set; }
    public string? BillNo { get; set; }
    public string? Description { get; set; }
    public string? ReceiptNo { get; set; }
    public string Status { get; set; } = "completed";
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
}

public static class SeedDataHelper
{
    public static List<FinanceSeedRecord> GetFinanceSeedData()
    {
        var list = new List<FinanceSeedRecord>();
        var now = DateTime.UtcNow;
        
        // 收入记录
        list.Add(new FinanceSeedRecord { RecordNumber = "TR-2026-0001", Type = "income", Category = "property_fee", Amount = 156800.00m, Balance = 156800.00m, PaymentMethod = "transfer", RecordDate = "2026-04-01", Handler = "王芳", RelatedParty = "翡翠湾小区业主委员会", Description = "2026年第一季度物业费收取", ReceiptNo = "RCPT20260401", Status = "completed", CreatedAt = now });
        list.Add(new FinanceSeedRecord { RecordNumber = "TR-2026-0002", Type = "income", Category = "property_fee", Amount = 89200.00m, Balance = 89200.00m, PaymentMethod = "wechat", RecordDate = "2026-04-02", Handler = "王芳", RelatedParty = "业主微信支付", Description = "物业费微信收款", ReceiptNo = "RCPT20260402", Status = "completed", CreatedAt = now });
        list.Add(new FinanceSeedRecord { RecordNumber = "TR-2026-0003", Type = "income", Category = "parking", Amount = 32000.00m, Balance = 32000.00m, PaymentMethod = "transfer", RecordDate = "2026-04-03", Handler = "李强", RelatedParty = "停车场月卡用户", Description = "停车场月卡收入", ReceiptNo = "RCPT20260403", Status = "completed", CreatedAt = now });
        list.Add(new FinanceSeedRecord { RecordNumber = "TR-2026-0004", Type = "income", Category = "advertising", Amount = 15000.00m, Balance = 15000.00m, PaymentMethod = "transfer", RecordDate = "2026-04-05", Handler = "王芳", RelatedParty = "楼道广告合作商", Description = "楼道广告位租赁费", ReceiptNo = "RCPT20260405", Status = "completed", CreatedAt = now });
        list.Add(new FinanceSeedRecord { RecordNumber = "TR-2026-0005", Type = "income", Category = "facility", Amount = 8500.00m, Balance = 8500.00m, PaymentMethod = "transfer", RecordDate = "2026-04-08", Handler = "李强", RelatedParty = "健身会所", Description = "公共设施场地租赁费", ReceiptNo = "RCPT20260408", Status = "completed", CreatedAt = now });
        list.Add(new FinanceSeedRecord { RecordNumber = "TR-2026-0006", Type = "income", Category = "other", Amount = 5600.00m, Balance = 5600.00m, PaymentMethod = "cash", RecordDate = "2026-04-10", Handler = "王芳", RelatedParty = "快递柜运营方", Description = "快递柜场地使用费", Status = "completed", CreatedAt = now });
        
        // 支出记录
        list.Add(new FinanceSeedRecord { RecordNumber = "TR-2026-0007", Type = "expense", Category = "maintenance", Amount = 45600.00m, Balance = 0m, PaymentMethod = "transfer", RecordDate = "2026-04-02", Handler = "李强", RelatedParty = "电梯维保公司", Description = "电梯季度维保费用", BillNo = "INV20260402", Status = "completed", CreatedAt = now });
        list.Add(new FinanceSeedRecord { RecordNumber = "TR-2026-0008", Type = "expense", Category = "cleaning", Amount = 18000.00m, Balance = 0m, PaymentMethod = "transfer", RecordDate = "2026-04-03", Handler = "李强", RelatedParty = "保洁公司", Description = "4月份保洁服务费", BillNo = "INV20260403", Status = "completed", CreatedAt = now });
        list.Add(new FinanceSeedRecord { RecordNumber = "TR-2026-0009", Type = "expense", Category = "utility", Amount = 28300.00m, Balance = 0m, PaymentMethod = "transfer", RecordDate = "2026-04-05", Handler = "王芳", RelatedParty = "电力公司", Description = "公共区域电费", BillNo = "INV20260405", Status = "completed", CreatedAt = now });
        list.Add(new FinanceSeedRecord { RecordNumber = "TR-2026-0010", Type = "expense", Category = "utility", Amount = 12600.00m, Balance = 0m, PaymentMethod = "transfer", RecordDate = "2026-04-06", Handler = "王芳", RelatedParty = "水务集团", Description = "公共区域水费", BillNo = "INV20260406", Status = "completed", CreatedAt = now });
        list.Add(new FinanceSeedRecord { RecordNumber = "TR-2026-0011", Type = "expense", Category = "maintenance", Amount = 8900.00m, Balance = 0m, PaymentMethod = "transfer", RecordDate = "2026-04-08", Handler = "李强", RelatedParty = "园林绿化公司", Description = "绿化维护费", BillNo = "INV20260408", Status = "completed", CreatedAt = now });
        list.Add(new FinanceSeedRecord { RecordNumber = "TR-2026-0012", Type = "expense", Category = "other", Amount = 4200.00m, Balance = 0m, PaymentMethod = "cash", RecordDate = "2026-04-10", Handler = "王芳", RelatedParty = "办公用品供应商", Description = "办公用品采购", BillNo = "INV20260410", Status = "completed", CreatedAt = now });
        
        return list;
    }
}
