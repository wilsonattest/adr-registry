using System.Text;
using System.Text.RegularExpressions;
using Octokit;
using AdrRegistry.Generator.Models;
using Repository = AdrRegistry.Generator.Models.Repository;

namespace AdrRegistry.Generator.Services;

/// <summary>
/// Service for interacting with GitHub to fetch repositories and ADRs.
/// </summary>
public class GitHubService
{
    private readonly GitHubClient _client;
    private readonly GeneratorConfig _config;
    private readonly AdrParser _parser;

    public GitHubService(GitHubClient client, GeneratorConfig config)
    {
        _client = client;
        _config = config;
        _parser = new AdrParser();
    }

    /// <summary>
    /// Gets all repositories in the organization that should be scanned.
    /// </summary>
    public async Task<List<Repository>> GetRepositoriesAsync(CancellationToken ct = default)
    {
        Console.WriteLine($"Fetching repositories from organization: {_config.Organization}");

        var allRepos = await _client.Repository.GetAllForOrg(_config.Organization);

        var repositories = allRepos
            .Where(r => !r.Archived)
            .Where(r => !IsExcluded(r.FullName))
            .Where(r => _config.DiscoverAll || IsIncluded(r.FullName))
            .Select(r => new Repository
            {
                Name = r.Name,
                FullName = r.FullName,
                DefaultBranch = r.DefaultBranch ?? "main"
            })
            .ToList();

        Console.WriteLine($"Found {repositories.Count} repositories to scan");
        return repositories;
    }

    /// <summary>
    /// Gets all ADRs from a repository.
    /// </summary>
    public async Task<List<Adr>> GetAdrsForRepositoryAsync(Repository repo, CancellationToken ct = default)
    {
        var adrs = new List<Adr>();

        try
        {
            Console.WriteLine($"  Scanning {repo.FullName}...");

            var contents = await _client.Repository.Content.GetAllContents(
                repo.FullName.Split('/')[0],
                repo.FullName.Split('/')[1],
                _config.AdrPath);

            var mdFiles = contents
                .Where(c => c.Type == ContentType.File)
                .Where(c => c.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                .Where(c => !c.Name.Equals("0000-template.md", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Console.WriteLine($"    Found {mdFiles.Count} ADR files");

            foreach (var file in mdFiles)
            {
                try
                {
                    var contentBytes = await _client.Repository.Content.GetRawContent(
                        repo.FullName.Split('/')[0],
                        repo.FullName.Split('/')[1],
                        file.Path);

                    var markdown = Encoding.UTF8.GetString(contentBytes);
                    var adr = _parser.Parse(markdown, repo, file.Path, file.Name, file.HtmlUrl);
                    adrs.Add(adr);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    Warning: Failed to parse {file.Name}: {ex.Message}");
                }
            }
        }
        catch (NotFoundException)
        {
            // Repository doesn't have the ADR directory - this is fine
            Console.WriteLine($"    No ADR directory found");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    Error scanning repository: {ex.Message}");
        }

        return adrs;
    }

    /// <summary>
    /// Builds the complete ADR index by scanning all repositories.
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

            // Small delay to avoid rate limiting
            await Task.Delay(100, ct);
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
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
    }
}
