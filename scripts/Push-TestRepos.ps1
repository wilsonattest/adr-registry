<#
.SYNOPSIS
    Pushes test ADR repositories to GitHub for testing.

.DESCRIPTION
    Creates Git repositories for each test repo and pushes them to GitHub.
    Requires GitHub CLI (gh) to be installed and authenticated.

.PARAMETER Organization
    The GitHub organization to push to.

.PARAMETER TestReposPath
    Path to the test-repos directory.

.PARAMETER Prefix
    Optional prefix for repository names (e.g., "adr-test-").

.EXAMPLE
    .\Push-TestRepos.ps1 -Organization "my-org"

.EXAMPLE
    .\Push-TestRepos.ps1 -Organization "my-org" -Prefix "adr-test-"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Organization,

    [Parameter(Mandatory = $false)]
    [string]$TestReposPath = "test-repos",

    [Parameter(Mandatory = $false)]
    [string]$Prefix = "adr-test-",

    [Parameter(Mandatory = $false)]
    [switch]$Private = $true
)

$ErrorActionPreference = "Stop"

# Check if gh CLI is installed
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI (gh) is not installed. Please install it from https://cli.github.com/"
    exit 1
}

# Check if authenticated
$authStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Not authenticated with GitHub CLI. Run 'gh auth login' first."
    exit 1
}

Write-Host "Pushing test repositories to GitHub organization: $Organization" -ForegroundColor Cyan
Write-Host "Repository prefix: $Prefix" -ForegroundColor Gray
Write-Host "Visibility: $(if ($Private) { 'Private' } else { 'Public' })" -ForegroundColor Gray
Write-Host ""

# Get all test repo directories
$repos = Get-ChildItem -Path $TestReposPath -Directory

if ($repos.Count -eq 0) {
    Write-Error "No repositories found in $TestReposPath"
    exit 1
}

Write-Host "Found $($repos.Count) repositories to push:" -ForegroundColor Yellow
$repos | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Gray }
Write-Host ""

$confirm = Read-Host "Continue? (y/n)"
if ($confirm -ne "y") {
    Write-Host "Aborted." -ForegroundColor Yellow
    exit 0
}

$created = @()
$failed = @()

foreach ($repo in $repos) {
    $repoName = "$Prefix$($repo.Name)"
    $repoPath = $repo.FullName

    Write-Host "`nProcessing $repoName..." -ForegroundColor Yellow

    try {
        # Initialize git repo if not already
        Push-Location $repoPath

        if (-not (Test-Path ".git")) {
            Write-Host "  Initializing git repository..."
            git init --initial-branch=main 2>&1 | Out-Null
        }

        # Create .gitignore if it doesn't exist
        if (-not (Test-Path ".gitignore")) {
            @"
# OS
.DS_Store
Thumbs.db

# IDE
.vs/
.vscode/
*.user
"@ | Set-Content ".gitignore"
        }

        # Add all files
        git add -A 2>&1 | Out-Null

        # Commit if there are changes
        $status = git status --porcelain
        if ($status) {
            Write-Host "  Committing files..."
            git commit -m "Initial commit: ADR test data" 2>&1 | Out-Null
        }

        # Check if repo exists on GitHub
        $existingRepo = gh repo view "$Organization/$repoName" 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  Repository already exists on GitHub" -ForegroundColor Gray
        }
        else {
            # Create repository on GitHub
            Write-Host "  Creating repository on GitHub..."
            $visibility = if ($Private) { "--private" } else { "--public" }
            gh repo create "$Organization/$repoName" $visibility --description "ADR test repository" 2>&1 | Out-Null

            if ($LASTEXITCODE -ne 0) {
                throw "Failed to create repository"
            }
        }

        # Set remote and push
        $remoteUrl = "https://github.com/$Organization/$repoName.git"

        # Remove existing remote if present
        git remote remove origin 2>&1 | Out-Null

        Write-Host "  Setting remote origin..."
        git remote add origin $remoteUrl

        Write-Host "  Pushing to GitHub..."
        git push -u origin main --force 2>&1 | Out-Null

        if ($LASTEXITCODE -ne 0) {
            throw "Failed to push to GitHub"
        }

        Write-Host "  Success!" -ForegroundColor Green
        $created += $repoName
    }
    catch {
        Write-Host "  Failed: $_" -ForegroundColor Red
        $failed += $repoName
    }
    finally {
        Pop-Location
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Created/Updated: $($created.Count)" -ForegroundColor Green
Write-Host "  Failed: $($failed.Count)" -ForegroundColor $(if ($failed.Count -gt 0) { "Red" } else { "Gray" })

if ($created.Count -gt 0) {
    Write-Host "`nRepositories:" -ForegroundColor White
    $created | ForEach-Object {
        Write-Host "  https://github.com/$Organization/$_" -ForegroundColor Gray
    }
}

if ($failed.Count -gt 0) {
    Write-Host "`nFailed:" -ForegroundColor Red
    $failed | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
}

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "  1. Update config/repositories.json with organization: '$Organization'" -ForegroundColor Gray
Write-Host "  2. Create a GitHub App and install it on your organization" -ForegroundColor Gray
Write-Host "  3. Run the generator to test with real GitHub API" -ForegroundColor Gray
