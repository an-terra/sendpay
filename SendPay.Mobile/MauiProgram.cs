using Microsoft.Extensions.Logging;
using SendPay.Mobile.Services;

namespace SendPay.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddTransient<Pages.LoginPage>();
        builder.Services.AddTransient<Pages.DashboardPage>();
        builder.Services.AddTransient<Pages.TopUpPage>();
        builder.Services.AddTransient<Pages.TransferPage>();
        builder.Services.AddTransient<Pages.RecipientsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
