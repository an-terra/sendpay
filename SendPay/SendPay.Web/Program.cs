using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SendPay.Web;
using SendPay.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBase = builder.Configuration["ApiBaseUrl"];
var baseUri = string.IsNullOrWhiteSpace(apiBase)
    ? new Uri(builder.HostEnvironment.BaseAddress)
    : new Uri(apiBase);

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = baseUri });
builder.Services.AddScoped<ApiService>();
builder.Services.AddSingleton<LanguageService>();
builder.Services.AddBlazoredLocalStorage();

await builder.Build().RunAsync();
