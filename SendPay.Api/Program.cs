using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Scalar.AspNetCore;
using SendPay.Api.Data;
using SendPay.Api.Infrastructure;
using SendPay.Api.Models;
using SendPay.Api.Security;
using SendPay.Api.Services;
using SendPay.Api.Services.BankLink;

var builder = WebApplication.CreateBuilder(args);

// ── Data Protection (mã hóa ProviderRef trong DB, khóa lưu DataProtectionKeys/) ──
var keysPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("SendPay.Api");

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

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

// ── Services ──────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<RatesService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IBankLinkAuthorizePathBuilder, FakeBankLinkAuthorizePathBuilder>();
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
        opt.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer           = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidateAudience         = true,
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.FromMinutes(1),
        };
        opt.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                await using var scope = ctx.HttpContext.RequestServices.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                if (ctx.SecurityToken is JwtSecurityToken jwt)
                {
                    var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                    if (!string.IsNullOrEmpty(jti) &&
                        await db.JwtBlacklistEntries.AsNoTracking().AnyAsync(x => x.Jti == jti))
                    {
                        ctx.Fail("Token đã thu hồi.");
                        ctx.Response.Headers.TryAdd("X-Token-Revoked", "1");
                        return;
                    }
                }

                var sub = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(sub, out var uid))
                    return;
                var active = await db.Users.AsNoTracking()
                    .Where(u => u.Id == uid)
                    .Select(u => u.IsActive)
                    .FirstOrDefaultAsync();
                if (!active)
                {
                    ctx.Fail("Tài khoản đã bị khóa.");
                    ctx.Response.Headers.TryAdd("X-User-Inactive", "1");
                }
            },
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
            p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
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
    ReconciliationSchema.EnsureTables(db);
    BankLinkSchema.EnsureTables(db);
    SecuritySchema.EnsureTables(db);

    SeedInitialAdmin(app);
}

// ── Middleware pipeline ────────────────────────────────────
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

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


static void SeedInitialAdmin(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("InitialAdmin");

    if (db.Users.Any(u => u.IsAdmin)) return;

    var email = (cfg["InitialAdmin:Email"] ?? "admin@sendpay.com").Trim();
    var phone = (cfg["InitialAdmin:Phone"] ?? "0000000000").Trim();
    var name  = (cfg["InitialAdmin:FullName"] ?? "Admin").Trim();
    var pwd   = cfg["InitialAdmin:Password"]?.Trim();

    if (string.IsNullOrEmpty(pwd))
    {
        if (env.IsProduction())
            throw new InvalidOperationException(
                "Production: đặt biến môi trường InitialAdmin__Password (mật khẩu mạnh, xem PasswordPolicy) trước khi chạy lần đầu.");

        pwd = "Dev-Admin-ChangeMe-9!";
        logger.LogWarning(
            "InitialAdmin:Password chưa cấu hình — dùng mật khẩu dev mặc định. Hãy đổi ngay hoặc đặt trong User Secrets.");
    }
    else
    {
        PasswordPolicy.EnsureStrongOrThrow(pwd);
    }

    db.Users.Add(new User
    {
        FullName     = name,
        Email        = email,
        Phone        = phone,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(pwd),
        IsAdmin      = true,
        IsActive     = true
    });
    db.SaveChanges();
    logger.LogInformation("Đã tạo tài khoản admin đầu tiên: {Email}", email);
}

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
        // ignore
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
        Host     = uri.Host,
        Port     = uri.IsDefaultPort ? 5432 : uri.Port,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
        SslMode  = SslMode.Require
    };

    return nb.ConnectionString;
}
