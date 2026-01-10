namespace AdrRegistry.Generator.Models;

/// <summary>
/// Configuration for the ADR Registry generator.
/// </summary>
public class GeneratorConfig
{
    /// <summary>
    /// The GitHub organization to scan.
    /// </summary>
    public string Organization { get; set; } = string.Empty;

    /// <summary>
    /// Whether to discover all repositories in the organization.
    /// </summary>
    public bool DiscoverAll { get; set; } = true;

    /// <summary>
    /// The path within each repository where ADRs are located.
    /// </summary>
    public string AdrPath { get; set; } = "docs/adr";

    /// <summary>
    /// Repositories to exclude (supports wildcards).
    /// </summary>
    public List<string> Exclude { get; set; } = new();

    /// <summary>
    /// Repositories to explicitly include (used when DiscoverAll is false).
    /// </summary>
    public List<string> Include { get; set; } = new();

    /// <summary>
    /// The title of the generated site.
    /// </summary>
    public string SiteTitle { get; set; } = "Architecture Decision Records";

    /// <summary>
    /// The description of the generated site.
    /// </summary>
    public string SiteDescription { get; set; } = "Cross-project ADR registry";

    /// <summary>
    /// The base URL for the generated site.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The output directory for the generated site.
    /// </summary>
    public string OutputPath { get; set; } = "docs";

    /// <summary>
    /// Whether to use local filesystem mode instead of GitHub API.
    /// </summary>
    public bool LocalMode { get; set; } = false;

    /// <summary>
    /// The local path containing repository directories (used when LocalMode is true).
    /// </summary>
    public string LocalPath { get; set; } = string.Empty;
}

/// <summary>
/// Configuration for GitHub App authentication.
/// </summary>
public class GitHubAppConfig
{
    /// <summary>
    /// The GitHub App ID.
    /// </summary>
    public int AppId { get; set; }

    /// <summary>
    /// The installation ID for the GitHub App.
    /// </summary>
    public long InstallationId { get; set; }

    /// <summary>
    /// The path to the private key PEM file.
    /// </summary>
    public string PrivateKeyPath { get; set; } = string.Empty;
}
