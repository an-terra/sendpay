using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.DTOs.Recipient;
using SendPay.Api.Models;

namespace SendPay.Api.Services;

public class RecipientService(AppDbContext db) : IRecipientService
{
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
        r.Name               = req.Name.Trim();
        r.Phone              = req.Phone?.Trim() ?? "";
        r.Note               = req.Note?.Trim() ?? "";
        r.BankName           = string.IsNullOrWhiteSpace(req.BankName) ? null : req.BankName.Trim();
        r.AccountNumber      = string.IsNullOrWhiteSpace(req.AccountNumber) ? null : req.AccountNumber.Trim();
        r.AccountHolderName = string.IsNullOrWhiteSpace(req.AccountHolderName) ? null : req.AccountHolderName.Trim();
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
        var p = OtpPayloadBuilder.NormalizePhone(req.Phone);
        var a = OtpPayloadBuilder.NormalizePhone(req.AccountNumber);
        if (string.IsNullOrEmpty(p) && string.IsNullOrEmpty(a))
            throw new InvalidOperationException("Nhập ít nhất số điện thoại hoặc số tài khoản ngân hàng.");
    }

    private static Recipient FromRequest(int userId, RecipientRequest req)
    {
        return new Recipient
        {
            UserId              = userId,
            Name                = req.Name.Trim(),
            Phone               = req.Phone?.Trim() ?? "",
            Note                = req.Note?.Trim() ?? "",
            BankName            = string.IsNullOrWhiteSpace(req.BankName) ? null : req.BankName.Trim(),
            AccountNumber       = string.IsNullOrWhiteSpace(req.AccountNumber) ? null : req.AccountNumber.Trim(),
            AccountHolderName   = string.IsNullOrWhiteSpace(req.AccountHolderName) ? null : req.AccountHolderName.Trim()
        };
    }

    private static RecipientResponse ToResponse(Recipient r) =>
        new(r.Id, r.Name,
            string.IsNullOrWhiteSpace(r.Phone) ? null : r.Phone,
            r.Note, r.BankName, r.AccountNumber, r.AccountHolderName, r.CreatedAt);
}
