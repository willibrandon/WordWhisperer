using Microsoft.EntityFrameworkCore;
using WordWhisperer.Core.Data;
using WordWhisperer.Core.Data.Models;
using WordWhisperer.Core.Interfaces;

namespace WordWhisperer.Core.Services;

public class UserDataService(DatabaseContext db) : IUserDataService
{
    public async Task AddToHistoryAsync(int wordId, string accentUsed)
    {
        var entry = new History
        {
            WordId = wordId,
            AccentUsed = accentUsed,
            Timestamp = DateTime.UtcNow
        };

        db.History.Add(entry);
        await db.SaveChangesAsync();
    }

    public async Task<List<History>> GetHistoryAsync(int limit = 50)
    {
        return await db.History
            .Include(h => h.Word)
            .OrderByDescending(h => h.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task AddToFavoritesAsync(int wordId, string? notes, string? tags)
    {
        var favorite = new Favorite
        {
            WordId = wordId,
            Notes = notes,
            Tags = tags,
            AddedAt = DateTime.UtcNow
        };

        db.Favorites.Add(favorite);
        await db.SaveChangesAsync();
    }

    public async Task<List<Favorite>> GetFavoritesAsync(string? tag = null)
    {
        var query = db.Favorites.Include(f => f.Word).AsQueryable();
        if (!string.IsNullOrEmpty(tag))
        {
            query = query.Where(f => f.Tags != null && f.Tags.Contains(tag));
        }

        return await query.ToListAsync();
    }

    public async Task UpdateSettingAsync(string key, string value)
    {
        var setting = await db.Settings.FindAsync(key);
        if (setting == null)
        {
            setting = new Setting
            {
                Key = key,
                Value = value
            };
            db.Settings.Add(setting);
        }
        else
        {
            setting.Value = value;
        }

        await db.SaveChangesAsync();
    }

    public async Task<string?> GetSettingAsync(string key)
    {
        var setting = await db.Settings.FindAsync(key);
        return setting?.Value;
    }
}
