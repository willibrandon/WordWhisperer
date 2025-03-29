using Microsoft.EntityFrameworkCore;
using WordWhisperer.Api.Models;
using WordWhisperer.Core.Data;
using WordWhisperer.Core.Data.Models;
using WordWhisperer.Core.Interfaces;
using WordWhisperer.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure SQLite
builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=pronunciation.db",
        b => b.MigrationsAssembly("WordWhisperer.Api")
    ));

// Register services
builder.Services.AddScoped<IPronunciationService, PronunciationService>();
builder.Services.AddScoped<MLPhoneticService>();
builder.Services.AddScoped<IPhoneticService, PhoneticService>();
builder.Services.AddScoped<IDictionaryService, DictionaryService>();

// Register the PhoneticDictionaryService as a singleton since it maintains dictionary state
builder.Services.AddSingleton<PhoneticDictionaryService>();

var app = builder.Build();

// Initialize the phonetic dictionary
using (var scope = app.Services.CreateScope())
{
    var dictionaryService = scope.ServiceProvider.GetRequiredService<IDictionaryService>();
    await dictionaryService.InitializeAsync();
    var phoneticDictionaryService = scope.ServiceProvider.GetRequiredService<PhoneticDictionaryService>();
    await phoneticDictionaryService.InitializeAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    context.Database.EnsureCreated();
}

// Word pronunciation endpoints
app.MapGet("/api/pronunciation/{word}", async (
    string word,
    string? accent,
    bool? slow,
    IPhoneticService phoneticService,
    IPronunciationService pronunciationService,
    IDictionaryService dictionaryService,
    DatabaseContext db) =>
{
    accent ??= "american";
    slow ??= false;

    var phonetics = await phoneticService.GetOrGeneratePhoneticsAsync(word, accent);
    var audioPath = await pronunciationService.GetOrGenerateAudioAsync(word, accent, slow.Value);
    var (definition, partOfSpeech) = await dictionaryService.GetWordInfoAsync(word);

    // Get or create word entry
    var normalizedWord = word.ToLower();
    var wordEntry = await db.Words.FirstOrDefaultAsync(w => w.WordText.ToLower() == normalizedWord);
    if (wordEntry == null)
    {
        wordEntry = new Word
        {
            WordText = word,
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow
        };
        db.Words.Add(wordEntry);
        await db.SaveChangesAsync();
    }

    // Add history entry
    var historyEntry = new History
    {
        WordId = wordEntry.Id,
        Timestamp = DateTime.UtcNow,
        AccentUsed = accent
    };
    db.History.Add(historyEntry);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        word,
        accent,
        phonetics,
        audioPath,
        definition,
        partOfSpeech
    });
})
.WithName("GetWordPronunciation")
.WithOpenApi();

app.MapGet("/api/pronunciation/{word}/audio", async (
    string word,
    string? accent,
    bool? slow,
    IPronunciationService pronunciationService) =>
{
    accent ??= "american";
    slow ??= false;

    var audioPath = await pronunciationService.GetOrGenerateAudioAsync(word, accent, slow.Value);
    if (audioPath == null || !File.Exists(audioPath))
        return Results.NotFound();

    // Serve the WAV file with the correct MIME type
    return Results.File(audioPath, "audio/wav");
})
.WithName("GetWordAudio")
.WithOpenApi();

app.MapGet("/api/pronunciation/{word}/phonetic", async (
    string word,
    string? accent,
    IPhoneticService phoneticService,
    DatabaseContext db) =>
{
    accent ??= "american";
    var phonetics = await phoneticService.GetOrGeneratePhoneticsAsync(word, accent);
    if (phonetics == null)
        return Results.NotFound();

    // Get or create word entry
    var normalizedWord = word.ToLower();
    var wordEntry = await db.Words.FirstOrDefaultAsync(w => w.WordText.ToLower() == normalizedWord);
    if (wordEntry == null)
    {
        wordEntry = new Word
        {
            WordText = word,
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow
        };
        db.Words.Add(wordEntry);
        await db.SaveChangesAsync();
    }

    // Add history entry
    var historyEntry = new History
    {
        WordId = wordEntry.Id,
        Timestamp = DateTime.UtcNow,
        AccentUsed = accent
    };
    db.History.Add(historyEntry);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        word,
        accent,
        ipa = phonetics.Value.ipa,
        simplified = phonetics.Value.simplified
    });
})
.WithName("GetWordPhonetics")
.WithOpenApi();

