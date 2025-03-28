using WordWhisperer.Core.Data.Models;

namespace WordWhisperer.Core.Interfaces;

public interface IUserDataService
{
    Task AddToHistoryAsync(int wordId, string accentUsed);
    Task<List<History>> GetHistoryAsync(int limit = 50);
    Task AddToFavoritesAsync(int wordId, string? notes, string? tags);
    Task<List<Favorite>> GetFavoritesAsync(string? tag = null);
    Task UpdateSettingAsync(string key, string value);
    Task<string?> GetSettingAsync(string key);
    Task<Dictionary<string, string>> GetAllSettingsAsync();
} 