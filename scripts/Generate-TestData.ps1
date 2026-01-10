<#
.SYNOPSIS
    Generates sample ADR test data for development testing.

.DESCRIPTION
    Creates multiple simulated repositories with ADRs for testing
    the ADR Registry Generator locally.

.PARAMETER OutputPath
    The path where test data will be generated.

.PARAMETER RepoCount
    Number of repositories to generate.

.EXAMPLE
    .\Generate-TestData.ps1 -OutputPath "./test-repos"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$OutputPath = "test-repos",

    [Parameter(Mandatory = $false)]
    [int]$RepoCount = 5
)

$ErrorActionPreference = "Stop"

# Sample data for generating realistic ADRs
$TechDomains = @(
    @{
        Name = "payment-service"
        Topics = @(
            "payment gateway", "transaction processing", "refund handling", "currency conversion",
            "fraud detection", "PCI compliance", "payment methods", "recurring billing",
            "invoice generation", "payment reconciliation", "chargeback handling", "tax calculation"
        )
    },
    @{
        Name = "user-management"
        Topics = @(
            "authentication", "authorization", "user profiles", "role management",
            "password policy", "session handling", "OAuth integration", "SSO",
            "user preferences", "account recovery", "audit logging", "GDPR compliance"
        )
    },
    @{
        Name = "inventory-system"
        Topics = @(
            "stock tracking", "warehouse management", "order fulfillment", "supplier integration",
            "inventory forecasting", "barcode scanning", "batch management", "returns processing",
            "stock alerts", "multi-location", "inventory valuation", "cycle counting"
        )
    },
    @{
        Name = "notification-hub"
        Topics = @(
            "email delivery", "push notifications", "SMS gateway", "in-app messaging",
            "notification preferences", "template management", "delivery tracking", "rate limiting",
            "priority queuing", "batch notifications", "webhook delivery", "notification history"
        )
    },
    @{
        Name = "analytics-platform"
        Topics = @(
            "data ingestion", "event tracking", "dashboard design", "report generation",
            "data retention", "real-time analytics", "custom metrics", "data export",
            "visualization library", "query optimization", "data warehouse", "ETL pipeline"
        )
    }
)

$DecisionVerbs = @(
    "Use", "Adopt", "Implement", "Switch to", "Replace with", "Integrate",
    "Migrate to", "Standardize on", "Choose", "Select", "Deploy", "Configure"
)

$Technologies = @(
    "PostgreSQL", "Redis", "Kafka", "RabbitMQ", "Elasticsearch", "MongoDB",
    "GraphQL", "REST API", "gRPC", "WebSockets", "Docker", "Kubernetes",
    "AWS Lambda", "Azure Functions", "Terraform", "GitHub Actions", "Jenkins",
    "React", "Vue.js", "TypeScript", "Node.js", ".NET Core", "Python",
    "OAuth 2.0", "JWT", "OpenID Connect", "SAML", "mTLS", "HashiCorp Vault"
)

$Patterns = @(
    "CQRS pattern", "Event Sourcing", "Saga pattern", "Circuit Breaker",
    "Retry policy", "Bulkhead pattern", "Cache-Aside", "Outbox pattern",
    "Strangler Fig", "Anti-Corruption Layer", "Backend for Frontend", "API Gateway"
)

$Statuses = @(
    @{ Status = "Accepted"; Weight = 60 },
    @{ Status = "Proposed"; Weight = 20 },
    @{ Status = "Superseded"; Weight = 15 },
    @{ Status = "Deprecated"; Weight = 5 }
)

$Deciders = @(
    "Alice Chen", "Bob Smith", "Carol Johnson", "David Kim", "Eva Martinez",
    "Frank Wilson", "Grace Lee", "Henry Brown", "Iris Taylor", "Jack Anderson"
)

function Get-WeightedRandom {
    param([array]$Items)

    $totalWeight = ($Items | Measure-Object -Property Weight -Sum).Sum
    $random = Get-Random -Minimum 0 -Maximum $totalWeight
    $cumulative = 0

    foreach ($item in $Items) {
        $cumulative += $item.Weight
        if ($random -lt $cumulative) {
            return $item.Status
        }
    }
    return $Items[-1].Status
}

