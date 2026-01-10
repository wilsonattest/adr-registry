using AdrRegistry.Generator.Models;
using AdrRegistry.Generator.Services;

namespace AdrRegistry.Generator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            Console.WriteLine("ADR Registry Generator");
            Console.WriteLine("======================\n");

            // Parse command line arguments
            var localMode = args.Contains("--local") || args.Contains("-l");
            var localPath = GetArgValue(args, "--path") ?? GetArgValue(args, "-p");

            // Load configuration
            var configLoader = new ConfigurationLoader();
            var config = configLoader.LoadGeneratorConfig();

            // Command line overrides
            if (localMode)
            {
                config.LocalMode = true;
            }
            if (!string.IsNullOrEmpty(localPath))
            {
                config.LocalPath = localPath;
            }

            // Also check environment variable for local path
            var envLocalPath = Environment.GetEnvironmentVariable("ADR_LOCAL_PATH");
            if (!string.IsNullOrEmpty(envLocalPath))
            {
                config.LocalMode = true;
                config.LocalPath = envLocalPath;
            }

            Console.WriteLine($"Mode: {(config.LocalMode ? "Local Filesystem" : "GitHub API")}");
            Console.WriteLine($"Organization: {config.Organization}");
            Console.WriteLine($"ADR Path: {config.AdrPath}");
            Console.WriteLine($"Output Path: {config.OutputPath}");

            if (config.LocalMode)
            {
                Console.WriteLine($"Local Path: {config.LocalPath}");
            }
            Console.WriteLine();

            // Build ADR index based on mode
            AdrIndex index;

            if (config.LocalMode)
            {
                // Local filesystem mode
                if (string.IsNullOrEmpty(config.LocalPath))
                {
                    Console.Error.WriteLine("Error: LocalPath is required when using local mode.");
                    Console.Error.WriteLine("Set it in config, via --path argument, or ADR_LOCAL_PATH environment variable.");
                    return 1;
                }

                if (!Directory.Exists(config.LocalPath))
                {
                    Console.Error.WriteLine($"Error: Local path does not exist: {config.LocalPath}");
                    return 1;
                }

                Console.WriteLine("Scanning local filesystem...\n");
                var localService = new LocalFileSystemService(config, config.LocalPath);
                index = await localService.BuildIndexAsync();
            }
            else
            {
                // GitHub API mode
                var gitHubAppConfig = configLoader.LoadGitHubAppConfig();

                Console.WriteLine("Authenticating with GitHub App...");
                var authenticator = new GitHubAppAuthenticator(
                    gitHubAppConfig.AppId,
                    gitHubAppConfig.PrivateKeyPath);

                var client = await authenticator.CreateClientAsync(gitHubAppConfig.InstallationId);
                Console.WriteLine("Authentication successful!\n");

                var gitHubService = new GitHubService(client, config);
                index = await gitHubService.BuildIndexAsync();
            }

            // Generate static site
            var siteGenerator = new SiteGenerator(config);
            await siteGenerator.GenerateAsync(index);

            Console.WriteLine($"\nDone! Run 'npx pagefind --site {config.OutputPath}' to build the search index.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"\nError: {ex.Message}");
            if (Environment.GetEnvironmentVariable("DEBUG") == "1")
            {
                Console.Error.WriteLine(ex.StackTrace);
            }
            return 1;
        }
    }

    static string? GetArgValue(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }
        return null;
    }

    static void PrintUsage()
    {
        Console.WriteLine(@"
Usage: AdrRegistry.Generator [options]

Options:
  --local, -l          Use local filesystem mode instead of GitHub API
  --path, -p <path>    Path to directory containing repository folders
  --help, -h           Show this help message

Environment Variables:
  ADR_LOCAL_PATH       Path for local mode (alternative to --path)
  GITHUB_APP_ID        GitHub App ID (required for GitHub mode)
  GITHUB_APP_INSTALLATION_ID  GitHub App Installation ID
  GITHUB_APP_PRIVATE_KEY_PATH Path to private key PEM file

Examples:
  # Local mode with test data
  AdrRegistry.Generator --local --path ./test-repos

  # GitHub mode (requires env vars)
  AdrRegistry.Generator
");
    }
}
