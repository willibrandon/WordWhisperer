Below is a step-by-step implementation guide derived from the **Word Whisperer** design document. This guide is organized into logical “bite-sized” chunks, each focusing on a clear objective. At the end of each chunk, you’ll find a step for verification, testing, and a recommended Git commit to ensure the project evolves in a controlled, stable manner.

---

# Implementation Guide

## Chunk 1: Project Setup and Basic Structure

### 1. Objective
- Establish the primary solution structure.
- Create folders and projects for:
  1. **Core** (Services, Models, Data Access)
  2. **API** (ASP.NET Core Minimal API or standard controllers)
  3. **CLI** (.NET console app using System.CommandLine)
  4. **Electron UI** (React/Tailwind front-end inside Electron)
  5. **Shared Libraries** (if needed for shared models or utility classes)

### 2. Steps

1. **Initialize Git Repository**
   - Create a new folder for the project (e.g., `PronunciationAssistant`).
   - Initialize a git repo:  
     ```bash
     git init
     ```
   - Create a `.gitignore` (using recommended .NET and Node patterns).

2. **Create the .NET Solution**
   - From the root directory, create a solution file:  
     ```bash
     dotnet new sln -n PronunciationAssistant
     ```
   - Create the **Core** project:
     ```bash
     dotnet new classlib -n PronunciationAssistant.Core
     ```
   - Create the **API** project (ASP.NET Core):
     ```bash
     dotnet new webapi -n PronunciationAssistant.Api
     ```
   - Create the **CLI** project:
     ```bash
     dotnet new console -n PronunciationAssistant.Cli
     ```
   - Add these projects to the solution:
     ```bash
     dotnet sln add PronunciationAssistant.Core/PronunciationAssistant.Core.csproj
     dotnet sln add PronunciationAssistant.Api/PronunciationAssistant.Api.csproj
     dotnet sln add PronunciationAssistant.Cli/PronunciationAssistant.Cli.csproj
     ```

3. **Establish Folder Structure**
   - **PronunciationAssistant.Core**  
     - `Services`  
       - `PronunciationService.cs` (placeholder)  
       - `PhoneticService.cs` (placeholder)  
       - `DictionaryService.cs` (placeholder)  
       - `UserDataService.cs` (placeholder)  
       - `ConfigurationService.cs` (placeholder)  
     - `Data`  
       - `DatabaseContext.cs` (placeholder)  
       - `Models` (tables/entities)  
     - `Interfaces` (contracts for services)  
   - **PronunciationAssistant.Api**  
     - `Controllers` or minimal API endpoints  
     - `Program.cs`
   - **PronunciationAssistant.Cli**  
     - `Program.cs`
   - **ElectronUI** (we’ll create this in a later chunk).  

4. **Basic Project References**
   - Reference **Core** library from the **API** project:
     ```xml
     <!-- In PronunciationAssistant.Api.csproj -->
     <ProjectReference Include="..\PronunciationAssistant.Core\PronunciationAssistant.Core.csproj" />
     ```
   - Reference **Core** library from the **CLI** project similarly.

5. **Basic Build Verification**
   - Run a `dotnet build` at the solution level to ensure everything compiles.

### 3. Verification & Git Commit
1. **Verification**:  
   - Confirm you have a valid .NET solution with three projects (Core, API, CLI) that build successfully.
   - Check that the folder structure aligns with the design document’s requirements.

2. **Testing**:  
   - For now, no functional tests exist. Just confirm the solution builds.

3. **Git Commit**:
   ```bash
   git add .
   git commit -m "Initial solution setup with Core, API, and CLI projects"
   ```

---

## Chunk 2: Database Setup with EF Core (SQLite)

### 1. Objective
- Implement the database schema using **Entity Framework Core** and configure a local **SQLite** database.
- Create initial migration and apply it.

### 2. Steps

1. **Install EF Core SQLite Packages** (in **Core** or in **API**, depending on where `DbContext` resides):
   ```bash
   cd PronunciationAssistant.Core
   dotnet add package Microsoft.EntityFrameworkCore
   dotnet add package Microsoft.EntityFrameworkCore.Sqlite
   dotnet add package Microsoft.EntityFrameworkCore.Design
   ```

