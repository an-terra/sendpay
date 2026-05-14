namespace SendPay.Api.Models;

public class User
{
    public int      Id           { get; set; }
    public string   FullName     { get; set; } = string.Empty;
    public string   Email        { get; set; } = string.Empty;
    public string   Phone        { get; set; } = string.Empty;
    public string   PasswordHash { get; set; } = string.Empty;
    public decimal  Balance      { get; set; } = 0;
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;
    public bool     IsActive     { get; set; } = true;
    public bool     IsAdmin      { get; set; } = false;

    /// <summary>Ngân hàng tại Nhật (do admin cấu hình) — hiển thị khi nạp tiền.</summary>
    public string? JapanBankName { get; set; }
    /// <summary>URL trang nạp/chuyển khoản của ngân hàng (https…).</summary>
    public string? JapanBankTopUpUrl { get; set; }

    public ICollection<Transaction> SentTransactions     { get; set; } = [];
    public ICollection<Transaction> ReceivedTransactions { get; set; } = [];
    public ICollection<Recipient>   Recipients           { get; set; } = [];
}
