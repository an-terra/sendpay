namespace SendPay.Api.DTOs.User;

/// <param name="MatchKind">saved | registered</param>
/// <param name="ResolvedPhone">Số dùng cho chuyển ví nội bộ (khi có).</param>
public record ReceiverLookupResponse(
    bool Found,
    string? FullName,
    bool IsSelf,
    int? SavedRecipientId,
    string? MatchKind,
    string? BankDisplay,
    string? ResolvedPhone);