2. **Create Data Models** (in `PronunciationAssistant.Core/Data/Models`):
   - **Words** model (based on the design document):
     ```csharp
     public class Word
     {
         public int Id { get; set; }
         public string WordText { get; set; } = string.Empty;
         public string? Phonetic { get; set; }
         public string? IpaPhonetic { get; set; }
         public string? AudioPath { get; set; }
         public string? Definition { get; set; }
         public string? PartOfSpeech { get; set; }
         public string? Source { get; set; }
         public bool IsGenerated { get; set; }
         public bool HasMultiplePron { get; set; }
         public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
         public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
         public int AccessCount { get; set; }
     }
     ```
   - **WordVariant** model:
     ```csharp
     public class WordVariant
     {
         public int Id { get; set; }
         public int WordId { get; set; }
         public Word? Word { get; set; }
         public string Variant { get; set; } = string.Empty;
         public string? Phonetic { get; set; }
         public string? IpaPhonetic { get; set; }
         public string? AudioPath { get; set; }
     }
     ```
   - **Favorite** model:
     ```csharp
     public class Favorite
     {
         public int Id { get; set; }
         public int WordId { get; set; }
         public Word? Word { get; set; }
         public string? Notes { get; set; }
         public string? Tags { get; set; }
         public DateTime AddedAt { get; set; } = DateTime.UtcNow;
     }
     ```
   - **History** model:
     ```csharp
     public class History
     {
         public int Id { get; set; }
         public int WordId { get; set; }
         public Word? Word { get; set; }
         public DateTime Timestamp { get; set; } = DateTime.UtcNow;
         public string? AccentUsed { get; set; }
     }
     ```
   - **Setting** model:
     ```csharp
     public class Setting
     {
         public string Key { get; set; } = string.Empty;
         public string? Value { get; set; }
         public string? Description { get; set; }
     }
     ```

3. **Create the `DatabaseContext`** (e.g., `PronunciationAssistant.Core/Data/DatabaseContext.cs`):
   ```csharp
   using Microsoft.EntityFrameworkCore;

   namespace PronunciationAssistant.Core.Data
   {
       public class DatabaseContext : DbContext
       {
           public DbSet<Word> Words { get; set; }
           public DbSet<WordVariant> WordVariants { get; set; }
           public DbSet<Favorite> Favorites { get; set; }
           public DbSet<History> History { get; set; }
           public DbSet<Setting> Settings { get; set; }

           public DatabaseContext(DbContextOptions<DatabaseContext> options)
               : base(options)
           {
           }

           protected override void OnModelCreating(ModelBuilder modelBuilder)
           {
               base.OnModelCreating(modelBuilder);

               // Composite key example for Setting if desired:
               modelBuilder.Entity<Setting>()
                   .HasKey(s => s.Key);

               // Relationship mappings:
               modelBuilder.Entity<WordVariant>()
                   .HasOne(wv => wv.Word)
                   .WithMany()
                   .HasForeignKey(wv => wv.WordId);

               modelBuilder.Entity<Favorite>()
                   .HasOne(f => f.Word)
                   .WithMany()
                   .HasForeignKey(f => f.WordId);

               modelBuilder.Entity<History>()
                   .HasOne(h => h.Word)
                   .WithMany()
                   .HasForeignKey(h => h.WordId);
           }
       }
   }
   ```

4. **Configure EF Core in the API Project**  
   - In `PronunciationAssistant.Api/Program.cs` (for a minimal API) or `Startup.cs`:
     ```csharp
     builder.Services.AddDbContext<DatabaseContext>(options =>
         options.UseSqlite("Data Source=pronunciation.db"));
     ```

5. **Create EF Core Migration** (from the **API** project directory if that’s where the design-time context lives):
   ```bash
   cd ../PronunciationAssistant.Api
   dotnet ef migrations add InitialCreate -o Data/Migrations
   dotnet ef database update
   ```
   - Verify that a `pronunciation.db` file appears.

### 3. Verification & Git Commit
1. **Verification**:  
   - Check that the migration was created and the `pronunciation.db` file is generated.
   - Open the DB (using a tool like `DB Browser for SQLite`) to confirm tables and columns match the models.

2. **Testing**:  
   - (Optional) Write a quick test that instantiates the `DatabaseContext`, adds a `Word` record, saves changes, and queries it back.

