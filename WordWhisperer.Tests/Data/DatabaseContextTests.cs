using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WordWhisperer.Core.Data;
using WordWhisperer.Core.Data.Models;

namespace WordWhisperer.Tests.Data;

public class DatabaseContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DatabaseContext _context;

    public DatabaseContextTests()
    {
        // Create and open a connection. This creates the SQLite in-memory database, which will persist until the connection is closed
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Create options for DbContext pointing to the in-memory database
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(_connection)
            .Options;

        // Create the schema and seed some test data
        _context = new DatabaseContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task CanAddAndRetrieveWord()
    {
        // Arrange
        var word = new Word
        {
            WordText = "test",
            Phonetic = "tɛst",
            IpaPhonetic = "tɛst",
            Definition = "A procedure intended to establish quality or performance",
            PartOfSpeech = "noun",
            Source = "test-dictionary"
        };

        // Act
        _context.Words.Add(word);
        await _context.SaveChangesAsync();

        // Assert
        var retrievedWord = await _context.Words.FirstOrDefaultAsync(w => w.WordText == "test");
        Assert.NotNull(retrievedWord);
        Assert.Equal("test", retrievedWord.WordText);
        Assert.Equal("tɛst", retrievedWord.Phonetic);
        Assert.Equal("noun", retrievedWord.PartOfSpeech);
    }

    [Fact]
    public async Task CanAddWordVariant()
    {
        // Arrange
        var word = new Word
        {
            WordText = "tomato",
            Phonetic = "təˈmeɪtoʊ",
            IpaPhonetic = "təˈmeɪtoʊ",
            Definition = "A red or yellow pulpy edible fruit",
            PartOfSpeech = "noun",
            Source = "test-dictionary",
            HasMultiplePron = true
        };

        _context.Words.Add(word);
        await _context.SaveChangesAsync();

        var variant = new WordVariant
        {
            WordId = word.Id,
            Variant = "British",
            Phonetic = "təˈmɑːtəʊ",
            IpaPhonetic = "təˈmɑːtəʊ"
        };

        // Act
        _context.WordVariants.Add(variant);
        await _context.SaveChangesAsync();

        // Assert
        var retrievedVariant = await _context.WordVariants
            .Include(v => v.Word)
            .FirstOrDefaultAsync(v => v.Word.WordText == "tomato" && v.Variant == "British");
            
        Assert.NotNull(retrievedVariant);
        Assert.Equal("təˈmɑːtəʊ", retrievedVariant.Phonetic);
        Assert.NotNull(retrievedVariant.Word);
        Assert.Equal("tomato", retrievedVariant.Word.WordText);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
} 