function Get-RandomSubset {
    param(
        [array]$Items,
        [int]$Min = 1,
        [int]$Max = 3
    )

    $count = Get-Random -Minimum $Min -Maximum ($Max + 1)
    return ($Items | Get-Random -Count ([Math]::Min($count, $Items.Count)))
}

function New-AdrContent {
    param(
        [int]$Number,
        [string]$Title,
        [string]$Status,
        [DateTime]$Date,
        [array]$DeciderList,
        [string]$Topic,
        [string]$RepoName,
        [int]$SupersedesNumber = 0
    )

    $deciderStr = ($DeciderList | ForEach-Object { $_ }) -join ", "

    $supersedes = if ($SupersedesNumber -gt 0) {
        "| Supersedes  | ADR-$('{0:D4}' -f $SupersedesNumber) |"
    } else { "" }

    $tech = $Technologies | Get-Random
    $pattern = $Patterns | Get-Random

    $contextOptions = @(
        "Our $Topic implementation needs to handle increasing scale and complexity. The current approach is becoming difficult to maintain and doesn't meet our performance requirements.",
        "As our system grows, we need a more robust solution for $Topic. The team has identified several pain points with the existing implementation.",
        "Recent incidents have highlighted the need for a better approach to $Topic. We need to improve reliability and reduce operational overhead.",
        "Customer feedback and internal metrics indicate that our $Topic solution needs improvement. We're seeing increased latency and occasional failures.",
        "The existing $Topic implementation was designed for a smaller scale. As we've grown, we've encountered limitations that require a new approach."
    )

    $decisionOptions = @(
        "We will $($DecisionVerbs | Get-Random) $tech for our $Topic needs. This technology provides the features we need and aligns with our team's expertise.",
        "We will implement the $pattern for $Topic. This approach will help us achieve better separation of concerns and improved testability.",
        "We will $($DecisionVerbs | Get-Random) $tech combined with $pattern. This combination addresses our scalability and maintainability requirements.",
        "We will migrate our $Topic implementation to use $tech. The migration will be phased to minimize risk and allow for learning.",
        "We will adopt $tech as our standard solution for $Topic. This decision aligns with our technology strategy and simplifies operations."
    )

    $content = @"
# [ADR-$('{0:D4}' -f $Number)] $Title

## Metadata

| Field       | Value                    |
|-------------|--------------------------|
| Date        | $($Date.ToString('yyyy-MM-dd')) |
| Status      | $Status |
| Deciders    | $deciderStr |
$supersedes

## Context

$($contextOptions | Get-Random)

Key considerations include:
- Performance requirements for high-volume scenarios
- Team familiarity with the technology
- Long-term maintenance and support
- Integration with existing systems
- Cost implications

## Decision

$($decisionOptions | Get-Random)

Key aspects of this decision:
1. We will start with a proof of concept to validate the approach
2. Documentation will be created for the new implementation
3. Training will be provided for team members as needed
4. Monitoring and alerting will be set up from the beginning

## Consequences

### Positive

- Improved scalability for $Topic
- Better alignment with industry best practices
- Reduced operational complexity
- Enhanced developer experience
- Better observability and debugging capabilities

### Negative

- Initial learning curve for the team
- Migration effort required for existing functionality
- Potential temporary increase in complexity during transition
- Need to update existing documentation and runbooks

### Neutral

- Will require updates to our CI/CD pipeline
- May influence future architectural decisions
- Team will gain experience with new technology

## Alternatives Considered

### Alternative 1: Continue with current approach

We considered continuing with our existing $Topic implementation. This was rejected because:
- Does not address the scalability concerns
- Technical debt continues to accumulate
- Team productivity is impacted

### Alternative 2: Build custom solution

We evaluated building a custom solution from scratch. This was not chosen because:
- Significantly higher development effort
- Ongoing maintenance burden
- Existing solutions are mature and well-tested

## References

- [Internal architecture guidelines]
- [Technology radar evaluation]
- [Team discussion notes from $($Date.ToString('yyyy-MM-dd'))]
"@

    return $content
}

