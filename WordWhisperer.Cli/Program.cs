using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WordWhisperer.Core.Data;
using WordWhisperer.Core.Interfaces;
using WordWhisperer.Core.Services;
using WordWhisperer.Core.Data.Models;
using System.Runtime.InteropServices;

// Create a host with services
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Database context
        services.AddDbContext<DatabaseContext>(options =>
            options.UseSqlite("Data Source=pronunciation.db"));

        // Register services
        services.AddScoped<IPhoneticService, PhoneticService>();
        services.AddScoped<IPronunciationService, PronunciationService>();
        services.AddScoped<IDictionaryService, DictionaryService>();
        services.AddScoped<IUserDataService, UserDataService>();
        services.AddSingleton<PhoneticDictionaryService>();
        services.AddSingleton<MLPhoneticService>();
    });

var host = builder.Build();

// Initialize services
using (var scope = host.Services.CreateScope())
{
    var phoneticDictionaryService = scope.ServiceProvider.GetRequiredService<PhoneticDictionaryService>();
    await phoneticDictionaryService.InitializeAsync();
    
    var mlPhoneticService = scope.ServiceProvider.GetRequiredService<MLPhoneticService>();
    await mlPhoneticService.InitializeAsync();

    var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    await db.Database.EnsureCreatedAsync();
}

// Root command
var rootCommand = new RootCommand("Word Whisperer CLI - A pronunciation and phonetics assistant");

// ------------------------
// Pronounce Command
// ------------------------
var pronounceCommand = new Command("pronounce", "Pronounce a word and display its phonetic representation");
var wordArgument = new Argument<string>("word", "The word to pronounce");
var accentOption = new Option<string>(
    aliases: new[] { "--accent", "-a" },
    description: "Accent to use (american, british)",
    getDefaultValue: () => "american");
var slowOption = new Option<bool>(
    aliases: new[] { "--slow", "-s" },
    description: "Play pronunciation at slower speed");
var withDefinitionOption = new Option<bool>(
    aliases: new[] { "--with-definition", "-d" },
    description: "Show word definition");

pronounceCommand.AddArgument(wordArgument);
pronounceCommand.AddOption(accentOption);
pronounceCommand.AddOption(slowOption);
pronounceCommand.AddOption(withDefinitionOption);

pronounceCommand.SetHandler(async (InvocationContext context) =>
{
    var word = context.ParseResult.GetValueForArgument(wordArgument);
    var accent = context.ParseResult.GetValueForOption(accentOption);
    var slow = context.ParseResult.GetValueForOption(slowOption);
    var withDefinition = context.ParseResult.GetValueForOption(withDefinitionOption);

    using var scope = host.Services.CreateScope();
    var phoneticService = scope.ServiceProvider.GetRequiredService<IPhoneticService>();
    var pronunciationService = scope.ServiceProvider.GetRequiredService<IPronunciationService>();
    var dictionaryService = scope.ServiceProvider.GetRequiredService<IDictionaryService>();
    var userDataService = scope.ServiceProvider.GetRequiredService<IUserDataService>();
    var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

    Console.WriteLine($"Looking up pronunciation for: {word}");

    // Get or update word in database
    var normalizedWord = word.ToLower();
    var wordEntity = await db.Words
        .FirstOrDefaultAsync(w => w.WordText.ToLower() == normalizedWord);
    
    if (wordEntity == null)
    {
        wordEntity = new Word
        {
            WordText = word,
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow
        };
        db.Words.Add(wordEntity);
        await db.SaveChangesAsync();
    }
    else
    {
        wordEntity.LastAccessedAt = DateTime.UtcNow;
        wordEntity.AccessCount++;
        await db.SaveChangesAsync();
    }

    // Add history entry
    await userDataService.AddToHistoryAsync(wordEntity.Id, accent ?? "american");

    // Get phonetics
    var phonetics = await phoneticService.GetOrGeneratePhoneticsAsync(word, accent ?? "american");
    if (phonetics != null)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"IPA: {phonetics.Value.ipa}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Simplified: {phonetics.Value.simplified}");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Could not generate phonetics for this word.");
        Console.ResetColor();
    }

    // Generate and play audio
    var audioPath = await pronunciationService.GetOrGenerateAudioAsync(word, accent ?? "american", slow);
    if (audioPath != null)
    {
        Console.WriteLine($"Audio available at: {audioPath}");
        Console.WriteLine("Playing audio...");
        
        // Basic audio playback (will need platform-specific implementation)
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "afplay" : 
                              Environment.OSVersion.Platform == PlatformID.Win32NT ? "powershell" : "play",
                    Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? $"\"{audioPath}\"" :
                              Environment.OSVersion.Platform == PlatformID.Win32NT ? 
                              $"-c (New-Object System.Media.SoundPlayer '{audioPath}').PlaySync()" : 
                              audioPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Could not play audio: {ex.Message}");
            Console.ResetColor();
        }
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Could not generate audio for this word.");
        Console.ResetColor();
    }

    // Show definition if requested
    if (withDefinition)
    {
        var wordInfo = await dictionaryService.GetWordInfoAsync(word);
        string definition = wordInfo.Item1 ?? string.Empty;
        string? partOfSpeech = wordInfo.Item2;
        
        if (!string.IsNullOrEmpty(definition))
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"Part of Speech: {partOfSpeech ?? "unknown"}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Definition: {definition}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No definition available for this word.");
            Console.ResetColor();
        }
    }
});

