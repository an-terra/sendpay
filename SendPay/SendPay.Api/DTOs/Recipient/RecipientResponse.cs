namespace SendPay.Api.DTOs.Recipient;

public record RecipientResponse(int Id, string Name, string Phone, string Note, DateTime CreatedAt);
