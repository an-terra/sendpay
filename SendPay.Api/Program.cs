using System.Text;
using System.Threading.RateLimiting;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Scalar.AspNetCore;
using SendPay.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Database ───────────────────────────────────────────────
var rawConn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
var isSqlite = rawConn.StartsWith("Data Source", StringComparison.OrdinalIgnoreCase)
            || rawConn.EndsWith(".db", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(rawConn);

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (isSqlite)
        opt.UseSqlite(string.IsNullOrWhiteSpace(rawConn) ? "Data Source=sendpay.db" : rawConn);
    else
        opt.UseNpgsql(NormalizePostgresConnectionString(rawConn));
});

// ── Services ──────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<RatesService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IRecipientService, RecipientService>();
builder.Services.AddScoped<IUserService, UserService>();

// ── JWT Authentication ─────────────────────────────────────
var jwtKey = JwtKeyResolver.ResolveSigningKey(builder.Configuration, builder.Environment);

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

// ── Rate limiting (auth endpoints) ─────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await ctx.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Quá nhiều yêu cầu đăng nhập hoặc đăng ký. Vui lòng thử lại sau ít phút." },
            ct);
    };
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.GetClientIpAddress(),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 25,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("bank-link", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.GetClientIpAddress(),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

builder.Services.AddScoped<IReconciliationService, ReconciliationService>();
builder.Services.AddScoped<IBankLinkService, BankLinkService>();
builder.Services.AddHostedService<SendPay.Api.Background.ReconciliationBackgroundService>();

// ── Controllers ────────────────────────────────────────────
builder.Services.AddControllers();

// ── OpenAPI / Scalar ───────────────────────────────────────
builder.Services.AddOpenApi();

// ── CORS ──────────────────────────────────────────────────
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p =>
    {
        var origins = builder.Configuration["Cors:AllowedOrigins"]?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (origins is { Length: > 0 })
        {
            p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        }
        else if (builder.Environment.IsDevelopment())
        {
            // Dev: cho phép mọi origin để tiện chạy WASM dev server.
            p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            // Prod mặc định: chặn cross-origin. WASM được host cùng origin
            // qua app.UseBlazorFrameworkFiles() nên không cần CORS.
            p.WithOrigins().AllowAnyHeader().AllowAnyMethod();
        }
    }));

var app = builder.Build();

app.MapGet("/health", () => Results.Text("ok", "text/plain"));

// ── Auto-create schema & seed admin ───────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    TryRecipientBankColumns(db);
    TryTransactionTransferColumns(db);
    TryUserJapanBankColumns(db);
    SendPay.Api.Infrastructure.ReconciliationSchema.EnsureTables(db);
    SendPay.Api.Infrastructure.BankLinkSchema.EnsureTables(db);

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opt =>
    {
        opt.Title = "SendPay API";
        opt.Theme = ScalarTheme.Purple;
    });
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();


static void TryRecipientBankColumns(AppDbContext db)
{
    try
    {
        db.Database.ExecuteSqlRaw("""ALTER TABLE "Recipients" ADD COLUMN IF NOT EXISTS "BankName" text NULL;""");
        db.Database.ExecuteSqlRaw("""ALTER TABLE "Recipients" ADD COLUMN IF NOT EXISTS "AccountNumber" text NULL;""");
        db.Database.ExecuteSqlRaw("""ALTER TABLE "Recipients" ADD COLUMN IF NOT EXISTS "AccountHolderName" text NULL;""");
        db.Database.ExecuteSqlRaw("""ALTER TABLE "Recipients" ADD COLUMN IF NOT EXISTS "SwiftBic" text NULL;""");
        db.Database.ExecuteSqlRaw("""ALTER TABLE "Recipients" ADD COLUMN IF NOT EXISTS "CountryCode" text NULL;""");
    }
    catch
    {
        // ignore nếu không phải Postgres / DB đã đồng bộ
    }
}

static void TryTransactionTransferColumns(AppDbContext db)
{
    try
    {
        db.Database.ExecuteSqlRaw("""ALTER TABLE "Transactions" ADD COLUMN IF NOT EXISTS "ReceiverBankName" text NULL;""");
        db.Database.ExecuteSqlRaw("""ALTER TABLE "Transactions" ADD COLUMN IF NOT EXISTS "ReceiverAccountNumber" text NULL;""");
    }
    catch
    {
        // ignore
    }
}

static void TryUserJapanBankColumns(AppDbContext db)
{
    try
    {
        db.Database.ExecuteSqlRaw("""ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "JapanBankName" text NULL;""");
        db.Database.ExecuteSqlRaw("""ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "JapanBankTopUpUrl" text NULL;""");
    }
    catch
    {
        // ignore
    }
}


// Convert URI dạng `postgresql://user:pass@host/db?sslmode=require`
// sang Npgsql key=value format. Hỗ trợ paste trực tiếp connection string từ Neon/Supabase.
static string? NormalizePostgresConnectionString(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw)) return raw;

    var trimmed = raw.Trim();
    if (!trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) &&
        !trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
    {
        return trimmed;
    }

    var uri = new Uri(trimmed);
    var userInfo = uri.UserInfo.Split(':', 2);

    var nb = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
        SslMode = SslMode.Require
    };

    return nb.ConnectionString;
}
