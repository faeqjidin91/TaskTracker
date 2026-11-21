namespace TaskTracker.Api.Extensions;

public static class EnvFileExtensions
{
    private const string EnvFileName = ".env";
    private const string SolutionFileName = "TaskTracker.sln";
    private const string SeedPrefix = "Seed__";
    private const int MaxParentLevels = 4;

    // Loads only Seed__ keys, so .env cannot silently change any other setting.
    // Real environment variables win, which lets a host override the file.
    public static ConfigurationManager AddSeedSettingsFromEnvFile(
        this ConfigurationManager configuration, IHostEnvironment environment)
    {
        var path = FindEnvFile(environment);
        if (path is null)
        {
            return configuration;
        }

        var settings = File.ReadLines(path)
            .Select(ParseLine)
            .OfType<KeyValuePair<string, string>>()
            .Where(setting => setting.Key.StartsWith(SeedPrefix, StringComparison.Ordinal))
            .Where(setting => Environment.GetEnvironmentVariable(setting.Key) is null)
            .Select(setting => new KeyValuePair<string, string?>(setting.Key.Replace("__", ":"), setting.Value));

        configuration.AddInMemoryCollection(settings);
        return configuration;
    }

    // Deployments read only the app folder. Development also checks up to the solution root.
    private static string? FindEnvFile(IHostEnvironment environment)
    {
        var directory = new DirectoryInfo(environment.ContentRootPath);
        var levels = environment.IsDevelopment() ? MaxParentLevels : 0;

        for (var level = 0; directory is not null && level <= levels; level++)
        {
            var candidate = Path.Combine(directory.FullName, EnvFileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return null;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static KeyValuePair<string, string>? ParseLine(string line)
    {
        var text = line.Trim();
        if (text.Length == 0 || text[0] == '#')
        {
            return null;
        }

        if (text.StartsWith("export ", StringComparison.Ordinal))
        {
            text = text["export ".Length..].TrimStart();
        }

        var separator = text.IndexOf('=');
        if (separator <= 0)
        {
            return null;
        }

        var key = text[..separator].Trim();
        var value = text[(separator + 1)..].Trim();

        if (value.Length >= 2 && value[0] is '"' or '\'')
        {
            var closing = value.IndexOf(value[0], 1);
            if (closing > 0)
            {
                value = value[1..closing];
            }
        }
        else
        {
            var comment = value.IndexOf(" #", StringComparison.Ordinal);
            if (comment >= 0)
            {
                value = value[..comment].TrimEnd();
            }
        }

        return new KeyValuePair<string, string>(key, value);
    }
}
