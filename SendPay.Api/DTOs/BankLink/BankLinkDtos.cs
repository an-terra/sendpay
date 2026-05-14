namespace SendPay.Api.DTOs.BankLink;

public record JapanBankOption(
    string Code,
    string NameJa,
    string NameEn,
    string Emoji
);

public record BankLinkStartRequest(string BankCode, string? ReturnUrl);

public record BankLinkStartResponse(
    string State,
    string AuthorizeUrl,
    DateTime ExpiresAt
);

public record FakeBankApproveRequest(
    string State,
    string AccountNo,
    string LoginId
);

public record FakeBankApproveResponse(
    string RedirectUrl,
    int LinkId,
    string BankCode,
    string BankName,
    string AccountMasked
);

public record UserBankLinkDto(
    int      Id,
    string   BankCode,
    string   BankName,
    string   AccountMasked,
    bool     IsPrimary,
    bool     IsActive,
    DateTime LinkedAt
);
