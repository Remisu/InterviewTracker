using InterviewTracker.Data;
using InterviewTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace InterviewTracker.Services;

public class ContactService(IDbContextFactory<AppDbContext> factory)
{
    public async Task<List<Contact>> GetByApplicationAsync(int appId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Contacts
            .Where(c => c.JobApplicationId == appId)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.FullName)
            .ToListAsync();
    }

    public async Task<Contact?> GetByIdAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Contacts.FindAsync(id);
    }

    public async Task<Contact> CreateAsync(Contact contact)
    {
        await using var db = await factory.CreateDbContextAsync();
        contact.CreatedAt = DateTime.UtcNow;
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();
        return contact;
    }

    public async Task UpdateAsync(Contact contact)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.Contacts.Update(contact);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var c = await db.Contacts.FindAsync(id);
        if (c is null) return;
        db.Contacts.Remove(c);
        await db.SaveChangesAsync();
    }
}
