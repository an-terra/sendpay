namespace SendPay.Api.DTOs.Recipient;

public record RecipientRequest(string Name, string Phone, string Note = "");
