using Scriban;
using Scriban.Runtime;
using AdrRegistry.Generator.Models;

namespace AdrRegistry.Generator.Services;

/// <summary>
/// Generates the static HTML site from ADR data.
/// </summary>
public class SiteGenerator
{
    private readonly GeneratorConfig _config;
    private readonly string _templatePath;
    private readonly Dictionary<string, Template> _templates;

    public SiteGenerator(GeneratorConfig config)
    {
        _config = config;
        _templatePath = Path.Combine(AppContext.BaseDirectory, "Templates");
        _templates = new Dictionary<string, Template>();

        LoadTemplates();
    }

    private void LoadTemplates()
    {
        var templateFiles = new[]
        {
            "_Layout.html",
            "Index.html",
            "AdrList.html",
            "AdrDetail.html",
            "Repository.html",
            "RepositoryList.html"
        };

        foreach (var file in templateFiles)
        {
            var path = Path.Combine(_templatePath, file);
            if (File.Exists(path))
            {
                var content = File.ReadAllText(path);
                _templates[file] = Template.Parse(content);
            }
            else
            {
                Console.WriteLine($"Warning: Template not found: {path}");
            }
        }
    }

    /// <summary>
    /// Generates the complete static site.
    /// </summary>
    public async Task GenerateAsync(AdrIndex index, CancellationToken ct = default)
    {
        Console.WriteLine($"\nGenerating site to: {_config.OutputPath}");

        // Create output directories
        var outputPath = _config.OutputPath;
        if (Directory.Exists(outputPath))
        {
            // Preserve search directory if it exists (Pagefind will regenerate it)
            var searchPath = Path.Combine(outputPath, "search");
            var hasSearch = Directory.Exists(searchPath);

            Directory.Delete(outputPath, true);
        }

        Directory.CreateDirectory(outputPath);
        Directory.CreateDirectory(Path.Combine(outputPath, "adr"));
        Directory.CreateDirectory(Path.Combine(outputPath, "adrs"));
        Directory.CreateDirectory(Path.Combine(outputPath, "repos"));
        Directory.CreateDirectory(Path.Combine(outputPath, "css"));
        Directory.CreateDirectory(Path.Combine(outputPath, "js"));

        // Generate pages
        await GenerateDashboard(index, ct);
        await GenerateAdrList(index, ct);
        await GenerateAdrDetailPages(index, ct);
        await GenerateRepositoryList(index, ct);
        await GenerateRepositoryPages(index, ct);

        // Copy static assets
        await CopyStaticAssets(ct);

        Console.WriteLine("Site generation complete!");
    }

    private async Task GenerateDashboard(AdrIndex index, CancellationToken ct)
    {
        Console.WriteLine("  Generating dashboard...");

        var content = RenderTemplate("Index.html", new
        {
            site_title = _config.SiteTitle,
            site_description = _config.SiteDescription,
            base_url = _config.BaseUrl,
            total_adr_count = index.TotalAdrCount,
            repository_count = index.RepositoryCount,
            adrs_by_status = index.AdrsByStatus,
            recent_adrs = index.RecentAdrs,
            repositories = index.Repositories.OrderBy(r => r.Name).ToList()
        });

        var html = RenderWithLayout("Dashboard", content, index.GeneratedAt);
        await File.WriteAllTextAsync(Path.Combine(_config.OutputPath, "index.html"), html, ct);
    }

    private async Task GenerateAdrList(AdrIndex index, CancellationToken ct)
    {
        Console.WriteLine("  Generating ADR list...");

        var sortedAdrs = index.Adrs
            .OrderByDescending(a => a.Date ?? DateTime.MinValue)
            .ThenBy(a => a.RepositoryName)
            .ThenBy(a => a.Number)
            .ToList();

        var content = RenderTemplate("AdrList.html", new
        {
            base_url = _config.BaseUrl,
            total_adr_count = index.TotalAdrCount,
            repository_count = index.RepositoryCount,
            adrs = sortedAdrs
        });

        var html = RenderWithLayout("All ADRs", content, index.GeneratedAt);
        await File.WriteAllTextAsync(Path.Combine(_config.OutputPath, "adrs", "index.html"), html, ct);
    }

