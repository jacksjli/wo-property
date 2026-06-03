// WeChat 请求模型（全局命名空间）
public class WeChatLoginRequest
{
    public string Code { get; set; } = string.Empty;
}

public class WeChatPhoneRequest
{
    public string Code { get; set; } = string.Empty;
    public string? EncryptedData { get; set; }
    public string? Iv { get; set; }
}

public class WeChatConfig
{
    public string AppId { get; set; } = "wx122aeb8c033c6a67";
    public string AppSecret { get; set; } = "";
}
