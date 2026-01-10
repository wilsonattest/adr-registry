using System.Text.RegularExpressions;
using Markdig;
using AdrRegistry.Generator.Models;

namespace AdrRegistry.Generator.Services;

/// <summary>
/// Parses ADR markdown files into structured Adr objects.
/// </summary>
public partial class AdrParser
{
    private readonly MarkdownPipeline _pipeline;

    public AdrParser()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    /// <summary>
    /// Parses an ADR markdown file.
    /// </summary>
    /// <param name="markdown">The markdown content.</param>
    /// <param name="repository">The repository containing the ADR.</param>
    /// <param name="filePath">The file path within the repository.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="gitHubUrl">The URL to view the file on GitHub.</param>
    public Adr Parse(string markdown, Repository repository, string filePath, string fileName, string gitHubUrl)
    {
        // Strip title and metadata for content-only HTML
        var contentMarkdown = StripTitleAndMetadata(markdown);

        var adr = new Adr
        {
            RawContent = markdown,
            HtmlContent = Markdown.ToHtml(contentMarkdown, _pipeline),
            RepositoryName = repository.Name,
            RepositoryFullName = repository.FullName,
            FilePath = filePath,
            GitHubUrl = gitHubUrl
        };

        // Extract number from filename (e.g., "0001-use-postgres.md" -> "0001")
        adr.Number = ExtractNumber(fileName);
        adr.Id = $"{repository.Name}_{adr.Number}";

        // Extract title from first heading
        adr.Title = ExtractTitle(markdown);

        // Parse metadata table
        var metadata = ExtractMetadataTable(markdown);
        adr.Date = ParseDate(metadata.GetValueOrDefault("Date", ""));
        adr.Status = metadata.GetValueOrDefault("Status", "Unknown");
        adr.Deciders = ParseDeciders(metadata.GetValueOrDefault("Deciders", ""));
        adr.SupersedesId = ParseAdrReference(metadata.GetValueOrDefault("Supersedes", ""), repository.Name);
        adr.SupersededById = ParseAdrReference(metadata.GetValueOrDefault("Superseded by", ""), repository.Name);

        // Extract sections
        adr.Context = ExtractSection(markdown, "Context");
        adr.Decision = ExtractSection(markdown, "Decision");
        adr.Consequences = ExtractSection(markdown, "Consequences");

        return adr;
    }

    /// <summary>
    /// Strips the title (first H1) and Metadata section from markdown for cleaner rendering.
    /// </summary>
    public string StripTitleAndMetadata(string markdown)
    {
        // Remove the first H1 heading line
        var result = TitlePattern().Replace(markdown, "", 1);

        // Remove the ## Metadata section including its table
        // Pattern: ## Metadata followed by everything until the next ## heading
        result = Regex.Replace(
            result,
            @"##\s+Metadata\s*\n[\s\S]*?(?=\n##\s)",
            "",
            RegexOptions.IgnoreCase);

        return result.TrimStart();
    }

    /// <summary>
    /// Extracts the ADR number from the filename.
    /// </summary>
    public string ExtractNumber(string fileName)
    {
        var match = NumberPattern().Match(fileName);
        return match.Success ? match.Groups[1].Value : "0000";
    }

    /// <summary>
    /// Extracts the title from the first H1 heading.
    /// </summary>
    public string ExtractTitle(string markdown)
    {
        var match = TitlePattern().Match(markdown);
        if (match.Success)
        {
            var title = match.Groups[1].Value.Trim();
            // Remove ADR number prefix if present (e.g., "[ADR-0001] Title" -> "Title")
            var prefixMatch = AdrPrefixPattern().Match(title);
            if (prefixMatch.Success)
            {
                title = title[(prefixMatch.Length)..].Trim();
            }
            return title;
        }
        return "Untitled";
    }

    /// <summary>
    /// Extracts the metadata table as a dictionary.
    /// </summary>
    public Dictionary<string, string> ExtractMetadataTable(string markdown)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Find the metadata section (table after ## Metadata heading or first table)
        var tableMatch = TableRowPattern().Matches(markdown);

        foreach (Match match in tableMatch)
        {
            var cells = match.Groups[1].Value
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .ToArray();

            if (cells.Length >= 2)
            {
                var key = cells[0];
                var value = cells[1];

                // Skip header separator rows
                if (!key.All(c => c == '-' || c == ':' || c == ' '))
                {
                    result[key] = value;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts a section by heading name.
    /// </summary>
    public string ExtractSection(string markdown, string sectionName)
    {
        var pattern = $@"##\s+{Regex.Escape(sectionName)}\s*\n([\s\S]*?)(?=\n##\s|\z)";
        var match = Regex.Match(markdown, pattern, RegexOptions.IgnoreCase);

        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        return string.Empty;
    }

    /// <summary>
    /// Parses a date string.
    /// </summary>
    public DateTime? ParseDate(string dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return null;

        if (DateTime.TryParse(dateStr, out var date))
            return date;

        return null;
    }

    /// <summary>
    /// Parses a comma-separated list of deciders.
    /// </summary>
    public List<string> ParseDeciders(string decidersStr)
    {
        if (string.IsNullOrWhiteSpace(decidersStr))
            return new List<string>();

        return decidersStr
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Trim().Trim('[', ']'))
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .ToList();
    }

    /// <summary>
    /// Parses an ADR reference (e.g., "ADR-0001") to an ID.
    /// </summary>
    public string? ParseAdrReference(string reference, string repoName)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return null;

        var match = AdrReferencePattern().Match(reference);
        if (match.Success)
        {
            var number = match.Groups[1].Value;
            return $"{repoName}_{number}";
        }

        return null;
    }

    [GeneratedRegex(@"^(\d{4})-")]
    private static partial Regex NumberPattern();

    [GeneratedRegex(@"^#\s+(.+)$", RegexOptions.Multiline)]
    private static partial Regex TitlePattern();

    [GeneratedRegex(@"^\[ADR-\d+\]\s*")]
    private static partial Regex AdrPrefixPattern();

    [GeneratedRegex(@"^\|(.+)\|$", RegexOptions.Multiline)]
    private static partial Regex TableRowPattern();

    [GeneratedRegex(@"ADR-(\d{4})")]
    private static partial Regex AdrReferencePattern();
}