// ------------------------
// Phonetic Command
// ------------------------
var phoneticCommand = new Command("phonetic", "Show phonetic spelling for a word");
phoneticCommand.AddArgument(wordArgument);
phoneticCommand.AddOption(accentOption);

phoneticCommand.SetHandler(async (InvocationContext context) =>
{
    var word = context.ParseResult.GetValueForArgument(wordArgument);
    var accent = context.ParseResult.GetValueForOption(accentOption);

    using var scope = host.Services.CreateScope();
    var phoneticService = scope.ServiceProvider.GetRequiredService<IPhoneticService>();
    var userDataService = scope.ServiceProvider.GetRequiredService<IUserDataService>();
    var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

    // Get or update word in database
    var normalizedWord = word.ToLower();
    var wordEntity = await db.Words
        .FirstOrDefaultAsync(w => w.WordText.ToLower() == normalizedWord);
    
    if (wordEntity == null)
    {
        wordEntity = new Word
        {
            WordText = word,
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow
        };
        db.Words.Add(wordEntity);
        await db.SaveChangesAsync();
    }
    else
    {
        wordEntity.LastAccessedAt = DateTime.UtcNow;
        wordEntity.AccessCount++;
        await db.SaveChangesAsync();
    }

    // Add history entry
    await userDataService.AddToHistoryAsync(wordEntity.Id, accent ?? "american");

    // Get phonetics
    var phonetics = await phoneticService.GetOrGeneratePhoneticsAsync(word, accent ?? "american");
    if (phonetics != null)
    {
        Console.WriteLine($"Word: {word}");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"IPA: {phonetics.Value.ipa}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Simplified: {phonetics.Value.simplified}");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Could not generate phonetics for this word.");
        Console.ResetColor();
    }
});

// ------------------------
// Define Command
// ------------------------
var defineCommand = new Command("define", "Show definition for a word");
defineCommand.AddArgument(wordArgument);

defineCommand.SetHandler(async (InvocationContext context) =>
{
    var word = context.ParseResult.GetValueForArgument(wordArgument);

    using var scope = host.Services.CreateScope();
    var dictionaryService = scope.ServiceProvider.GetRequiredService<IDictionaryService>();
    var userDataService = scope.ServiceProvider.GetRequiredService<IUserDataService>();
    var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

    // Get or update word in database
    var normalizedWord = word.ToLower();
    var wordEntity = await db.Words
        .FirstOrDefaultAsync(w => w.WordText.ToLower() == normalizedWord);
    
    if (wordEntity == null)
    {
        wordEntity = new Word
        {
            WordText = word,
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow
        };
        db.Words.Add(wordEntity);
        await db.SaveChangesAsync();
    }
    else
    {
        wordEntity.LastAccessedAt = DateTime.UtcNow;
        wordEntity.AccessCount++;
        await db.SaveChangesAsync();
    }

    // Add history entry
    await userDataService.AddToHistoryAsync(wordEntity.Id, "american");

    // Get definition
    var wordInfo = await dictionaryService.GetWordInfoAsync(word);
    string definition = wordInfo.Item1 ?? string.Empty;
    string? partOfSpeech = wordInfo.Item2;
    
    Console.WriteLine($"Word: {word}");
    if (!string.IsNullOrEmpty(definition))
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"Part of Speech: {partOfSpeech ?? "unknown"}");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"Definition: {definition}");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("No definition available for this word.");
        Console.ResetColor();
    }
});

