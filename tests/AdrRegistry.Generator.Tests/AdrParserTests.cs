using Xunit;
using AdrRegistry.Generator.Models;
using AdrRegistry.Generator.Services;

namespace AdrRegistry.Generator.Tests;

public class AdrParserTests
{
    private readonly AdrParser _parser;
    private readonly Repository _testRepo;

    public AdrParserTests()
    {
        _parser = new AdrParser();
        _testRepo = new Repository
        {
            Name = "test-repo",
            FullName = "org/test-repo",
            DefaultBranch = "main"
        };
    }

    [Fact]
    public void ExtractNumber_WithValidFilename_ReturnsNumber()
    {
        var result = _parser.ExtractNumber("0001-use-postgres.md");
        Assert.Equal("0001", result);
    }

    [Fact]
    public void ExtractNumber_WithHighNumber_ReturnsNumber()
    {
        var result = _parser.ExtractNumber("0123-some-decision.md");
        Assert.Equal("0123", result);
    }

    [Fact]
    public void ExtractNumber_WithInvalidFilename_ReturnsDefault()
    {
        var result = _parser.ExtractNumber("readme.md");
        Assert.Equal("0000", result);
    }

    [Fact]
    public void ExtractTitle_WithAdrPrefix_RemovesPrefix()
    {
        var markdown = "# [ADR-0001] Use PostgreSQL for persistence";
        var result = _parser.ExtractTitle(markdown);
        Assert.Equal("Use PostgreSQL for persistence", result);
    }

    [Fact]
    public void ExtractTitle_WithoutPrefix_ReturnsTitle()
    {
        var markdown = "# Use PostgreSQL for persistence";
        var result = _parser.ExtractTitle(markdown);
        Assert.Equal("Use PostgreSQL for persistence", result);
    }

    [Fact]
    public void ExtractTitle_WithNoHeading_ReturnsUntitled()
    {
        var markdown = "Some content without a heading";
        var result = _parser.ExtractTitle(markdown);
        Assert.Equal("Untitled", result);
    }

    [Fact]
    public void ExtractMetadataTable_WithValidTable_ExtractsFields()
    {
        var markdown = """
            # [ADR-0001] Test

            ## Metadata

            | Field       | Value                    |
            |-------------|--------------------------|
            | Date        | 2026-01-09               |
            | Status      | Accepted                 |
            | Deciders    | Alice, Bob               |
            """;

        var result = _parser.ExtractMetadataTable(markdown);

        Assert.Equal("2026-01-09", result["Date"]);
        Assert.Equal("Accepted", result["Status"]);
        Assert.Equal("Alice, Bob", result["Deciders"]);
    }

    [Fact]
    public void ExtractSection_WithValidSection_ReturnsContent()
    {
        var markdown = """
            # Test ADR

            ## Context

            This is the context section with important information.

            ## Decision

            We will do something.
            """;

        var context = _parser.ExtractSection(markdown, "Context");
        var decision = _parser.ExtractSection(markdown, "Decision");

        Assert.Contains("context section", context);
        Assert.Contains("We will do something", decision);
    }

    [Fact]
    public void ExtractSection_WithMissingSection_ReturnsEmpty()
    {
        var markdown = """
            # Test ADR

            ## Context

            Some context.
            """;

        var result = _parser.ExtractSection(markdown, "Decision");
        Assert.Empty(result);
    }

    [Fact]
    public void ParseDate_WithValidDate_ReturnsDateTime()
    {
        var result = _parser.ParseDate("2026-01-09");
        Assert.NotNull(result);
        Assert.Equal(2026, result.Value.Year);
        Assert.Equal(1, result.Value.Month);
        Assert.Equal(9, result.Value.Day);
    }

    [Fact]
    public void ParseDate_WithInvalidDate_ReturnsNull()
    {
        var result = _parser.ParseDate("not-a-date");
        Assert.Null(result);
    }

    [Fact]
    public void ParseDate_WithEmptyString_ReturnsNull()
    {
        var result = _parser.ParseDate("");
        Assert.Null(result);
    }

    [Fact]
    public void ParseDeciders_WithCommaSeparatedList_ReturnsAll()
    {
        var result = _parser.ParseDeciders("Alice, Bob, Charlie");
        Assert.Equal(3, result.Count);
        Assert.Contains("Alice", result);
        Assert.Contains("Bob", result);
        Assert.Contains("Charlie", result);
    }

    [Fact]
    public void ParseDeciders_WithBrackets_RemovesBrackets()
    {
        var result = _parser.ParseDeciders("[Team Lead], [Architect]");
        Assert.Equal(2, result.Count);
        Assert.Contains("Team Lead", result);
        Assert.Contains("Architect", result);
    }

    [Fact]
    public void ParseDeciders_WithEmptyString_ReturnsEmptyList()
    {
        var result = _parser.ParseDeciders("");
        Assert.Empty(result);
    }

    [Fact]
    public void ParseAdrReference_WithValidReference_ReturnsId()
    {
        var result = _parser.ParseAdrReference("ADR-0005", "test-repo");
        Assert.Equal("test-repo_0005", result);
    }

    [Fact]
    public void ParseAdrReference_WithInvalidReference_ReturnsNull()
    {
        var result = _parser.ParseAdrReference("not a reference", "test-repo");
        Assert.Null(result);
    }

    [Fact]
    public void Parse_WithFullAdr_ExtractsAllFields()
    {
        var markdown = """
            # [ADR-0001] Use PostgreSQL for persistence

            ## Metadata

            | Field       | Value                    |
            |-------------|--------------------------|
            | Date        | 2026-01-09               |
            | Status      | Accepted                 |
            | Deciders    | Alice, Bob               |

            ## Context

            We need a relational database for our application.

            ## Decision

            We will use PostgreSQL as our primary database.

            ## Consequences

            ### Positive

            - Strong SQL support
            - Excellent performance

            ### Negative

            - Requires more setup than SQLite
            """;

        var adr = _parser.Parse(
            markdown,
            _testRepo,
            "docs/adr/0001-use-postgres.md",
            "0001-use-postgres.md",
            "https://github.com/org/test-repo/blob/main/docs/adr/0001-use-postgres.md");

        Assert.Equal("test-repo_0001", adr.Id);
        Assert.Equal("0001", adr.Number);
        Assert.Equal("Use PostgreSQL for persistence", adr.Title);
        Assert.Equal("Accepted", adr.Status);
        Assert.Equal(new DateTime(2026, 1, 9), adr.Date);
        Assert.Contains("Alice", adr.Deciders);
        Assert.Contains("Bob", adr.Deciders);
        Assert.Contains("relational database", adr.Context);
        Assert.Contains("PostgreSQL", adr.Decision);
        Assert.Contains("Strong SQL support", adr.Consequences);
        Assert.Equal("test-repo", adr.RepositoryName);
        Assert.Equal("org/test-repo", adr.RepositoryFullName);
        Assert.NotEmpty(adr.HtmlContent);
    }
}
