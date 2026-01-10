namespace AdrRegistry.Generator.Models;

/// <summary>
/// Represents a GitHub repository containing ADRs.
/// </summary>
public class Repository
{
    /// <summary>
    /// The short name of the repository.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The full name of the repository (org/repo).
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// The default branch of the repository.
    /// </summary>
    public string DefaultBranch { get; set; } = "main";

    /// <summary>
    /// The URL to the repository on GitHub.
    /// </summary>
    public string GitHubUrl => $"https://github.com/{FullName}";

    /// <summary>
    /// The ADRs found in this repository.
    /// </summary>
    public List<Adr> Adrs { get; set; } = new();
}