# Main script
Write-Host "Generating test ADR data..." -ForegroundColor Cyan
Write-Host "Output path: $OutputPath" -ForegroundColor Gray
Write-Host "Repositories: $RepoCount" -ForegroundColor Gray
Write-Host ""

# Create output directory
if (Test-Path $OutputPath) {
    Remove-Item -Path $OutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

$totalAdrs = 0

for ($r = 0; $r -lt $RepoCount; $r++) {
    $repo = $TechDomains[$r % $TechDomains.Count]
    $repoPath = Join-Path $OutputPath $repo.Name "docs" "adr"

    New-Item -ItemType Directory -Path $repoPath -Force | Out-Null

    # Generate between 50 and 100 ADRs
    $adrCount = Get-Random -Minimum 50 -Maximum 101

    Write-Host "Creating $($repo.Name) with $adrCount ADRs..." -ForegroundColor Yellow

    # Track which ADRs get superseded
    $supersededBy = @{}

    # Generate ADRs
    for ($i = 1; $i -le $adrCount; $i++) {
        $topic = $repo.Topics | Get-Random
        $verb = $DecisionVerbs | Get-Random
        $tech = $Technologies | Get-Random

        $titleOptions = @(
            "$verb $tech for $topic",
            "$verb $($Patterns | Get-Random) for $topic",
            "Implement $topic with $tech",
            "Standardize $topic approach",
            "Update $topic strategy"
        )
        $title = $titleOptions | Get-Random

        # Generate a date within the last 3 years
        $daysAgo = Get-Random -Minimum 0 -Maximum 1095
        $date = (Get-Date).AddDays(-$daysAgo)

        $status = Get-WeightedRandom -Items $Statuses
        $deciderList = Get-RandomSubset -Items $Deciders -Min 2 -Max 4

        # Handle supersedes relationships
        $supersedesNumber = 0
        if ($status -eq "Accepted" -and $i -gt 10 -and (Get-Random -Minimum 0 -Maximum 100) -lt 15) {
            # This ADR supersedes an older one
            $candidateNumber = Get-Random -Minimum 1 -Maximum ($i - 5)
            if (-not $supersededBy.ContainsKey($candidateNumber)) {
                $supersedesNumber = $candidateNumber
                $supersededBy[$candidateNumber] = $i
            }
        }

        $content = New-AdrContent `
            -Number $i `
            -Title $title `
            -Status $status `
            -Date $date `
            -DeciderList $deciderList `
            -Topic $topic `
            -RepoName $repo.Name `
            -SupersedesNumber $supersedesNumber

        $slug = $title.ToLower() -replace "[^a-z0-9\s-]", "" -replace "\s+", "-" -replace "-+", "-"
        $slug = $slug.Substring(0, [Math]::Min(50, $slug.Length)).Trim("-")
        $filename = "{0:D4}-{1}.md" -f $i, $slug
        $filePath = Join-Path $repoPath $filename

        Set-Content -Path $filePath -Value $content -Encoding UTF8
        $totalAdrs++
    }

    # Create the template file
    $templatePath = Join-Path $repoPath "0000-template.md"
    Copy-Item -Path (Join-Path $PSScriptRoot ".." "docs" "adr" "0000-template.md") -Destination $templatePath -ErrorAction SilentlyContinue

    Write-Host "  Created $adrCount ADRs in $($repo.Name)" -ForegroundColor Green
}

Write-Host ""
Write-Host "Done! Generated $totalAdrs ADRs across $RepoCount repositories." -ForegroundColor Cyan
Write-Host ""
Write-Host "Test repositories created:" -ForegroundColor White
Get-ChildItem $OutputPath -Directory | ForEach-Object {
    $count = (Get-ChildItem (Join-Path $_.FullName "docs" "adr") -Filter "*.md" | Where-Object { $_.Name -ne "0000-template.md" }).Count
    Write-Host "  - $($_.Name): $count ADRs" -ForegroundColor Gray
}
