param(
    [Parameter(Mandatory)] [string] $ApiKey,
    [string] $Source = "https://api.nuget.org/v3/index.json",
    [switch] $IncrementMinor
)

$ErrorActionPreference = "Stop"
$artifactsDir = Join-Path (Join-Path $PSScriptRoot "..") "artifacts"
$directoryBuildProps = Join-Path $PSScriptRoot "Directory.Build.props"

# --- Compute next version from git tags ---

function Get-NextVersion {
    try { git fetch --tags 2>$null } catch { Write-Host "Could not fetch tags from remote" -ForegroundColor Yellow }

    $tags = git tag -l "v*" --sort=-v:refname 2>$null
    $latestTag = $tags | Where-Object { $_ -match "^v\d+\.\d+\.\d+$" } | Select-Object -First 1

    if (-not $latestTag) {
        Write-Host "No version tag found. Baseline: git tag v0.1.0 && git push origin v0.1.0" -ForegroundColor Yellow
        $major = 0; $minor = 1; $patch = 0
    } else {
        $parts = $latestTag.TrimStart("v").Split(".")
        $major = [int]$parts[0]; $minor = [int]$parts[1]; $patch = [int]$parts[2]
        Write-Host "Current version: $latestTag" -ForegroundColor Cyan
    }

    if ($IncrementMinor) { $minor++; $patch = 0 }
    else { $patch++ }

    $baseVersion = "$major.$minor.$patch"

    # Detect branch
    $branch = if ($env:BUILD_SOURCEBRANCH) {
        $env:BUILD_SOURCEBRANCH -replace "^refs/heads/", ""
    } else {
        (git rev-parse --abbrev-ref HEAD 2>$null).Trim()
    }

    if ($branch -in @("main", "master", "fresh-start")) {
        $label = if ($IncrementMinor) { "minor increment" } else { "patch increment" }
        Write-Host "Next version: $baseVersion ($label)" -ForegroundColor Green
        return $baseVersion
    }

    # Feature branch: pre-release suffix
    $branchSuffix = ($branch -replace "^[^/]+/", "") -replace "[^a-zA-Z0-9-]", "-"
    $branchSuffix = $branchSuffix.Trim("-")

    $pattern = "^v$([regex]::Escape($baseVersion))-$([regex]::Escape($branchSuffix))\.(\d+)$"
    $latestIndex = $tags |
        ForEach-Object { if ($_ -match $pattern) { [int]$Matches[1] } } |
        Measure-Object -Maximum |
        Select-Object -ExpandProperty Maximum

    if (-not $latestIndex) { $latestIndex = 0 }

    $version = "$baseVersion-$branchSuffix.$($latestIndex + 1)"
    Write-Host "Next version: $version (feature branch: $branch)" -ForegroundColor Green
    return $version
}

function Set-VersionInProps([string]$version) {
    $content = Get-Content $directoryBuildProps -Raw
    $content = $content -replace "<Version>[^<]*</Version>", "<Version>$version</Version>"
    Set-Content $directoryBuildProps -Value $content -NoNewline
    Write-Host "Directory.Build.props updated: $version" -ForegroundColor Cyan
}

# --- Main ---

$version = Get-NextVersion
Set-VersionInProps $version

# Clean
if (Test-Path $artifactsDir) { Remove-Item $artifactsDir -Recurse -Force }
New-Item $artifactsDir -ItemType Directory | Out-Null

# Pack
$projects = @(
    "MediatR.Extensions.Common"
    "MediatR.Extensions.Mocking"
    "MediatR.Extensions.Mocking.Generator"
    "MediatR.Extensions.Facade"
    "MediatR.Extensions.FluentBuilder"
)

foreach ($project in $projects) {
    Write-Host "Packing $project $version..." -ForegroundColor Cyan
    dotnet pack "$PSScriptRoot/$project/$project.csproj" -c Release -o $artifactsDir
    if ($LASTEXITCODE -ne 0) { throw "Pack failed for $project" }
}

# Push
$packages = Get-ChildItem $artifactsDir -Filter "*.nupkg"
foreach ($pkg in $packages) {
    Write-Host "Pushing $($pkg.Name)..." -ForegroundColor Green
    dotnet nuget push $pkg.FullName --api-key $ApiKey --source $Source --skip-duplicate
    if ($LASTEXITCODE -ne 0) { throw "Push failed for $($pkg.Name)" }
}

# Tag
git tag "v$version"
git push origin "v$version"
Write-Host "`nDone! Published $($packages.Count) packages v$version to $Source" -ForegroundColor Green
Write-Host "Tag v$version created and pushed." -ForegroundColor Green
