using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WordWhisperer.Core.Interfaces;
using WordWhisperer.Core.Models;

namespace WordWhisperer.Core.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Word Whisperer core services to the service collection
    /// </summary>
    public static IServiceCollection AddWordWhispererServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration
        services.Configure<PhoneticServiceConfig>(configuration.GetSection("PhoneticService"));
        
        // Register services
        services.AddSingleton<PhoneticDictionaryService>();
        services.AddSingleton<MLPhoneticService>();
        services.AddScoped<IPhoneticService, PhoneticService>();
        services.AddScoped<IPronunciationService, PronunciationService>();
        services.AddScoped<IDictionaryService, DictionaryService>();
        services.AddScoped<IUserDataService, UserDataService>();
        
        return services;
    }
    
    /// <summary>
    /// Initializes all required services
    /// </summary>
    public static async Task InitializeServicesAsync(this IServiceProvider serviceProvider)
    {
        // Initialize dictionary service
        var dictionaryService = serviceProvider.GetRequiredService<PhoneticDictionaryService>();
        await dictionaryService.InitializeAsync();
        
        // Initialize ML phonetic service
        var mlPhoneticService = serviceProvider.GetRequiredService<MLPhoneticService>();
        await mlPhoneticService.InitializeAsync();
    }
}
