using AdrRegistry.Generator.Models;

namespace AdrRegistry.Generator.Services;

/// <summary>
/// Service for reading ADRs from the local filesystem (for testing without GitHub).
/// </summary>
public class LocalFileSystemService
{
    private readonly GeneratorConfig _config;
    private readonly AdrParser _parser;
    private readonly string _basePath;

    public LocalFileSystemService(GeneratorConfig config, string basePath)
    {
        _config = config;
        _basePath = basePath;
        _parser = new AdrParser();
    }

    /// <summary>
    /// Gets all repositories from the local filesystem.
    /// </summary>
    public Task<List<Repository>> GetRepositoriesAsync(CancellationToken ct = default)
    {
        Console.WriteLine($"Scanning local directory: {_basePath}");

        var repositories = new List<Repository>();

        if (!Directory.Exists(_basePath))
        {
            Console.WriteLine($"Directory not found: {_basePath}");
            return Task.FromResult(repositories);
        }

        foreach (var repoDir in Directory.GetDirectories(_basePath))
        {
            var repoName = Path.GetFileName(repoDir);
            var fullName = $"{_config.Organization}/{repoName}";

            // Check exclusion/inclusion rules
            if (IsExcluded(fullName))
            {
                Console.WriteLine($"  Skipping {repoName} (excluded)");
                continue;
            }

            if (!_config.DiscoverAll && !IsIncluded(fullName))
            {
                Console.WriteLine($"  Skipping {repoName} (not in include list)");
                continue;
            }

            var adrPath = Path.Combine(repoDir, _config.AdrPath.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (Directory.Exists(adrPath))
            {
                repositories.Add(new Repository
                {
                    Name = repoName,
                    FullName = fullName,
                    DefaultBranch = "main"
                });
            }
        }

        Console.WriteLine($"Found {repositories.Count} repositories with ADR directories");
        return Task.FromResult(repositories);
    }

    /// <summary>
    /// Gets all ADRs from a local repository directory.
    /// </summary>
    public Task<List<Adr>> GetAdrsForRepositoryAsync(Repository repo, CancellationToken ct = default)
    {
        var adrs = new List<Adr>();
        var repoPath = Path.Combine(_basePath, repo.Name);
        var adrPath = Path.Combine(repoPath, _config.AdrPath.Replace("/", Path.DirectorySeparatorChar.ToString()));

        Console.WriteLine($"  Scanning {repo.Name}...");

        if (!Directory.Exists(adrPath))
        {
            Console.WriteLine($"    No ADR directory found");
            return Task.FromResult(adrs);
        }

        var mdFiles = Directory.GetFiles(adrPath, "*.md")
            .Where(f => !Path.GetFileName(f).Equals("0000-template.md", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Console.WriteLine($"    Found {mdFiles.Count} ADR files");

        foreach (var file in mdFiles)
        {
            try
            {
                var fileName = Path.GetFileName(file);
                var relativePath = Path.Combine(_config.AdrPath, fileName).Replace("\\", "/");
                var gitHubUrl = $"https://github.com/{repo.FullName}/blob/main/{relativePath}";

                var markdown = File.ReadAllText(file);
                var adr = _parser.Parse(markdown, repo, relativePath, fileName, gitHubUrl);
                adrs.Add(adr);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Warning: Failed to parse {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        return Task.FromResult(adrs);
    }

    /// <summary>
    /// Builds the complete ADR index by scanning all local repositories.
    /// </summary>
    public async Task<AdrIndex> BuildIndexAsync(CancellationToken ct = default)
    {
        var index = new AdrIndex();
        var repositories = await GetRepositoriesAsync(ct);

        foreach (var repo in repositories)
        {
            var adrs = await GetAdrsForRepositoryAsync(repo, ct);

            if (adrs.Count > 0)
            {
                repo.Adrs = adrs;
                index.Repositories.Add(repo);
                index.Adrs.AddRange(adrs);
            }
        }

        Console.WriteLine($"\nTotal: {index.TotalAdrCount} ADRs across {index.RepositoryCount} repositories");
        return index;
    }

    private bool IsExcluded(string repoFullName)
    {
        foreach (var pattern in _config.Exclude)
        {
            if (MatchesWildcard(repoFullName, pattern))
                return true;
        }
        return false;
    }

    private bool IsIncluded(string repoFullName)
    {
        if (_config.Include.Count == 0)
            return true;

        foreach (var pattern in _config.Include)
        {
            if (MatchesWildcard(repoFullName, pattern))
                return true;
        }
        return false;
    }

    private static bool MatchesWildcard(string input, string pattern)
    {
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(input, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
