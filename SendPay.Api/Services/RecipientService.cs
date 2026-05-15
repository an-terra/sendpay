using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.DTOs.Recipient;
using SendPay.Api.Infrastructure;
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
                 ?? throw AppError.NotFound(ErrorCodes.RecipientNotFound, "Không tìm thấy người nhận.");
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
                ?? throw AppError.NotFound(ErrorCodes.RecipientNotFound, "Không tìm thấy người nhận.");
        r.Name              = req.Name.Trim();
        r.Phone             = req.Phone?.Trim() ?? "";
        r.Note              = req.Note?.Trim() ?? "";
        r.AccountNumber     = req.AccountNumber!.Trim();
        r.AccountHolderName = req.AccountHolderName!.Trim();
        ApplyCountryBankSwift(r, req);
        await db.SaveChangesAsync();
        return ToResponse(r);
    }

    public async Task DeleteAsync(int userId, int id)
    {
        var r = await db.Recipients.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId)
                ?? throw AppError.NotFound(ErrorCodes.RecipientNotFound, "Không tìm thấy người nhận.");
        db.Recipients.Remove(r);
        await db.SaveChangesAsync();
    }

    private static void EnsureRecipientValid(RecipientRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw AppError.BadRequest(ErrorCodes.RecipientNameRequired, "Tên người nhận không được để trống.");
        if (string.IsNullOrWhiteSpace(req.BankName))
            throw AppError.BadRequest(ErrorCodes.RecipientBankRequired, "Tên ngân hàng không được để trống.");
        if (string.IsNullOrWhiteSpace(req.AccountHolderName))
            throw AppError.BadRequest(ErrorCodes.RecipientHolderRequired,
                "Tên chủ tài khoản (theo sổ ngân hàng) không được để trống.");
        if (string.IsNullOrWhiteSpace(req.AccountNumber))
            throw AppError.BadRequest(ErrorCodes.RecipientAccountRequired, "Số tài khoản không được để trống.");
        var acctKey = OtpPayloadBuilder.NormalizeAccountKey(req.AccountNumber);
        if (acctKey.Length < 6)
            throw AppError.BadRequest(ErrorCodes.RecipientAccountInvalid,
                "Số tài khoản không hợp lệ (ít nhất 6 ký tự chữ hoặc số).");

        var cc = CountryBankCatalog.NormalizeCountry(req.CountryCode);
        if (CountryBankCatalog.IsCatalogCountry(cc))
        {
            if (!CountryBankCatalog.TryGetSwiftByBankName(cc, req.BankName, out _))
                throw AppError.BadRequest(ErrorCodes.RecipientBankNotInCatalog,
                    "Hãy chọn ngân hàng đúng trong danh sách gợi ý theo quốc gia (để hệ thống lấy mã SWIFT).");
        }
    }

    private static void ApplyCountryBankSwift(Recipient r, RecipientRequest req)
    {
        var cc = CountryBankCatalog.NormalizeCountry(req.CountryCode);
        r.CountryCode = cc;
        if (CountryBankCatalog.IsCatalogCountry(cc))
        {
            if (!CountryBankCatalog.TryGetSwiftByBankName(cc, req.BankName, out var swift))
                throw AppError.BadRequest(ErrorCodes.RecipientSwiftMissing,
                    "Không xác định được mã SWIFT cho ngân hàng đã chọn.");
            r.SwiftBic = swift;
            r.BankName = CountryBankCatalog.CanonicalBankName(cc, req.BankName) ?? req.BankName!.Trim();
        }
        else
        {
            r.SwiftBic = null;
            r.BankName = req.BankName!.Trim();
        }
    }

    private static Recipient FromRequest(int userId, RecipientRequest req)
    {
        var r = new Recipient
        {
            UserId            = userId,
            Name              = req.Name.Trim(),
            Phone             = req.Phone?.Trim() ?? "",
            Note              = req.Note?.Trim() ?? "",
            AccountNumber     = req.AccountNumber!.Trim(),
            AccountHolderName = req.AccountHolderName!.Trim()
        };
        ApplyCountryBankSwift(r, req);
        return r;
    }

    private static RecipientResponse ToResponse(Recipient r) =>
        new(r.Id, r.Name,
            string.IsNullOrWhiteSpace(r.Phone) ? null : r.Phone,
            r.Note,
            string.IsNullOrWhiteSpace(r.CountryCode) ? null : r.CountryCode,
            r.BankName, r.AccountNumber, r.AccountHolderName, r.SwiftBic, r.CreatedAt);
}
