namespace SendPay.Api.DTOs.Auth;

public class AuthResponse
{
    public int    UserId   { get; set; }
    public string Token    { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
    public string Phone    { get; set; } = string.Empty;
    public bool   IsAdmin  { get; set; } = false;
}