3. **Git Commit**:
   ```bash
   git add .
   git commit -m "Implemented EF Core SQLite schema and initial migration"
   ```

---

## Chunk 3: Dictionary Service (Offline Dictionary Handling)

### 1. Objective
- Implement the **DictionaryService** that can look up word definitions from bundled offline sources (e.g., WordNet-based or local SQLite dictionary).

### 2. Steps

1. **Plan Dictionary Data Access**  
   - Decide on how offline dictionaries will be stored (e.g., in a separate SQLite DB or combined with the main DB in a special table).  
   - If it’s a separate resource, place it under `Resources/Dictionary` or a dedicated folder.

2. **Implement DictionaryService** Interface
   ```csharp
   public interface IDictionaryService
   {
       Task<string?> GetDefinitionAsync(string word);
       Task<string?> GetPartOfSpeechAsync(string word);
       // Additional methods as needed
   }
   ```
   - Store it in `PronunciationAssistant.Core/Interfaces/IDictionaryService.cs`.

3. **DictionaryService Implementation**
   ```csharp
   public class DictionaryService : IDictionaryService
   {
       // Suppose we store dictionary data in a separate local DB or a file
       public Task<string?> GetDefinitionAsync(string word)
       {
           // Pseudocode:
           // 1. Normalize the word (lowercase, trim).
           // 2. Look up in local resource (SQLite or file).
           // 3. Return definition string or null if not found.

           throw new NotImplementedException();
       }

       public Task<string?> GetPartOfSpeechAsync(string word)
       {
           // Similar approach to fetch part of speech.
           throw new NotImplementedException();
       }
   }
   ```

4. **Integrate the DictionaryService**
   - In `Program.cs` or `Startup.cs` of the API:
     ```csharp
     builder.Services.AddScoped<IDictionaryService, DictionaryService>();
     ```
   - In the **CLI** or other layers, you can also inject or instantiate as needed.

5. **Offline Data Example**  
   - If using WordNet data, create a process or script to parse WordNet files into a local table.  
   - For the sake of the minimal MVP, you might just store a small dictionary subset in SQLite or as JSON.

### 3. Verification & Git Commit
1. **Verification**:  
   - Ensure the `DictionaryService` compiles.
   - Confirm that your chosen offline dictionary resource is included in the project structure (even if not fully populated yet).

2. **Testing**:
   - Create a small test or console snippet that calls `DictionaryService.GetDefinitionAsync("test")` to see if it returns a placeholder or actual data.

3. **Git Commit**:
   ```bash
   git add .
   git commit -m "Added DictionaryService with placeholder offline lookup implementation"
   ```

---

## Chunk 4: PronunciationService (TTS Integration) and Audio Caching

### 1. Objective
- Implement local TTS capabilities for generating audio files.
- Cache audio files in the database or file system.

### 2. Steps

