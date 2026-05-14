using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.DTOs.Recipient;
using SendPay.Api.Models;

namespace SendPay.Api.Services;

public class RecipientService(AppDbContext db) : IRecipientService
{
    private static readonly Regex BicRegex = new("^[A-Z]{6}[A-Z0-9]{2}([A-Z0-9]{3})?$", RegexOptions.Compiled);

    public async Task<List<RecipientResponse>> GetAllAsync(int userId)
    {
        var rows = await db.Recipients.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return rows.Select(ToResponse).ToList();
    }

    public async Task<RecipientResponse> GetByIdAsync(int userId, int id)
    {
        var r = await db.Recipients.AsNoTracking()
                     .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId)
                 ?? throw new KeyNotFoundException("Không tìm thấy người nhận.");
        return ToResponse(r);
    }

    public async Task<RecipientResponse> AddAsync(int userId, RecipientRequest req)
    {
        EnsureRecipientValid(req);
        var r = FromRequest(userId, req);
        db.Recipients.Add(r);
        await db.SaveChangesAsync();
        return ToResponse(r);
    }

    public async Task<RecipientResponse> UpdateAsync(int userId, int id, RecipientRequest req)
    {
        EnsureRecipientValid(req);
        var r = await db.Recipients.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId)
                ?? throw new KeyNotFoundException("Không tìm thấy người nhận.");
        r.Name              = req.Name.Trim();
        r.Phone             = req.Phone?.Trim() ?? "";
        r.Note              = req.Note?.Trim() ?? "";
        r.BankName          = req.BankName!.Trim();
        r.AccountNumber     = req.AccountNumber!.Trim();
        r.AccountHolderName = req.AccountHolderName!.Trim();
        r.SwiftBic          = NormalizeSwiftOrThrow(req.SwiftBic);
        await db.SaveChangesAsync();
        return ToResponse(r);
    }

    public async Task DeleteAsync(int userId, int id)
    {
        var r = await db.Recipients.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId)
                ?? throw new KeyNotFoundException("Không tìm thấy người nhận.");
        db.Recipients.Remove(r);
        await db.SaveChangesAsync();
    }

    private static void EnsureRecipientValid(RecipientRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new InvalidOperationException("Tên người nhận không được để trống.");
        if (string.IsNullOrWhiteSpace(req.BankName))
            throw new InvalidOperationException("Tên ngân hàng không được để trống.");
        if (string.IsNullOrWhiteSpace(req.AccountHolderName))
            throw new InvalidOperationException("Tên chủ tài khoản (theo sổ ngân hàng) không được để trống.");
        if (string.IsNullOrWhiteSpace(req.AccountNumber))
            throw new InvalidOperationException("Số tài khoản không được để trống.");
        var acctKey = OtpPayloadBuilder.NormalizeAccountKey(req.AccountNumber);
        if (acctKey.Length < 6)
            throw new InvalidOperationException("Số tài khoản không hợp lệ (ít nhất 6 ký tự chữ hoặc số).");
        _ = NormalizeSwiftOrThrow(req.SwiftBic);
    }

    private static string NormalizeSwiftOrThrow(string? swift)
    {
        if (string.IsNullOrWhiteSpace(swift))
            throw new InvalidOperationException("Mã SWIFT/BIC không được để trống.");
        var s = string.Concat(swift.Trim().ToUpperInvariant().Where(c => c is >= 'A' and <= 'Z' || char.IsDigit(c)));
        if (!BicRegex.IsMatch(s))
            throw new InvalidOperationException("Mã SWIFT/BIC không hợp lệ (8 hoặc 11 ký tự, ví dụ BIDVVNVX).");
        return s;
    }

    private static Recipient FromRequest(int userId, RecipientRequest req)
    {
        var swift = NormalizeSwiftOrThrow(req.SwiftBic);
        return new Recipient
        {
            UserId            = userId,
            Name              = req.Name.Trim(),
            Phone             = req.Phone?.Trim() ?? "",
            Note              = req.Note?.Trim() ?? "",
            BankName          = req.BankName!.Trim(),
            AccountNumber     = req.AccountNumber!.Trim(),
            AccountHolderName = req.AccountHolderName!.Trim(),
            SwiftBic          = swift
        };
    }

    private static RecipientResponse ToResponse(Recipient r) =>
        new(r.Id, r.Name,
            string.IsNullOrWhiteSpace(r.Phone) ? null : r.Phone,
            r.Note, r.BankName, r.AccountNumber, r.AccountHolderName, r.SwiftBic, r.CreatedAt);
}
