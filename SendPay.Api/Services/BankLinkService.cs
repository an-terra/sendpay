using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.DTOs.BankLink;
using SendPay.Api.Infrastructure;
using SendPay.Api.Models;
using SendPay.Api.Services.BankLink;

namespace SendPay.Api.Services;

public class BankLinkService(
    AppDbContext db,
    ILogger<BankLinkService> logger,
    IBankLinkAuthorizePathBuilder authorizePaths,
    IDataProtectionProvider dataProtection,
    IAuditService audit) : IBankLinkService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(10);
    private readonly IDataProtector _refProtector = dataProtection.CreateProtector("sendpay.banklink.ref.v1");

    public async Task<BankLinkStartResponse> StartAsync(
        int userId, BankLinkStartRequest req, string? ipAddress, CancellationToken ct = default)
    {
        var bank = JapanBankCatalog.FindByCode(req.BankCode)
            ?? throw AppError.BadRequest(ErrorCodes.BankLinkInvalidBank, "Ngân hàng không hợp lệ.");

        var safeReturn = NormalizeReturnUrl(req.ReturnUrl);

        var existingPending = await db.BankLinkSessions
            .Where(s => s.UserId == userId && s.Status == BankLinkSessionStatus.Pending)
            .ToListAsync(ct);
        foreach (var s in existingPending)
            s.Status = BankLinkSessionStatus.Cancelled;

        var state = GenerateState();
        var now = DateTime.UtcNow;
        var session = new BankLinkSession
        {
            UserId    = userId,
            BankCode  = bank.Code,
            State     = state,
            Status    = BankLinkSessionStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.Add(SessionLifetime),
            ReturnUrl = safeReturn,
            IpAddress = ipAddress,
        };
        db.BankLinkSessions.Add(session);
        await db.SaveChangesAsync(ct);

        var authorizeUrl = authorizePaths.BuildAuthorizeUrl(bank.Code, state, safeReturn);
        logger.LogInformation("BankLink start: user={UserId} bank={Bank}", userId, bank.Code);
        await audit.WriteAsync("banklink.start", $"bank={bank.Code}", userId, ipAddress, ct);

        return new BankLinkStartResponse(state, authorizeUrl, session.ExpiresAt);
    }

    public async Task<FakeBankApproveResponse> FakeApproveAsync(
        FakeBankApproveRequest req, string? ipAddress, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.State))
            throw AppError.BadRequest(ErrorCodes.BankLinkStateMissing, "Thiếu state.");

        await using var trx = await db.Database.BeginTransactionAsync(ct);
        var session = await db.BankLinkSessions
            .FirstOrDefaultAsync(s => s.State == req.State, ct)
            ?? throw AppError.BadRequest(ErrorCodes.BankLinkStateInvalid, "State không hợp lệ hoặc đã hết hạn.");

        if (session.Status != BankLinkSessionStatus.Pending)
            throw AppError.BadRequest(ErrorCodes.BankLinkSessionUsed, "Phiên liên kết đã được sử dụng hoặc bị hủy.");

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            session.Status = BankLinkSessionStatus.Expired;
            await db.SaveChangesAsync(ct);
            await trx.CommitAsync(ct);
            throw AppError.BadRequest(ErrorCodes.BankLinkSessionExpired, "Phiên liên kết đã hết hạn.");
        }

        var bank = JapanBankCatalog.FindByCode(session.BankCode)
            ?? throw AppError.BadRequest(ErrorCodes.BankLinkInvalidBank, "Ngân hàng không hợp lệ.");

        var rawAccount = (req.AccountNo ?? "").Trim();
        if (rawAccount.Length < 4)
            rawAccount = GenerateDemoAccount();

        var masked = MaskAccount(rawAccount);
        var providerRefPlain = $"fake-{bank.Code}-{Guid.NewGuid():N}";
        var providerRefProtected = _refProtector.Protect(providerRefPlain);

        var hasActive = await db.UserBankLinks.AnyAsync(l => l.UserId == session.UserId && l.IsActive && l.IsPrimary, ct);

        var link = new UserBankLink
        {
            UserId        = session.UserId,
            BankCode      = bank.Code,
            BankName      = bank.NameJa,
            AccountMasked = masked,
            ProviderRef   = providerRefProtected,
            Provider      = "fake",
            IsPrimary     = !hasActive,
            IsActive      = true,
            LinkedAt      = DateTime.UtcNow,
        };
        db.UserBankLinks.Add(link);
        await db.SaveChangesAsync(ct);

        session.Status      = BankLinkSessionStatus.Linked;
        session.CompletedAt = DateTime.UtcNow;
        session.LinkId      = link.Id;
        await db.SaveChangesAsync(ct);
        await trx.CommitAsync(ct);

        logger.LogInformation("BankLink linked: user={UserId} bank={Bank} link={LinkId}",
            session.UserId, bank.Code, link.Id);
        await audit.WriteAsync("banklink.linked", $"bank={bank.Code} link={link.Id}", session.UserId, ipAddress, ct);

        var redirectBase = string.IsNullOrWhiteSpace(session.ReturnUrl) ? "/settings" : session.ReturnUrl;
        var sep = redirectBase.Contains('?') ? "&" : "?";
        var redirectUrl = $"{redirectBase}{sep}linked=ok&bank={Uri.EscapeDataString(bank.Code)}";

        return new FakeBankApproveResponse(redirectUrl, link.Id, bank.Code, bank.NameJa, masked);
    }

    public async Task<List<UserBankLinkDto>> GetMyLinksAsync(int userId, CancellationToken ct = default)
    {
        return await db.UserBankLinks.AsNoTracking()
            .Where(l => l.UserId == userId && l.IsActive)
            .OrderByDescending(l => l.IsPrimary)
            .ThenByDescending(l => l.LinkedAt)
            .Select(l => new UserBankLinkDto(
                l.Id, l.BankCode, l.BankName, l.AccountMasked,
                l.IsPrimary, l.IsActive, l.LinkedAt))
            .ToListAsync(ct);
    }

    public async Task<UserBankLinkDto?> GetPrimaryAsync(int userId, CancellationToken ct = default)
    {
        var link = await db.UserBankLinks.AsNoTracking()
            .Where(l => l.UserId == userId && l.IsActive && l.IsPrimary)
            .OrderByDescending(l => l.LinkedAt)
            .FirstOrDefaultAsync(ct);

        if (link is null) return null;
        return new UserBankLinkDto(
            link.Id, link.BankCode, link.BankName, link.AccountMasked,
            link.IsPrimary, link.IsActive, link.LinkedAt);
    }

    public async Task<bool> UnlinkAsync(int userId, int linkId, CancellationToken ct = default)
    {
        var link = await db.UserBankLinks
            .FirstOrDefaultAsync(l => l.Id == linkId && l.UserId == userId && l.IsActive, ct);
        if (link is null) return false;

        link.IsActive   = false;
        link.IsPrimary  = false;
        link.UnlinkedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        if (!await db.UserBankLinks.AnyAsync(l => l.UserId == userId && l.IsActive && l.IsPrimary, ct))
        {
            var fallback = await db.UserBankLinks
                .Where(l => l.UserId == userId && l.IsActive)
                .OrderByDescending(l => l.LinkedAt)
                .FirstOrDefaultAsync(ct);
            if (fallback is not null)
            {
                fallback.IsPrimary = true;
                await db.SaveChangesAsync(ct);
            }
        }

        logger.LogInformation("BankLink unlinked: user={UserId} link={LinkId}", userId, linkId);
        await audit.WriteAsync("banklink.unlink", $"linkId={linkId}", userId, null, ct);
        return true;
    }

    public async Task<bool> SetPrimaryAsync(int userId, int linkId, CancellationToken ct = default)
    {
        var link = await db.UserBankLinks
            .FirstOrDefaultAsync(l => l.Id == linkId && l.UserId == userId && l.IsActive, ct);
        if (link is null) return false;

        var others = await db.UserBankLinks
            .Where(l => l.UserId == userId && l.IsActive && l.Id != linkId)
            .ToListAsync(ct);
        foreach (var o in others) o.IsPrimary = false;
        link.IsPrimary = true;

        await db.SaveChangesAsync(ct);
        return true;
    }

    private static string GenerateState()
    {
        Span<byte> buf = stackalloc byte[32];
        RandomNumberGenerator.Fill(buf);
        return Convert.ToHexString(buf);
    }

    private static string GenerateDemoAccount()
    {
        Span<byte> buf = stackalloc byte[4];
        RandomNumberGenerator.Fill(buf);
        var num = BitConverter.ToUInt32(buf) % 10_000_000;
        return num.ToString("D7");
    }

    private static string MaskAccount(string raw)
    {
        var digits = new string([.. raw.Where(char.IsDigit)]);
        if (digits.Length <= 4) return $"****{digits.PadLeft(4, '0')}";
        return $"****{digits[^4..]}";
    }

    private static string? NormalizeReturnUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "/settings";
        var t = raw.Trim();
        if (!t.StartsWith('/')) return "/settings";
        if (t.StartsWith("//")) return "/settings";
        if (t.Length > 256) return "/settings";
        return t;
    }
}