// ------------------------
// History Command
// ------------------------
var historyCommand = new Command("history", "Show pronunciation history");
var limitOption = new Option<int>(
    aliases: new[] { "--limit", "-l" },
    description: "Number of history entries to show",
    getDefaultValue: () => 10);

historyCommand.AddOption(limitOption);

historyCommand.SetHandler(async (InvocationContext context) =>
{
    var limit = context.ParseResult.GetValueForOption(limitOption);

    using var scope = host.Services.CreateScope();
    var userDataService = scope.ServiceProvider.GetRequiredService<IUserDataService>();
    var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

    var history = await db.History
        .Include(h => h.Word)
        .OrderByDescending(h => h.Timestamp)
        .Take(limit)
        .ToListAsync();

    Console.WriteLine($"Recent Pronunciation History (Last {Math.Min(limit, history.Count)} entries)");
    Console.WriteLine(new string('-', 80));
    Console.WriteLine($"{"Timestamp",-20}{"Word",-20}{"Accent",-15}");
    Console.WriteLine(new string('-', 80));

    foreach (var entry in history)
    {
        Console.WriteLine($"{entry.Timestamp.ToLocalTime(),-20}{entry.Word?.WordText ?? "Unknown",-20}{entry.AccentUsed,-15}");
    }

    Console.WriteLine(new string('-', 80));
});

// ------------------------
// Favorites Command
// ------------------------
var favoritesCommand = new Command("favorites", "Show favorite words");
var tagOption = new Option<string?>(
    aliases: new[] { "--tag", "-t" },
    description: "Filter favorites by tag");

favoritesCommand.AddOption(tagOption);

favoritesCommand.SetHandler(async (InvocationContext context) =>
{
    var tag = context.ParseResult.GetValueForOption(tagOption);

    using var scope = host.Services.CreateScope();
    var userDataService = scope.ServiceProvider.GetRequiredService<IUserDataService>();
    var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

    var query = db.Favorites
        .Include(f => f.Word)
        .AsQueryable();

    if (!string.IsNullOrEmpty(tag))
    {
        query = query.Where(f => f.Tags != null && f.Tags.Contains(tag));
    }

    var favorites = await query
        .OrderByDescending(f => f.AddedAt)
        .ToListAsync();

    Console.WriteLine("Favorite Words");
    if (!string.IsNullOrEmpty(tag))
    {
        Console.WriteLine($"Filtered by tag: {tag}");
    }
    Console.WriteLine(new string('-', 80));
    Console.WriteLine($"{"Word",-20}{"Added",-20}{"Tags",-25}{"Notes",-15}");
    Console.WriteLine(new string('-', 80));

    foreach (var fav in favorites)
    {
        Console.WriteLine($"{fav.Word?.WordText ?? "Unknown",-20}{fav.AddedAt.ToLocalTime(),-20}{fav.Tags ?? "",-25}{fav.Notes ?? "",-15}");
    }

    Console.WriteLine(new string('-', 80));
});

// ------------------------
// Add-Favorite Command
// ------------------------
var addFavoriteCommand = new Command("add-favorite", "Add word to favorites");
addFavoriteCommand.AddArgument(wordArgument);
var notesOption = new Option<string?>(
    aliases: new[] { "--notes", "-n" },
    description: "Notes about the word");
var tagsOption = new Option<string?>(
    aliases: new[] { "--tags", "-t" },
    description: "Comma-separated tags for the word");

addFavoriteCommand.AddOption(notesOption);
addFavoriteCommand.AddOption(tagsOption);

