<#
.SYNOPSIS
    Creates a new Architecture Decision Record (ADR).

.DESCRIPTION
    Creates a new ADR file from the template with the next sequential number.
    Optionally marks an existing ADR as superseded.

.PARAMETER Title
    The title of the architecture decision.

.PARAMETER Supersedes
    Optional. The number of the ADR this decision supersedes (e.g., "0003").

.EXAMPLE
    .\New-Adr.ps1 -Title "Use PostgreSQL for persistence"

.EXAMPLE
    .\New-Adr.ps1 -Title "Switch to MySQL" -Supersedes "0005"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Title,

    [Parameter(Mandatory = $false)]
    [string]$Supersedes
)

$ErrorActionPreference = "Stop"

# Configuration
$AdrDir = "docs\adr"
$TemplateFile = "0000-template.md"

# Find repository root by walking up until we find docs/adr
function Find-RepoRoot {
    $dir = Get-Location
    while ($dir -ne $null) {
        $adrPath = Join-Path $dir $AdrDir
        if (Test-Path $adrPath -PathType Container) {
            return $dir
        }
        $parent = Split-Path $dir -Parent
        if ($parent -eq $dir) { break }
        $dir = $parent
    }
    return $null
}

# Get the next ADR number
function Get-NextAdrNumber {
    param([string]$AdrPath)

    $existingFiles = Get-ChildItem -Path $AdrPath -Filter "????-*.md" -File |
        Where-Object { $_.Name -match "^\d{4}-" } |
        Sort-Object Name -Descending

    if ($existingFiles.Count -eq 0) {
        return "0001"
    }

    $lastNumber = [int]($existingFiles[0].Name.Substring(0, 4))
    return "{0:D4}" -f ($lastNumber + 1)
}

# Convert title to URL-friendly slug
function ConvertTo-Slug {
    param([string]$Text)

    $slug = $Text.ToLower()
    $slug = $slug -replace "[^a-z0-9\s-]", ""
    $slug = $slug -replace "\s+", "-"
    $slug = $slug -replace "-+", "-"
    $slug = $slug.Trim("-")
    return $slug
}

# Main script
$repoRoot = Find-RepoRoot
if (-not $repoRoot) {
    Write-Error "Could not find '$AdrDir' directory. Make sure you're running this from within the repository."
    exit 1
}

$adrPath = Join-Path $repoRoot $AdrDir
$templatePath = Join-Path $adrPath $TemplateFile

if (-not (Test-Path $templatePath)) {
    Write-Error "Template file not found at: $templatePath"
    exit 1
}

# Generate new ADR details
$number = Get-NextAdrNumber -AdrPath $adrPath
$slug = ConvertTo-Slug -Text $Title
$filename = "$number-$slug.md"
$filepath = Join-Path $adrPath $filename
$date = Get-Date -Format "yyyy-MM-dd"

if (Test-Path $filepath) {
    Write-Error "File already exists: $filepath"
    exit 1
}

# Read and transform template
$content = Get-Content $templatePath -Raw

$content = $content -replace "\[ADR-NNNN\]", "[ADR-$number]"
$content = $content -replace "\[Title\]", $Title
$content = $content -replace "YYYY-MM-DD", $date
$content = $content -replace "Proposed / Accepted / Deprecated / Superseded", "Proposed"

# Handle supersedes
if ($Supersedes) {
    $content = $content -replace "\| Supersedes  \| \[ADR-NNNN\] \(if applicable\) \|", "| Supersedes  | ADR-$Supersedes |"

    # Find and update the old ADR
    $oldAdrFile = Get-ChildItem -Path $adrPath -Filter "$Supersedes-*.md" -File | Select-Object -First 1
    if ($oldAdrFile) {
        $oldContent = Get-Content $oldAdrFile.FullName -Raw
        $oldContent = $oldContent -replace "\| Superseded by \| \[ADR-NNNN\] \(if applicable\) \|", "| Superseded by | ADR-$number |"
        $oldContent = $oldContent -replace "\| Status      \| Accepted", "| Status      | Superseded"
        Set-Content -Path $oldAdrFile.FullName -Value $oldContent -NoNewline
        Write-Host "Updated $($oldAdrFile.Name) to mark as superseded" -ForegroundColor Yellow
    }
    else {
        Write-Warning "Could not find ADR-$Supersedes to update"
    }
}
else {
    # Remove supersedes/superseded lines if not applicable
    $content = $content -replace "(?m)^\| Supersedes  \|.*if applicable.*\r?\n", ""
    $content = $content -replace "(?m)^\| Superseded by \|.*if applicable.*\r?\n", ""
}

# Write the new ADR
Set-Content -Path $filepath -Value $content -NoNewline

Write-Host ""
Write-Host "Created: $filename" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Edit the ADR to fill in the details"
Write-Host "  2. Submit for review (e.g., via pull request)"
Write-Host "  3. Update status to 'Accepted' once approved"
Write-Host ""

# Return the path for VS Code integration
return $filepath