    private async Task GenerateAdrDetailPages(AdrIndex index, CancellationToken ct)
    {
        Console.WriteLine($"  Generating {index.Adrs.Count} ADR detail pages...");

        foreach (var adr in index.Adrs)
        {
            var content = RenderTemplate("AdrDetail.html", new
            {
                base_url = _config.BaseUrl,
                adr
            });

            var html = RenderWithLayout(adr.Title, content, index.GeneratedAt);
            var filename = $"{adr.Id}.html";
            await File.WriteAllTextAsync(Path.Combine(_config.OutputPath, "adr", filename), html, ct);
        }
    }

    private async Task GenerateRepositoryList(AdrIndex index, CancellationToken ct)
    {
        Console.WriteLine("  Generating repository list...");

        var content = RenderTemplate("RepositoryList.html", new
        {
            base_url = _config.BaseUrl,
            repository_count = index.RepositoryCount,
            repositories = index.Repositories.OrderBy(r => r.Name).ToList()
        });

        var html = RenderWithLayout("Repositories", content, index.GeneratedAt);
        await File.WriteAllTextAsync(Path.Combine(_config.OutputPath, "repos", "index.html"), html, ct);
    }

    private async Task GenerateRepositoryPages(AdrIndex index, CancellationToken ct)
    {
        Console.WriteLine($"  Generating {index.Repositories.Count} repository pages...");

        foreach (var repo in index.Repositories)
        {
            var statusCounts = repo.Adrs
                .GroupBy(a => a.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            var content = RenderTemplate("Repository.html", new
            {
                base_url = _config.BaseUrl,
                repository = repo,
                status_counts = statusCounts
            });

            var html = RenderWithLayout(repo.Name, content, index.GeneratedAt);
            var filename = $"{repo.Name}.html";
            await File.WriteAllTextAsync(Path.Combine(_config.OutputPath, "repos", filename), html, ct);
        }
    }

    private async Task CopyStaticAssets(CancellationToken ct)
    {
        Console.WriteLine("  Copying static assets...");

        // Write CSS
        var css = GetDefaultCss();
        await File.WriteAllTextAsync(Path.Combine(_config.OutputPath, "css", "style.css"), css, ct);

        // Write JS
        var js = GetDefaultJs();
        await File.WriteAllTextAsync(Path.Combine(_config.OutputPath, "js", "app.js"), js, ct);
    }

    private string RenderTemplate(string templateName, object model)
    {
        if (!_templates.TryGetValue(templateName, out var template))
        {
            throw new InvalidOperationException($"Template not found: {templateName}");
        }

        var scriptObject = new ScriptObject();
        scriptObject.Import(model, renamer: member => member.Name);

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        return template.Render(context);
    }

    private string RenderWithLayout(string title, string content, DateTime generatedAt)
    {
        if (!_templates.TryGetValue("_Layout.html", out var layout))
        {
            return content;
        }

        var scriptObject = new ScriptObject();
        scriptObject.Import(new
        {
            title,
            content,
            site_title = _config.SiteTitle,
            site_description = _config.SiteDescription,
            base_url = _config.BaseUrl,
            generated_at = generatedAt
        }, renamer: member => member.Name);

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        return layout.Render(context);
    }

    private static string GetDefaultCss()
    {
        return """
            :root {
                --color-primary: #2563eb;
                --color-secondary: #64748b;
                --color-success: #22c55e;
                --color-warning: #f59e0b;
                --color-danger: #ef4444;
                --color-bg: #ffffff;
                --color-bg-secondary: #f8fafc;
                --color-text: #1e293b;
                --color-text-muted: #64748b;
                --color-border: #e2e8f0;
                --radius: 8px;
                --shadow: 0 1px 3px rgba(0,0,0,0.1);
            }

            * {
                box-sizing: border-box;
                margin: 0;
                padding: 0;
            }

            body {
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                line-height: 1.6;
                color: var(--color-text);
                background: var(--color-bg-secondary);
            }

            .container {
                max-width: 1200px;
                margin: 0 auto;
                padding: 0 1rem;
            }

            /* Header */
            .header {
                background: var(--color-bg);
                border-bottom: 1px solid var(--color-border);
                padding: 1rem 0;
            }

            .nav {
                display: flex;
                align-items: center;
                justify-content: space-between;
            }

            .nav-brand {
                font-size: 1.25rem;
                font-weight: 600;
                color: var(--color-primary);
                text-decoration: none;
            }

            .nav-links {
                display: flex;
                gap: 1.5rem;
                list-style: none;
            }

            .nav-links a {
                color: var(--color-text-muted);
                text-decoration: none;
            }

            .nav-links a:hover {
                color: var(--color-primary);
            }

            /* Main */
            .main {
                padding: 2rem 0;
                min-height: calc(100vh - 200px);
            }

            /* Footer */
            .footer {
                background: var(--color-bg);
                border-top: 1px solid var(--color-border);
                padding: 1.5rem 0;
                text-align: center;
                color: var(--color-text-muted);
                font-size: 0.875rem;
            }

            .footer a {
                color: var(--color-primary);
            }

            /* Typography */
            h1 { font-size: 2rem; margin-bottom: 0.5rem; }
            h2 { font-size: 1.5rem; margin-bottom: 1rem; margin-top: 2rem; }
            h3 { font-size: 1.25rem; margin-bottom: 0.5rem; }

            .lead {
                font-size: 1.125rem;
                color: var(--color-text-muted);
                margin-bottom: 2rem;
            }

            /* Sections */
            .section {
                margin-bottom: 2rem;
            }

            /* Stats Grid */
            .stats-grid {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
                gap: 1rem;
                margin-bottom: 2rem;
            }

            .stat-card {
                background: var(--color-bg);
                border-radius: var(--radius);
                padding: 1.5rem;
                text-align: center;
                box-shadow: var(--shadow);
            }

            .stat-value {
                font-size: 2rem;
                font-weight: 700;
                color: var(--color-primary);
            }

            .stat-label {
                color: var(--color-text-muted);
                font-size: 0.875rem;
            }

            .stat-accepted .stat-value { color: var(--color-success); }
            .stat-proposed .stat-value { color: var(--color-warning); }
            .stat-deprecated .stat-value,
            .stat-superseded .stat-value { color: var(--color-text-muted); }

            /* ADR Cards */
            .adr-list {
                display: flex;
                flex-direction: column;
                gap: 1rem;
            }

            .adr-card {
                background: var(--color-bg);
                border-radius: var(--radius);
                padding: 1.25rem;
                box-shadow: var(--shadow);
            }

            .adr-header {
                display: flex;
                align-items: center;
                gap: 0.75rem;
                margin-bottom: 0.5rem;
            }

            .adr-title a {
                color: var(--color-text);
                text-decoration: none;
            }

            .adr-title a:hover {
                color: var(--color-primary);
            }

            .adr-meta {
                display: flex;
                gap: 1rem;
                font-size: 0.875rem;
                color: var(--color-text-muted);
            }

            .adr-meta a {
                color: var(--color-primary);
                text-decoration: none;
            }

            .adr-excerpt {
                margin-top: 0.75rem;
                color: var(--color-text-muted);
                font-size: 0.875rem;
            }

            /* Badges */
            .badge {
                display: inline-block;
                padding: 0.25rem 0.5rem;
                border-radius: 4px;
                font-size: 0.75rem;
                font-weight: 500;
                text-transform: uppercase;
            }

            .badge-accepted { background: #dcfce7; color: #166534; }
            .badge-proposed { background: #fef3c7; color: #92400e; }
            .badge-deprecated { background: #f1f5f9; color: #475569; }
            .badge-superseded { background: #f1f5f9; color: #475569; }
            .badge-unknown { background: #f1f5f9; color: #475569; }

            /* Repo Grid */
            .repo-grid {
                display: grid;
                grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
                gap: 1rem;
            }

            .repo-card {
                background: var(--color-bg);
                border-radius: var(--radius);
                padding: 1.25rem;
                box-shadow: var(--shadow);
                text-decoration: none;
                color: var(--color-text);
                transition: transform 0.2s, box-shadow 0.2s;
            }

            .repo-card:hover {
                transform: translateY(-2px);
                box-shadow: 0 4px 12px rgba(0,0,0,0.1);
            }

            .repo-card h3 {
                margin-bottom: 0.25rem;
            }

            .repo-count {
                color: var(--color-text-muted);
                font-size: 0.875rem;
            }

            /* ADR Detail */
            .breadcrumb {
                margin-bottom: 1.5rem;
                font-size: 0.875rem;
                color: var(--color-text-muted);
            }

            .breadcrumb a {
                color: var(--color-primary);
                text-decoration: none;
            }

            .adr-detail-header {
                margin-bottom: 2rem;
            }

            .adr-detail-meta {
                display: flex;
                align-items: center;
                gap: 0.75rem;
                margin-bottom: 0.5rem;
            }

            .adr-actions {
                margin-top: 1rem;
            }

            .adr-detail-body {
                display: grid;
                grid-template-columns: 1fr 280px;
                gap: 2rem;
            }

            @media (max-width: 768px) {
                .adr-detail-body {
                    grid-template-columns: 1fr;
                }
                .adr-sidebar {
                    order: -1;
                }
            }

            .adr-sidebar {
                background: var(--color-bg);
                border-radius: var(--radius);
                padding: 1.25rem;
                box-shadow: var(--shadow);
                height: fit-content;
            }

            .adr-sidebar h3 {
                margin-bottom: 1rem;
                padding-bottom: 0.5rem;
                border-bottom: 1px solid var(--color-border);
            }

            .adr-sidebar dl {
                display: grid;
                gap: 0.75rem;
            }

            .adr-sidebar dt {
                font-weight: 600;
                font-size: 0.75rem;
                text-transform: uppercase;
                color: var(--color-text-muted);
            }

            .adr-sidebar dd {
                margin-left: 0;
            }

            .adr-content {
                background: var(--color-bg);
                border-radius: var(--radius);
                padding: 2rem;
                box-shadow: var(--shadow);
            }

            .adr-content h1,
            .adr-content h2,
            .adr-content h3,
            .adr-content h4 {
                margin-top: 1.5rem;
                margin-bottom: 0.75rem;
            }

            /* Hide the duplicate title from rendered markdown */
            .adr-content > h1:first-child {
                display: none;
            }

            /* Hide the duplicate title + metadata table from rendered markdown */
            .adr-content > h1:first-child + h2#metadata,
            .adr-content > h1:first-child + h2#metadata + table {
                display: none;
            }

            /* Also handle when h1 is hidden - metadata section right after */
            .adr-content > h2#metadata:first-child,
            .adr-content > h2#metadata:first-child + table,
            .adr-content > h2:first-child + table {
                display: none;
            }

            .adr-content p {
                margin-bottom: 1rem;
            }

            .adr-content ul,
            .adr-content ol {
                margin-bottom: 1rem;
                padding-left: 1.5rem;
            }

            .adr-content li {
                margin-bottom: 0.25rem;
            }

            .adr-content table {
                width: 100%;
                border-collapse: collapse;
                margin-bottom: 1rem;
            }

            .adr-content th,
            .adr-content td {
                padding: 0.5rem;
                border: 1px solid var(--color-border);
                text-align: left;
            }

            .adr-content th {
                background: var(--color-bg-secondary);
            }

            .adr-content code {
                background: var(--color-bg-secondary);
                padding: 0.125rem 0.25rem;
                border-radius: 3px;
                font-size: 0.875em;
            }

            .adr-content pre {
                background: var(--color-bg-secondary);
                padding: 1rem;
                border-radius: var(--radius);
                overflow-x: auto;
                margin-bottom: 1rem;
            }

            .adr-content pre code {
                background: none;
                padding: 0;
            }

            /* Buttons */
            .btn {
                display: inline-block;
                padding: 0.5rem 1rem;
                border-radius: var(--radius);
                text-decoration: none;
                font-weight: 500;
                cursor: pointer;
                border: none;
                font-size: 0.875rem;
            }

            .btn-primary {
                background: var(--color-primary);
                color: white;
            }

            .btn-secondary {
                background: var(--color-bg-secondary);
                color: var(--color-text);
                border: 1px solid var(--color-border);
            }

            .btn-secondary:hover {
                background: var(--color-border);
            }

            /* Search */
            #search {
                margin-bottom: 1rem;
            }
            """;
    }

    private static string GetDefaultJs()
    {
        return """
            // ADR Registry JavaScript
            document.addEventListener('DOMContentLoaded', function() {
                console.log('ADR Registry loaded');
            });
            """;
    }
}