addFavoriteCommand.SetHandler(async (InvocationContext context) =>
{
    var word = context.ParseResult.GetValueForArgument(wordArgument);
    var notes = context.ParseResult.GetValueForOption(notesOption);
    var tags = context.ParseResult.GetValueForOption(tagsOption);

    using var scope = host.Services.CreateScope();
    var userDataService = scope.ServiceProvider.GetRequiredService<IUserDataService>();
    var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

    // Get or create word
    var normalizedWord = word.ToLower();
    var wordEntity = await db.Words
        .FirstOrDefaultAsync(w => w.WordText.ToLower() == normalizedWord);
    
    if (wordEntity == null)
    {
        wordEntity = new Word
        {
            WordText = word,
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow
        };
        db.Words.Add(wordEntity);
        await db.SaveChangesAsync();
    }

    // Check if already a favorite
    var existing = await db.Favorites
        .FirstOrDefaultAsync(f => f.WordId == wordEntity.Id);
    
    if (existing != null)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"'{word}' is already in your favorites. Updating notes and tags.");
        Console.ResetColor();
        
        existing.Notes = notes;
        existing.Tags = tags;
        await db.SaveChangesAsync();
    }
    else
    {
        // Add to favorites
        await userDataService.AddToFavoritesAsync(wordEntity.Id, notes, tags);
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Added '{word}' to favorites!");
        Console.ResetColor();
    }
});

// ------------------------
// Remove-Favorite Command
// ------------------------
var removeFavoriteCommand = new Command("remove-favorite", "Remove word from favorites");
removeFavoriteCommand.AddArgument(wordArgument);

removeFavoriteCommand.SetHandler(async (InvocationContext context) =>
{
    var word = context.ParseResult.GetValueForArgument(wordArgument);

    using var scope = host.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

    // Find word
    var normalizedWord = word.ToLower();
    var wordEntity = await db.Words
        .FirstOrDefaultAsync(w => w.WordText.ToLower() == normalizedWord);
    
    if (wordEntity == null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Word '{word}' not found in the database.");
        Console.ResetColor();
        return;
    }

    // Find and remove from favorites
    var favorite = await db.Favorites
        .FirstOrDefaultAsync(f => f.WordId == wordEntity.Id);
    
    if (favorite != null)
    {
        db.Favorites.Remove(favorite);
        await db.SaveChangesAsync();
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Removed '{word}' from favorites.");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"'{word}' is not in your favorites.");
        Console.ResetColor();
    }
});

// ------------------------
// Settings Command
// ------------------------
var settingsCommand = new Command("settings", "View or change settings");
var keyOption = new Option<string?>(
    aliases: new[] { "--key", "-k" },
    description: "Setting key to get or set");
var valueOption = new Option<string?>(
    aliases: new[] { "--value", "-v" },
    description: "Value to set for the specified key");

settingsCommand.AddOption(keyOption);
settingsCommand.AddOption(valueOption);

settingsCommand.SetHandler(async (InvocationContext context) =>
{
    var key = context.ParseResult.GetValueForOption(keyOption);
    var value = context.ParseResult.GetValueForOption(valueOption);

    using var scope = host.Services.CreateScope();
    var userDataService = scope.ServiceProvider.GetRequiredService<IUserDataService>();
    var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

    // If key and value provided, update setting
    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
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
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Updated setting '{key}' to '{value}'");
        Console.ResetColor();
    }
    // If only key provided, show that setting
    else if (!string.IsNullOrEmpty(key))
    {
        var setting = await db.Settings.FindAsync(key);
        if (setting != null)
        {
            Console.WriteLine($"{setting.Key}: {setting.Value}");
            if (!string.IsNullOrEmpty(setting.Description))
            {
                Console.WriteLine($"  {setting.Description}");
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Setting '{key}' not found.");
            Console.ResetColor();
        }
    }
    // Otherwise, show all settings
    else
    {
        var settings = await db.Settings.ToListAsync();
        Console.WriteLine("Application Settings");
        Console.WriteLine(new string('-', 50));
        
        foreach (var setting in settings)
        {
            Console.WriteLine($"{setting.Key}: {setting.Value}");
            if (!string.IsNullOrEmpty(setting.Description))
            {
                Console.WriteLine($"  {setting.Description}");
            }
            Console.WriteLine();
        }
        
        if (!settings.Any())
        {
            Console.WriteLine("No settings defined yet.");
        }
    }
});

// Add commands to root command
rootCommand.AddCommand(pronounceCommand);
rootCommand.AddCommand(phoneticCommand);
rootCommand.AddCommand(defineCommand);
rootCommand.AddCommand(historyCommand);
rootCommand.AddCommand(favoritesCommand);
rootCommand.AddCommand(addFavoriteCommand);
rootCommand.AddCommand(removeFavoriteCommand);
rootCommand.AddCommand(settingsCommand);

// Run the command
return await rootCommand.InvokeAsync(args);
