namespace AdrRegistry.Generator.Models;

/// <summary>
/// Represents the complete index of all ADRs across all repositories.
/// </summary>
public class AdrIndex
{
    /// <summary>
    /// All repositories that were scanned.
    /// </summary>
    public List<Repository> Repositories { get; set; } = new();

    /// <summary>
    /// All ADRs across all repositories.
    /// </summary>
    public List<Adr> Adrs { get; set; } = new();

    /// <summary>
    /// When the index was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total number of ADRs.
    /// </summary>
    public int TotalAdrCount => Adrs.Count;

    /// <summary>
    /// Total number of repositories with ADRs.
    /// </summary>
    public int RepositoryCount => Repositories.Count(r => r.Adrs.Count > 0);

    /// <summary>
    /// ADRs grouped by status.
    /// </summary>
    public Dictionary<string, int> AdrsByStatus => Adrs
        .GroupBy(a => a.Status)
        .ToDictionary(g => g.Key, g => g.Count());

    /// <summary>
    /// Most recently updated ADRs (merged only).
    /// </summary>
    public List<Adr> RecentAdrs => Adrs
        .Where(a => a.Date.HasValue && !a.IsFromPullRequest)
        .OrderByDescending(a => a.Date)
        .Take(10)
        .ToList();

    /// <summary>
    /// ADRs from open pull requests.
    /// </summary>
    public List<Adr> ProposedAdrs => Adrs
        .Where(a => a.IsFromPullRequest)
        .OrderByDescending(a => a.PullRequestNumber)
        .ToList();

    /// <summary>
    /// ADRs that have been merged (not from PRs).
    /// </summary>
    public List<Adr> MergedAdrs => Adrs
        .Where(a => !a.IsFromPullRequest)
        .ToList();

    /// <summary>
    /// Count of proposed ADRs in PRs.
    /// </summary>
    public int ProposedAdrCount => ProposedAdrs.Count;

    /// <summary>
    /// Count of merged ADRs.
    /// </summary>
    public int MergedAdrCount => MergedAdrs.Count;
}
