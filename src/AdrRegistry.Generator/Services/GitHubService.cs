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

        // Fetch ADRs from open pull requests
        Console.WriteLine("\nScanning for ADRs in open pull requests...");
        foreach (var repo in repositories)
        {
            try
            {
                var prAdrs = await GetAdrsFromPullRequestsAsync(repo, ct);
                if (prAdrs.Count > 0)
                {
                    index.Adrs.AddRange(prAdrs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Warning: Failed to scan PRs for {repo.FullName}: {ex.Message}");
            }

            await Task.Delay(100, ct);
        }

        Console.WriteLine($"\nTotal: {index.MergedAdrCount} merged ADRs, {index.ProposedAdrCount} proposed ADRs across {index.RepositoryCount} repositories");
        return index;
    }

    /// <summary>
    /// Gets open pull requests that contain ADR changes.
    /// </summary>
    public async Task<List<PullRequestInfo>> GetOpenPullRequestsWithAdrsAsync(Repository repo, CancellationToken ct = default)
    {
        var result = new List<PullRequestInfo>();

        try
        {
            var owner = repo.FullName.Split('/')[0];
            var repoName = repo.FullName.Split('/')[1];

            var prs = await _client.PullRequest.GetAllForRepository(
                owner,
                repoName,
                new PullRequestRequest { State = ItemStateFilter.Open });

            foreach (var pr in prs)
            {
                try
                {
                    var files = await _client.PullRequest.Files(owner, repoName, pr.Number);

                    var adrFiles = files
                        .Where(f => f.FileName.StartsWith(_config.AdrPath, StringComparison.OrdinalIgnoreCase))
                        .Where(f => f.FileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                        .Where(f => !f.FileName.EndsWith("0000-template.md", StringComparison.OrdinalIgnoreCase))
                        .Where(f => f.Status != "removed")
                        .Select(f => f.FileName)
                        .ToList();

                    if (adrFiles.Count > 0)
                    {
                        result.Add(new PullRequestInfo
                        {
                            Number = pr.Number,
                            Title = pr.Title,
                            Url = pr.HtmlUrl,
                            Author = pr.User?.Login ?? "unknown",
                            SourceBranch = pr.Head.Ref,
                            RepositoryFullName = repo.FullName,
                            AdrFiles = adrFiles
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    Warning: Failed to get files for PR #{pr.Number}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Failed to fetch PRs for {repo.FullName}: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Gets ADR content from a pull request branch.
    /// </summary>
    public async Task<Adr?> GetAdrFromPullRequestAsync(Repository repo, PullRequestInfo pr, string filePath, CancellationToken ct = default)
    {
        try
        {
            var owner = repo.FullName.Split('/')[0];
            var repoName = repo.FullName.Split('/')[1];

            var contentBytes = await _client.Repository.Content.GetRawContentByRef(
                owner,
                repoName,
                filePath,
                pr.SourceBranch);

            var markdown = Encoding.UTF8.GetString(contentBytes);
            var fileName = Path.GetFileName(filePath);
            var githubUrl = $"https://github.com/{repo.FullName}/blob/{pr.SourceBranch}/{filePath}";

            var adr = _parser.Parse(markdown, repo, filePath, fileName, githubUrl);

            // Mark as PR ADR and add PR metadata
            adr.IsFromPullRequest = true;
            adr.PullRequestNumber = pr.Number;
            adr.PullRequestTitle = pr.Title;
            adr.PullRequestUrl = pr.Url;
            adr.PullRequestAuthor = pr.Author;
            adr.SourceBranch = pr.SourceBranch;

            // Append PR info to ID to avoid conflicts with merged ADRs
            adr.Id = $"{adr.Id}_pr{pr.Number}";

            return adr;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    Warning: Failed to fetch ADR from PR #{pr.Number}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets all ADRs from open pull requests for a repository.
    /// </summary>
    public async Task<List<Adr>> GetAdrsFromPullRequestsAsync(Repository repo, CancellationToken ct = default)
    {
        var adrs = new List<Adr>();

        var prs = await GetOpenPullRequestsWithAdrsAsync(repo, ct);

        foreach (var pr in prs)
        {
            Console.WriteLine($"  PR #{pr.Number} in {repo.Name}: {pr.Title}");

            foreach (var filePath in pr.AdrFiles)
            {
                var adr = await GetAdrFromPullRequestAsync(repo, pr, filePath, ct);
                if (adr != null)
                {
                    adrs.Add(adr);
                    Console.WriteLine($"    Found: {adr.Title}");
                }
            }
        }

        return adrs;
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
