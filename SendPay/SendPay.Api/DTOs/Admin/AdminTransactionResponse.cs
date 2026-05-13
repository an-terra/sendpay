namespace SendPay.Api.DTOs.Admin;

public record AdminTransactionResponse(
    int      Id,
    string   SenderName,
    string   ReceiverName,
    decimal  Amount,
    string   Note,
    string   Type,
    string   Status,
    DateTime CreatedAt);
