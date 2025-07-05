using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AkkaWordcounter2.App.Config;

public class WordCounterSettings
{
    public static readonly string Section = "WordCounter";
    public string[] DocumentUris { get; set; } = [];
}

public sealed class WordCounterSettingsValidator : IValidateOptions<WordCounterSettings>
{
    public ValidateOptionsResult Validate(string? name, WordCounterSettings options)
    {
        var errors = new List<string>();
        if (options.DocumentUris.Length == 0)
        {
            errors.Add("DocumentUris must not be empty");
        }

        if (options.DocumentUris.Any(uri => !Uri.IsWellFormedUriString(uri, UriKind.Absolute)))
        {
            errors.Add("DocumentUris must contain only valid absolute URIs");
        }
        return errors.Count == 0
            ? ValidateOptionsResult.Success 
            : ValidateOptionsResult.Fail(errors);
    }
}

public static class WordCounterSettingsExtenstions
{
    public static IServiceCollection AddWordCounterSettings(this IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<WordCounterSettings>, WordCounterSettingsValidator>();
        services.AddOptionsWithValidateOnStart<WordCounterSettings>()
            .BindConfiguration(WordCounterSettings.Section);
        
        return services;
    }
}