1. **Select TTS Engine**  
   - For cross-platform local TTS, consider using [eSpeak NG](https://github.com/espeak-ng/espeak-ng) or `.NET System.Speech` on Windows only.  
   - Alternatively, set up a placeholder TTS method if the actual engine is not ready.

2. **Create IPronunciationService Interface**
   ```csharp
   public interface IPronunciationService
   {
       Task<string?> GetOrGenerateAudioAsync(string word, string accent, bool slow);
   }
   ```

3. **PronunciationService Implementation**
   ```csharp
   public class PronunciationService : IPronunciationService
   {
       private readonly string _audioCachePath;

       public PronunciationService(IConfiguration configuration)
       {
           // e.g., from appsettings or environment variable
           _audioCachePath = configuration["AudioCachePath"] ?? "AudioCache";
           Directory.CreateDirectory(_audioCachePath);
       }

       public async Task<string?> GetOrGenerateAudioAsync(string word, string accent, bool slow)
       {
           // 1. Check if audio file exists in cache (e.g., {word}_{accent}_{slow}.ogg).
           // 2. If exists, return path.
           // 3. Otherwise:
           //      a) Use local TTS engine to generate audio file
           //      b) Convert to OGG Vorbis if needed
           //      c) Save file to _audioCachePath
           //      d) Return the file path
           throw new NotImplementedException();
       }
   }
   ```

4. **Audio Conversion (If Needed)**
   - Use [NAudio](https://github.com/naudio/NAudio) or a command-line tool like `ffmpeg` for format conversion.
   - For cross-platform usage, you might spawn a process that calls `ffmpeg`.

5. **Register PronunciationService**  
   - In `Program.cs` or `Startup.cs`:
     ```csharp
     builder.Services.AddScoped<IPronunciationService, PronunciationService>();
     ```

6. **AudioPath Storage in DB**  
   - Whenever you generate a new audio file, update the `Words` table’s `AudioPath` for that word (if appropriate).  
   - Alternatively, keep the audio path in variants if accent is different.

### 3. Verification & Git Commit
1. **Verification**:  
   - Ensure your service can generate an audio file for a test word (e.g., “hello”).
   - Confirm the file is saved in the designated cache directory.

2. **Testing**:
   - Write a quick integration test or console script that calls `GetOrGenerateAudioAsync("hello", "american", false)` and checks if a file is created.

3. **Git Commit**:
   ```bash
   git add .
   git commit -m "Implemented basic PronunciationService with local TTS and audio caching"
   ```

---

## Chunk 5: PhoneticService (IPA + Simplified)

### 1. Objective
- Implement generation or retrieval of phonetic data (IPA and simplified).
- Handle stress markers and fallback logic.

### 2. Steps

1. **Create IPhoneticService Interface**
   ```csharp
   public interface IPhoneticService
   {
       Task<(string ipa, string simplified)?> GetOrGeneratePhoneticsAsync(string word, string accent);
   }
   ```

2. **PhoneticService Implementation**
   ```csharp
   public class PhoneticService : IPhoneticService
   {
       public async Task<(string ipa, string simplified)?> GetOrGeneratePhoneticsAsync(string word, string accent)
       {
           // 1. Check if phonetics are in the DB for the word/accent.
           // 2. If not, try offline dictionary that has IPA.
           // 3. If still not found, generate via rule-based approach.
           // 4. Return a tuple: (ipa, simplified).

           throw new NotImplementedException();
       }
   }
   ```

3. **Rule-Based Fallback**  
   - Use a library (e.g., [Phonetisaurus](https://github.com/AdolfVonKleist/Phonetisaurus) or [eSpeak NG] for approximate IPA) or implement a simplified approach for demonstration.

4. **DB Integration**  
   - When found or generated, store the phonetics (`Phonetic`, `IpaPhonetic`) in the `Words` table or `WordVariants` if accent-based.

5. **Register PhoneticService**  
   ```csharp
   builder.Services.AddScoped<IPhoneticService, PhoneticService>();
   ```

### 3. Verification & Git Commit
1. **Verification**:
   - Confirm you can retrieve or generate phonetics for test words.
   - Check that the data is saved to the DB or returned properly.

2. **Testing**:
   - Create test calls that request phonetics for known words (“test” or “example”) and confirm results.

3. **Git Commit**:
   ```bash
   git add .
   git commit -m "Added PhoneticService with IPA and simplified fallback generation"
   ```

---

## Chunk 6: Integrating Dictionary & Phonetics into Word Lookup

### 1. Objective
- Create a central method to orchestrate dictionary definition lookup, phonetic generation, and audio generation for a given word.

### 2. Steps

1. **Create WordLookupOrchestrator (or use a ‘Core’ method)**
   ```csharp
   public class WordLookupOrchestrator
   {
       private readonly IDictionaryService _dictionaryService;
       private readonly IPhoneticService _phoneticService;
       private readonly IPronunciationService _pronunciationService;
       private readonly DatabaseContext _db;

       public WordLookupOrchestrator(
           IDictionaryService dictionaryService,
           IPhoneticService phoneticService,
           IPronunciationService pronunciationService,
           DatabaseContext db)
       {
           _dictionaryService = dictionaryService;
           _phoneticService = phoneticService;
           _pronunciationService = pronunciationService;
           _db = db;
       }

       public async Task<Word> GetWordDataAsync(string inputWord, string accent, bool slow = false)
       {
           // 1. Normalize inputWord.
           // 2. Check if word exists in DB (Words table).
           // 3. If found, use existing data; else create a new record.
           // 4. Look up definition if needed (DictionaryService).
           // 5. Look up/generate phonetics (PhoneticService).
           // 6. Generate audio (PronunciationService).
           // 7. Save changes to DB (definition, phonetics, audio path).
           // 8. Return the Word entity with updated info.
           throw new NotImplementedException();
       }
   }
   ```

2. **Register Orchestrator**  
   ```csharp
   builder.Services.AddScoped<WordLookupOrchestrator>();
   ```

3. **Refactor**  
   - Ensure the orchestrator calls your existing services in the correct order, stores results in the `Word` entity, and saves to the database.

### 3. Verification & Git Commit
1. **Verification**:
   - The orchestrator should coordinate everything and return a fully populated `Word` record (definition, phonetics, audio path).

2. **Testing**:
   - Use a test script in the CLI or a unit test to call `GetWordDataAsync("hello", "american")`.
   - Check the database row after completion.

3. **Git Commit**:
   ```bash
   git add .
   git commit -m "Implemented WordLookupOrchestrator for coordinated dictionary, phonetics, and TTS"
   ```

---

## Chunk 7: History, Favorites, and Settings

### 1. Objective
- Implement the **UserDataService** to handle:
  - Storing history entries
  - Managing favorites
  - Managing app-wide settings

### 2. Steps

1. **UserDataService Interface**
   ```csharp
   public interface IUserDataService
   {
       Task AddToHistoryAsync(int wordId, string accentUsed);
       Task<List<History>> GetHistoryAsync(int limit = 50);
       Task AddToFavoritesAsync(int wordId, string? notes, string? tags);
       Task<List<Favorite>> GetFavoritesAsync(string? tag = null);
       Task UpdateSettingAsync(string key, string value);
       Task<string?> GetSettingAsync(string key);
   }
   ```

2. **UserDataService Implementation**
   ```csharp
   public class UserDataService : IUserDataService
   {
       private readonly DatabaseContext _db;
       public UserDataService(DatabaseContext db)
       {
           _db = db;
       }

       public async Task AddToHistoryAsync(int wordId, string accentUsed)
       {
           var entry = new History
           {
               WordId = wordId,
               AccentUsed = accentUsed,
               Timestamp = DateTime.UtcNow
           };
           _db.History.Add(entry);
           await _db.SaveChangesAsync();
       }

       public async Task<List<History>> GetHistoryAsync(int limit = 50)
       {
           return await _db.History
               .OrderByDescending(h => h.Timestamp)
               .Take(limit)
               .ToListAsync();
       }

       public async Task AddToFavoritesAsync(int wordId, string? notes, string? tags)
       {
           var fav = new Favorite
           {
               WordId = wordId,
               Notes = notes,
               Tags = tags
           };
           _db.Favorites.Add(fav);
           await _db.SaveChangesAsync();
       }

       public async Task<List<Favorite>> GetFavoritesAsync(string? tag = null)
       {
           var query = _db.Favorites.Include(f => f.Word).AsQueryable();
           if (!string.IsNullOrEmpty(tag))
           {
               // naive filter approach:
               query = query.Where(f => f.Tags != null && f.Tags.Contains(tag));
           }
           return await query.ToListAsync();
       }

       public async Task UpdateSettingAsync(string key, string value)
       {
           var setting = await _db.Settings.FindAsync(key);
           if (setting == null)
           {
               setting = new Setting
               {
                   Key = key,
                   Value = value
               };
               _db.Settings.Add(setting);
           }
           else
           {
               setting.Value = value;
           }
           await _db.SaveChangesAsync();
       }

       public async Task<string?> GetSettingAsync(string key)
       {
           var setting = await _db.Settings.FindAsync(key);
           return setting?.Value;
       }
   }
   ```

3. **Register UserDataService**
   ```csharp
   builder.Services.AddScoped<IUserDataService, UserDataService>();
   ```

4. **Refine Orchestrator**  
   - Add code to store each successful lookup in `History`.

### 3. Verification & Git Commit
1. **Verification**:
   - `UserDataService` compiles and integrates with `DatabaseContext`.
   - Methods create and retrieve data in the correct tables.

2. **Testing**:
   - Write quick test calls to `AddToHistoryAsync`, `GetHistoryAsync`, `AddToFavoritesAsync`, etc., confirming data is stored and retrieved.

3. **Git Commit**:
   ```bash
   git add .
   git commit -m "Added UserDataService for history, favorites, and settings management"
   ```

---

## Chunk 8: RESTful API Endpoints

### 1. Objective
- Implement the API endpoints as defined in the design document:
  - `/api/pronunciation/{word}`
  - `/api/history`, `/api/favorites`, `/api/settings`, etc.

### 2. Steps

1. **PronunciationController** (if using controllers) or minimal API mappings. For example, using minimal APIs:
   ```csharp
   app.MapGet("/api/pronunciation/{word}", async (
       string word,
       [FromQuery] string? accent,
       [FromServices] WordLookupOrchestrator orchestrator,
       [FromServices] IUserDataService userDataService) =>
   {
       if (string.IsNullOrWhiteSpace(accent)) accent = "american";
       var result = await orchestrator.GetWordDataAsync(word, accent);

       // Log to history
       await userDataService.AddToHistoryAsync(result.Id, accent);

       return Results.Ok(result); // or map to a DTO
   });
   ```

2. **History Endpoints**  
   ```csharp
   app.MapGet("/api/history", async (
       [FromQuery] int page = 1,
       [FromQuery] int pageSize = 50,
       [FromServices] IUserDataService userDataService) =>
   {
       var history = await userDataService.GetHistoryAsync(pageSize);
       return Results.Ok(history);
   });
   ```

3. **Favorites Endpoints**  
   ```csharp
   app.MapPost("/api/favorites", async (
       [FromBody] FavoriteRequest request,
       [FromServices] IUserDataService userDataService) =>
   {
       await userDataService.AddToFavoritesAsync(request.WordId, request.Notes, request.Tags);
       return Results.Ok();
   });
   ```
   ```csharp
   // ... GET favorites, PUT (update) favorites, DELETE favorites, etc.
   ```

4. **Settings Endpoints**  
   ```csharp
   app.MapGet("/api/settings", async ([FromServices] IUserDataService userDataService) =>
   {
       // Return all settings or a subset
   });
   app.MapPut("/api/settings/{key}", async (string key, [FromBody] string value, [FromServices] IUserDataService userDataService) =>
   {
       await userDataService.UpdateSettingAsync(key, value);
       return Results.Ok();
   });
   ```

5. **Test via Postman or curl**  
   - Ensure endpoints behave as expected.

### 3. Verification & Git Commit
1. **Verification**:
   - The API runs (e.g., `dotnet run` in the **API** project).
   - Calls to `/api/pronunciation/{word}` return the expected data.

2. **Testing**:
   - Use Postman/curl to test endpoints. Verify:
     - A word that doesn’t exist triggers fallback logic.
     - Stored data (history, favorites, settings) is correct in the DB.

3. **Git Commit**:
   ```bash
   git add .
   git commit -m "Implemented RESTful API endpoints for pronunciation, history, favorites, and settings"
   ```

---

## Chunk 9: Command Line Interface

### 1. Objective
- Provide a fully functional CLI using `System.CommandLine`.

### 2. Steps

1. **Add System.CommandLine NuGet Package**
   ```bash
   cd ../PronunciationAssistant.Cli
   dotnet add package System.CommandLine
   ```

2. **CLI Program.cs (Skeleton)**
   ```csharp
   using System.CommandLine;
   using PronunciationAssistant.Core.Data;
   using Microsoft.EntityFrameworkCore;
   // ... other using statements

   var rootCommand = new RootCommand("Pronunciation Assistant CLI");

   var pronounceCommand = new Command("pronounce", "Pronounce a word")
   {
       new Argument<string>("word"),
       new Option<string>(new[] {"--accent", "-a"}, "Accent to use"),
       new Option<bool>(new[] {"--slow", "-s"}, "Play at slower speed")
   };

   pronounceCommand.SetHandler(async (string word, string accent, bool slow) =>
   {
       // Resolve services, run orchestrator, etc.
   },
   pronounceCommand.Arguments[0],
   pronounceCommand.Options[0],
   pronounceCommand.Options[1]);

   rootCommand.AddCommand(pronounceCommand);

   // Add other commands similarly
   return await rootCommand.InvokeAsync(args);
   ```

3. **Service Resolution**  
   - Typically, for console apps, you might build a small `Host` that registers your DI services (similar to the API).
   ```csharp
   var builder = Host.CreateDefaultBuilder(args)
       .ConfigureServices(services =>
       {
           services.AddDbContext<DatabaseContext>(options =>
               options.UseSqlite("Data Source=pronunciation.db"));

           // Register your orchestrator, dictionary, user data services, etc.
           services.AddScoped<WordLookupOrchestrator>();
           services.AddScoped<IDictionaryService, DictionaryService>();
           services.AddScoped<IPhoneticService, PhoneticService>();
           services.AddScoped<IPronunciationService, PronunciationService>();
           services.AddScoped<IUserDataService, UserDataService>();
       });
   var host = builder.Build();
   ```

4. **Implement Each CLI Command**  
   - `pronounce <word>`: Looks up and plays the audio (via a platform-specific player or TTS).
   - `phonetic <word>`: Returns phonetic data.
   - `define <word>`: Returns definition(s).
   - `history`, `favorites`, etc., to retrieve and manipulate user data.

5. **Color-Coded Output**  
   - Use `System.Console.ForegroundColor = ConsoleColor.Green;` etc., for highlighting.

### 3. Verification & Git Commit
1. **Verification**:
   - Run `dotnet run -- pronounce hello -a american` in the CLI project. Confirm it performs the lookup.

2. **Testing**:
   - Manually test each command.  
   - Optionally, build a small test suite using `dotnet test` with integration tests that run CLI commands.

3. **Git Commit**:
   ```bash
   git add .
   git commit -m "Added CLI project commands for pronunciation, definitions, history, and favorites"
   ```

---

## Chunk 10: Electron UI (React + Tailwind)

### 1. Objective
- Provide a graphical interface with React, packaged in Electron.

### 2. Steps

1. **Create Electron Project Structure**
   - From the root, create a folder `ElectronUI`.
   - Initialize Node environment:
     ```bash
     cd ElectronUI
     npm init -y
     npm install electron react react-dom tailwindcss axios
     ```
   - Set up scripts in `package.json`:
     ```json
     {
       "name": "electron-ui",
       "main": "public/main.js",
       "scripts": {
         "start": "electron public/main.js",
         "dev": "concurrently \"npm run react-dev\" \"npm run start\"",
         "react-dev": "react-scripts start",
         "build": "react-scripts build"
       }
       // ...
     }
     ```

2. **Initialize Tailwind**
   ```bash
   npx tailwindcss init
   ```
   - Configure `tailwind.config.js` to purge unused CSS in production.

3. **React App**  
   - Create `src/index.jsx` with a basic React structure.
   - Create components: `SearchBar.jsx`, `WordResult.jsx`, `History.jsx`, `Favorites.jsx`, etc.

4. **Electron Main Process** (`public/main.js` example)
   ```js
   const { app, BrowserWindow } = require('electron');
   function createWindow() {
     const win = new BrowserWindow({
       width: 800,
       height: 600,
       webPreferences: {
         nodeIntegration: false
       }
     });
     win.loadURL('http://localhost:3000'); // dev server or file path in production
   }

   app.whenReady().then(() => {
     createWindow();
     app.on('activate', () => {
       if (BrowserWindow.getAllWindows().length === 0) createWindow();
     });
   });

   app.on('window-all-closed', () => {
     if (process.platform !== 'darwin') app.quit();
   });
   ```

5. **API Integration (Axios)**
   - Create a helper in `src/api.js`:
     ```js
     import axios from 'axios';

     const apiClient = axios.create({
       baseURL: 'http://localhost:5000/api', // or wherever your API is
     });

     export default apiClient;
     ```
   - In React components, call endpoints (e.g., `apiClient.get(`/pronunciation/${word}`)`).

6. **Basic UI Flows**  
   - **Search**: Input a word, call `GET /pronunciation/{word}`, display results.
   - **History**: Call `GET /api/history`, show in a table.
   - **Favorites**: `POST /api/favorites`, `GET /api/favorites`, etc.
   - **Settings**: Provide a form that calls `PUT /api/settings/{key}`.

### 3. Verification & Git Commit
1. **Verification**:
   - Run the Electron app in dev mode (`npm run dev` if you set up concurrency).
   - Ensure you can look up a word, see results, and play audio (if integrated).

2. **Testing**:
   - Manual UI testing.  
   - Possibly add [React Testing Library](https://testing-library.com/docs/react-testing-library/intro/) for component tests.

3. **Git Commit**:
   ```bash
   git add .
   git commit -m "Implemented Electron UI with React, Tailwind, and basic API integration"
   ```

---

## Chunk 11: Cross-Platform Testing & Raspberry Pi Optimization

### 1. Objective
- Test on Windows, macOS, Linux, and Raspberry Pi.
- Optimize memory, CPU usage, and startup times.

### 2. Steps

1. **Cross-Platform Builds**
   - **Windows**: `dotnet publish -c Release -r win-x64`
   - **macOS**: `dotnet publish -c Release -r osx-x64`
   - **Linux**: `dotnet publish -c Release -r linux-x64`
   - Use `-r linux-arm` or `linux-arm64` for Raspberry Pi.

2. **Performance Profiling**
   - Check memory usage with `dotnet-counters` or `dotnet-trace`.
   - On Raspberry Pi, monitor CPU usage with `top` or `htop`.

3. **Optimizations**
   - Minimize memory usage by:
     - Using the “Trim unused assemblies” feature (`PublishTrimmed=true`).
     - Reducing concurrency where possible.
   - Evaluate TTS engine overhead. Possibly switch to a simpler TTS on Pi if memory is high.

### 3. Verification & Git Commit
1. **Verification**:
   - Confirm the app runs on each OS with expected performance.
   - Measure startup times and memory usage. Compare with design targets.

2. **Testing**:
   - Perform quick manual tests to confirm offline dictionary access, TTS, and UI rendering.

3. **Git Commit**:
   ```bash
   git add .
   git commit -m "Cross-platform build configurations and Raspberry Pi optimizations"
   ```

---

## Chunk 12: Packaging and Deployment

### 1. Objective
- Create installers or self-contained packages for Windows, macOS, Linux, and Raspberry Pi.

### 2. Steps

1. **Windows Packaging**  
   - Create an MSI or use a self-contained folder + script.
   - Tools: [WiX Toolset](https://wixtoolset.org/) or [MSIX Packaging](https://docs.microsoft.com/en-us/windows/msix/overview).

2. **macOS Packaging**  
   - Generate `.app` folder or `.dmg` with a script.
   - Sign/notarize if distributing externally.

3. **Linux Packaging**  
   - Provide `.deb` or `.rpm`, or use an **AppImage** approach.
   - Alternatively, a `tar.gz` with a run script.

4. **Raspberry Pi**  
   - Provide a `.tar.gz` build for `linux-arm` or a custom image if needed.

5. **Electron Packaging**  
   - Use [electron-builder](https://www.electron.build/) to generate cross-platform installers for the UI.

### 3. Verification & Git Commit
1. **Verification**:
   - Test each package/installer on the respective platforms.

2. **Testing**:
   - Install on a clean system. Verify offline usage.

3. **Git Commit**:
   ```bash
   git add .
   git commit -m "Added packaging scripts and cross-platform deployment artifacts"
   ```

---

## Chunk 13: Final QA, Bug Fixes, and Documentation

### 1. Objective
- Ensure the solution is production-ready with final polish.

### 2. Steps

1. **Documentation**  
   - Write or update `README.md` with setup, usage, and troubleshooting info.
   - Add or refine in-line code documentation and XML doc comments.

2. **Final Bug Fixes**
   - Triage any outstanding issues.
   - Apply final UI/UX tweaks.

3. **Licensing and Attribution**
   - Confirm each third-party library/dictionary license is included.
   - Provide credit where required.

### 3. Verification & Git Commit
1. **Verification**:
   - Ensure all user stories are satisfied.
   - Confirm licensing compliance.

2. **Testing**:
   - Perform a full regression pass (manual or automated).
   - Optional user acceptance testing if working with end-users.

3. **Git Commit**:
   ```bash
   git add .
   git commit -m "Final QA pass, documentation updates, and license attributions"
   ```

---

# Conclusion

By following these sequential “chunks,” you systematically build out each component of the **Pronunciation Assistant** application—starting with basic project structure and ending with a fully cross-platform, packaged product. Each chunk concludes with a clear step to verify functionality, run tests, and commit your changes to Git, ensuring both **focus** and **traceability** throughout the development process. 

This approach will help keep the AI (and any human teammates) on task, prevent drifting scope, and result in a maintainable, production-ready solution that meets all the requirements defined in the original design document. Good luck with your implementation!