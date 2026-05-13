using Microsoft.EntityFrameworkCore;
using SendPay.Api.Data;
using SendPay.Api.DTOs.Recipient;
using SendPay.Api.Models;

namespace SendPay.Api.Services;

public class RecipientService(AppDbContext db) : IRecipientService
{
    public async Task<List<RecipientResponse>> GetAllAsync(int userId) =>
        await db.Recipients
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RecipientResponse(r.Id, r.Name, r.Phone, r.Note, r.CreatedAt))
            .ToListAsync();

    public async Task<RecipientResponse> AddAsync(int userId, RecipientRequest req)
    {
        var r = new Recipient { UserId = userId, Name = req.Name, Phone = req.Phone, Note = req.Note };
        db.Recipients.Add(r);
        await db.SaveChangesAsync();
        return new RecipientResponse(r.Id, r.Name, r.Phone, r.Note, r.CreatedAt);
    }

    public async Task DeleteAsync(int userId, int id)
    {
        var r = await db.Recipients.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người nhận.");
        db.Recipients.Remove(r);
        await db.SaveChangesAsync();
    }
}