app.MapGet("/api/pronunciation/{word}/definition", async (
    string word,
    IDictionaryService dictionaryService,
    DatabaseContext db) =>
{
    var (definition, partOfSpeech) = await dictionaryService.GetWordInfoAsync(word);
    if (definition == null)
        return Results.NotFound();

    // Get or create word entry
    var normalizedWord = word.ToLower();
    var wordEntry = await db.Words.FirstOrDefaultAsync(w => w.WordText.ToLower() == normalizedWord);
    if (wordEntry == null)
    {
        wordEntry = new Word
        {
            WordText = word,
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow
        };
        db.Words.Add(wordEntry);
        await db.SaveChangesAsync();
    }

    // Add history entry
    var historyEntry = new History
    {
        WordId = wordEntry.Id,
        Timestamp = DateTime.UtcNow,
        AccentUsed = "american" // Default accent for definition lookup
    };
    db.History.Add(historyEntry);
    await db.SaveChangesAsync();

    return Results.Ok(new { definition, partOfSpeech });
})
.WithName("GetWordDefinition")
.WithOpenApi();

// History endpoints
app.MapGet("/api/history", async (
    int? page,
    int? pageSize,
    DatabaseContext db) =>
{
    page ??= 1;
    pageSize ??= 50;

    var history = await db.History
        .Include(h => h.Word)
        .OrderByDescending(h => h.Timestamp)
        .Skip((page.Value - 1) * pageSize.Value)
        .Take(pageSize.Value)
        .Select(h => new
        {
            h.Id,
            h.Timestamp,
            h.AccentUsed,
            Word = h.Word.WordText
        })
        .ToListAsync();

    return Results.Ok(history);
})
.WithName("GetHistory")
.WithOpenApi();

app.MapGet("/api/history/recent", async (
    int? limit,
    DatabaseContext db) =>
{
    limit ??= 10;

    var recent = await db.History
        .Include(h => h.Word)
        .OrderByDescending(h => h.Timestamp)
        .Take(limit.Value)
        .Select(h => new
        {
            h.Id,
            h.Timestamp,
            h.AccentUsed,
            Word = h.Word.WordText
        })
        .ToListAsync();

    return Results.Ok(recent);
})
.WithName("GetRecentHistory")
.WithOpenApi();

// Favorites endpoints
app.MapPost("/api/favorites", async (
    FavoriteRequest request,
    DatabaseContext db) =>
{
    var favorite = new Favorite
    {
        WordId = request.WordId,
        Notes = request.Notes,
        Tags = request.Tags
    };

    db.Favorites.Add(favorite);
    await db.SaveChangesAsync();

    return Results.Created($"/api/favorites/{favorite.Id}", favorite);
})
.WithName("AddFavorite")
.WithOpenApi();

app.MapGet("/api/favorites", async (
    string? tag,
    int? page,
    int? pageSize,
    DatabaseContext db) =>
{
    page ??= 1;
    pageSize ??= 50;

    var query = db.Favorites
        .Include(f => f.Word)
        .AsQueryable();

    if (!string.IsNullOrEmpty(tag))
    {
        query = query.Where(f => f.Tags != null && f.Tags.Contains(tag));
    }

    var favorites = await query
        .OrderByDescending(f => f.AddedAt)
        .Skip((page.Value - 1) * pageSize.Value)
        .Take(pageSize.Value)
        .ToListAsync();

    return Results.Ok(favorites);
})
.WithName("GetFavorites")
.WithOpenApi();

app.MapPut("/api/favorites/{id}", async (
    int id,
    FavoriteRequest request,
    DatabaseContext db) =>
{
    var favorite = await db.Favorites.FindAsync(id);
    if (favorite == null)
        return Results.NotFound();

    favorite.Notes = request.Notes;
    favorite.Tags = request.Tags;
    await db.SaveChangesAsync();

    return Results.Ok(favorite);
})
.WithName("UpdateFavorite")
.WithOpenApi();

app.MapDelete("/api/favorites/{id}", async (
    int id,
    DatabaseContext db) =>
{
    var favorite = await db.Favorites.FindAsync(id);
    if (favorite == null)
        return Results.NotFound();

    db.Favorites.Remove(favorite);
    await db.SaveChangesAsync();

    return Results.Ok();
})
.WithName("DeleteFavorite")
.WithOpenApi();

// Settings endpoints
app.MapGet("/api/settings", async (DatabaseContext db) =>
{
    var settings = await db.Settings.ToListAsync();
    return Results.Ok(settings);
})
.WithName("GetSettings")
.WithOpenApi();

app.MapPut("/api/settings/{key}", async (
    string key,
    string value,
    DatabaseContext db) =>
{
    var setting = await db.Settings.FindAsync(key);
    if (setting == null)
    {
        setting = new Setting { Key = key, Value = value };
        db.Settings.Add(setting);
    }
    else
    {
        setting.Value = value;
    }

    await db.SaveChangesAsync();
    return Results.Ok(setting);
})
.WithName("UpdateSetting")
.WithOpenApi();

app.Run();
