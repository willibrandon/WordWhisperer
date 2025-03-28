using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WordWhisperer.Core.Data;
using WordWhisperer.Core.Data.Models;
using WordWhisperer.Core.Services;

namespace WordWhisperer.Tests.Services;

public class UserDataServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DatabaseContext _context;
    private readonly UserDataService _userDataService;

    public UserDataServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new DatabaseContext(options);
        _context.Database.EnsureCreated();

        _userDataService = new UserDataService(_context);
    }

    [Fact]
    public async Task CanAddAndRetrieveHistory()
    {
        // Arrange
        var word = new Word
        {
            WordText = "test",
            Phonetic = "tɛst",
            Definition = "A procedure for testing"
        };
        _context.Words.Add(word);
        await _context.SaveChangesAsync();

        // Act
        await _userDataService.AddToHistoryAsync(word.Id, "american");
        var history = await _userDataService.GetHistoryAsync(10);

        // Assert
        Assert.Single(history);
        Assert.Equal("american", history[0].AccentUsed);
        Assert.Equal(word.Id, history[0].WordId);
    }

    [Fact]
    public async Task CanAddAndRetrieveFavorites()
    {
        // Arrange
        var word = new Word
        {
            WordText = "test",
            Phonetic = "tɛst",
            Definition = "A procedure for testing"
        };
        _context.Words.Add(word);
        await _context.SaveChangesAsync();

        // Act
        await _userDataService.AddToFavoritesAsync(word.Id, "My test note", "test,example");
        var favorites = await _userDataService.GetFavoritesAsync();

        // Assert
        Assert.Single(favorites);
        Assert.Equal("My test note", favorites[0].Notes);
        Assert.Equal("test,example", favorites[0].Tags);
    }

    [Fact]
    public async Task CanFilterFavoritesByTag()
    {
        // Arrange
        var word1 = new Word { WordText = "test1" };
        var word2 = new Word { WordText = "test2" };
        _context.Words.AddRange(word1, word2);
        await _context.SaveChangesAsync();

        await _userDataService.AddToFavoritesAsync(word1.Id, "Note 1", "tag1,common");
        await _userDataService.AddToFavoritesAsync(word2.Id, "Note 2", "tag2,common");

        // Act
        var tag1Favorites = await _userDataService.GetFavoritesAsync("tag1");
        var commonFavorites = await _userDataService.GetFavoritesAsync("common");

        // Assert
        Assert.Single(tag1Favorites);
        Assert.Equal(2, commonFavorites.Count);
    }

    [Fact]
    public async Task CanManageSettings()
    {
        // Arrange & Act
        await _userDataService.UpdateSettingAsync("theme", "dark");
        var theme = await _userDataService.GetSettingAsync("theme");

        // Assert
        Assert.Equal("dark", theme);

        // Update existing setting
        await _userDataService.UpdateSettingAsync("theme", "light");
        theme = await _userDataService.GetSettingAsync("theme");
        Assert.Equal("light", theme);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
} 