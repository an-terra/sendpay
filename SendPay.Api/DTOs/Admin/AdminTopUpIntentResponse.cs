namespace SendPay.Api.DTOs.Admin;

public record AdminTopUpIntentResponse(
    int Id,
    int UserId,
    string UserName,
    string UserEmail,
    decimal ExpectedAmount,
    string ReferenceCode,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? MatchedAt,
    int? TransactionId,
    int? BankStatementLineId);
