namespace AdrRegistry.Generator.Models;

/// <summary>
/// Represents information about a pull request that contains ADR changes.
/// </summary>
public class PullRequestInfo
{
    /// <summary>
    /// The pull request number.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// The title of the pull request.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The URL to the pull request on GitHub.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The GitHub username of the PR author.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// The source branch of the pull request.
    /// </summary>
    public string SourceBranch { get; set; } = string.Empty;

    /// <summary>
    /// The repository full name (org/repo).
    /// </summary>
    public string RepositoryFullName { get; set; } = string.Empty;

    /// <summary>
    /// List of ADR file paths modified in this PR.
    /// </summary>
    public List<string> AdrFiles { get; set; } = new();
}
