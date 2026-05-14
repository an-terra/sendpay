namespace SendPay.Api.DTOs.Admin;

public record AdminDailyStatResponse(
    DateTime StatDate,
    string TransactionType,
    string Status,
    int Count,
    decimal TotalAmount,
    decimal TotalFee,
    DateTime ComputedAt);
