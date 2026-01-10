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
    /// Most recently updated ADRs.
    /// </summary>
    public List<Adr> RecentAdrs => Adrs
        .Where(a => a.Date.HasValue)
        .OrderByDescending(a => a.Date)
        .Take(10)
        .ToList();
}
