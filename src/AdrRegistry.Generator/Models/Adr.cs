namespace AdrRegistry.Generator.Models;

/// <summary>
/// Represents an Architecture Decision Record.
/// </summary>
public class Adr
{
    /// <summary>
    /// Unique identifier in the format "{repo}_{number}".
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The ADR number (e.g., "0001").
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// The title of the ADR.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The status of the ADR (Proposed, Accepted, Deprecated, Superseded).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// The date the decision was made.
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// List of people involved in the decision.
    /// </summary>
    public List<string> Deciders { get; set; } = new();

    /// <summary>
    /// The context section describing the forces at play.
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// The decision that was made.
    /// </summary>
    public string Decision { get; set; } = string.Empty;

    /// <summary>
    /// The consequences of the decision.
    /// </summary>
    public string Consequences { get; set; } = string.Empty;

    /// <summary>
    /// The rendered HTML content.
    /// </summary>
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>
    /// The original markdown content.
    /// </summary>
    public string RawContent { get; set; } = string.Empty;

    /// <summary>
    /// The short name of the repository.
    /// </summary>
    public string RepositoryName { get; set; } = string.Empty;

    /// <summary>
    /// The full name of the repository (org/repo).
    /// </summary>
    public string RepositoryFullName { get; set; } = string.Empty;

    /// <summary>
    /// The file path within the repository.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// The URL to view the file on GitHub.
    /// </summary>
    public string GitHubUrl { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the ADR this supersedes, if any.
    /// </summary>
    public string? SupersedesId { get; set; }

    /// <summary>
    /// The ID of the ADR that superseded this one, if any.
    /// </summary>
    public string? SupersededById { get; set; }
}
