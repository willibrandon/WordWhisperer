using Microsoft.EntityFrameworkCore;
using WordWhisperer.Core.Data;
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
builder.Services.AddScoped<IPhoneticService, PhoneticService>();
builder.Services.AddScoped<IDictionaryService, DictionaryService>();

// Register the PhoneticDictionaryService as a singleton since it maintains dictionary state
builder.Services.AddSingleton<PhoneticDictionaryService>();

var app = builder.Build();

// Initialize the phonetic dictionary
using (var scope = app.Services.CreateScope())
{
    var dictionaryService = scope.ServiceProvider.GetRequiredService<PhoneticDictionaryService>();
    await dictionaryService.InitializeAsync();
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

app.Run();
