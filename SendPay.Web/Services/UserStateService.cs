namespace SendPay.Web.Services;

public class UserStateService
{
    public string AvatarData { get; private set; } = "";
    public event Action? OnChanged;

    public void SetAvatar(string data)
    {
        AvatarData = data;
        OnChanged?.Invoke();
    }
}
