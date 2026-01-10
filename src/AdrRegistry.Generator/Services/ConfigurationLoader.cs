using Microsoft.Extensions.Configuration;
using AdrRegistry.Generator.Models;

namespace AdrRegistry.Generator.Services;

/// <summary>
/// Loads configuration from JSON files and environment variables.
/// </summary>
public class ConfigurationLoader
{
    private readonly IConfiguration _configuration;

    public ConfigurationLoader()
    {
        // Check for custom config file via environment variable
        var configFile = Environment.GetEnvironmentVariable("ADR_CONFIG_FILE")
            ?? "config/repositories.json";

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory());

        // Add the config file if it exists
        if (File.Exists(configFile))
        {
            builder.AddJsonFile(configFile, optional: false);
        }
        else
        {
            // Fall back to default
            builder.AddJsonFile("config/repositories.json", optional: true);
        }

        builder.AddEnvironmentVariables();

        _configuration = builder.Build();
    }

    public ConfigurationLoader(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ConfigurationLoader(string configFilePath)
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(configFilePath, optional: false)
            .AddEnvironmentVariables()
            .Build();
    }

    /// <summary>
    /// Loads the generator configuration.
    /// </summary>
    public GeneratorConfig LoadGeneratorConfig()
    {
        var config = new GeneratorConfig();
        _configuration.Bind(config);
        return config;
    }

    /// <summary>
    /// Loads the GitHub App configuration from environment variables.
    /// </summary>
    public GitHubAppConfig LoadGitHubAppConfig()
    {
        var appIdStr = Environment.GetEnvironmentVariable("GITHUB_APP_ID");
        var installationIdStr = Environment.GetEnvironmentVariable("GITHUB_APP_INSTALLATION_ID");
        var privateKeyPath = Environment.GetEnvironmentVariable("GITHUB_APP_PRIVATE_KEY_PATH");

        if (string.IsNullOrEmpty(appIdStr))
            throw new InvalidOperationException("GITHUB_APP_ID environment variable is required");

        if (string.IsNullOrEmpty(installationIdStr))
            throw new InvalidOperationException("GITHUB_APP_INSTALLATION_ID environment variable is required");

        if (string.IsNullOrEmpty(privateKeyPath))
            throw new InvalidOperationException("GITHUB_APP_PRIVATE_KEY_PATH environment variable is required");

        if (!int.TryParse(appIdStr, out var appId))
            throw new InvalidOperationException("GITHUB_APP_ID must be a valid integer");

        if (!long.TryParse(installationIdStr, out var installationId))
            throw new InvalidOperationException("GITHUB_APP_INSTALLATION_ID must be a valid integer");

        return new GitHubAppConfig
        {
            AppId = appId,
            InstallationId = installationId,
            PrivateKeyPath = privateKeyPath
        };
    }
}
