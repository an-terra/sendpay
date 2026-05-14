using System.Text;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SendPay.Api.Data;
using SendPay.Api.Models;
using SendPay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ───────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Services ──────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<RatesService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IRecipientService, RecipientService>();
builder.Services.AddScoped<IUserService, UserService>();

// ── JWT Authentication ─────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer           = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidateAudience         = true,
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            ValidateLifetime         = true
        };
    });

builder.Services.AddAuthorization();

// ── Controllers ────────────────────────────────────────────
builder.Services.AddControllers();

// ── OpenAPI / Scalar ───────────────────────────────────────
builder.Services.AddOpenApi();

// ── CORS ──────────────────────────────────────────────────
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// ── Auto-run migrations & seed admin ──────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!db.Users.Any(u => u.IsAdmin))
    {
        db.Users.Add(new User
        {
            FullName     = "Admin",
            Email        = "admin@sendpay.com",
            Phone        = "0000000000",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            IsAdmin      = true,
            IsActive     = true
        });
        db.SaveChanges();
    }
}

// ── Middleware pipeline ────────────────────────────────────
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapOpenApi();
app.MapScalarApiReference(opt =>
{
    opt.Title = "SendPay API";
    opt.Theme = ScalarTheme.Purple;
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
