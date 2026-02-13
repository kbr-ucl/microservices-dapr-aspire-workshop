<#
.SYNOPSIS
    Updates all Aspire-related NuGet packages and SDK versions.

.DESCRIPTION
    Updates the central Directory.Packages.props file (one level up from script dir)
    and the Aspire.AppHost.Sdk version in all .csproj files.

.PARAMETER TargetVersion
    Optional. Specify a specific Aspire SDK version (e.g., "9.2.0").
    If omitted, each package is updated to its latest stable NuGet version.

.PARAMETER DryRun
    If set, shows what would be updated without making changes.

.EXAMPLE
    .\Update-AspirePackages.ps1
    .\Update-AspirePackages.ps1 -TargetVersion "9.2.0"
    .\Update-AspirePackages.ps1 -DryRun
#>

param(
    [string]$TargetVersion,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot
$repoRoot = Split-Path $scriptDir -Parent
$propsFile = Join-Path $repoRoot "Directory.Packages.props"

if (-not (Test-Path $propsFile)) {
    Write-Error "Directory.Packages.props not found at: $propsFile"
    return
}

# SDK name used in AppHost projects
$aspireSdkName = "Aspire.AppHost.Sdk"

function Get-LatestNuGetVersion([string]$packageId) {
    try {
        $url = "https://api.nuget.org/v3-flatcontainer/$($packageId.ToLower())/index.json"
        $response = Invoke-RestMethod -Uri $url -ErrorAction Stop
        $versions = $response.versions | Where-Object { $_ -notmatch "-" }
        return $versions[$versions.Count - 1]
    }
    catch {
        Write-Warning "  Could not fetch latest version for ${packageId}: $_"
        return $null
    }
}

Write-Host ""
Write-Host "===== Aspire Package Updater =====" -ForegroundColor Cyan
Write-Host "Repo root : $repoRoot"
Write-Host "Props file: $propsFile"
Write-Host ""

# --- Step 1: Update Directory.Packages.props ---
Write-Host "--- Updating Directory.Packages.props ---" -ForegroundColor Yellow

[xml]$propsXml = Get-Content $propsFile -Raw
$packageVersionNodes = $propsXml.SelectNodes("//PackageVersion")
$updatedCount = 0

foreach ($node in $packageVersionNodes) {
    $packageName = $node.GetAttribute("Include")
    $currentVersion = $node.GetAttribute("Version")

    $latestVersion = Get-LatestNuGetVersion $packageName
    if (-not $latestVersion) {
        Write-Host "  [SKIP] $packageName (could not determine latest version)" -ForegroundColor DarkYellow
        continue
    }

    if ($currentVersion -eq $latestVersion) {
        Write-Host "  [UP-TO-DATE] $packageName ($currentVersion)" -ForegroundColor DarkGray
    }
    else {
        Write-Host "  [UPDATE] $packageName : $currentVersion -> $latestVersion" -ForegroundColor White
        if (-not $DryRun) {
            $node.SetAttribute("Version", $latestVersion)
            $updatedCount++
        }
    }
}

if ((-not $DryRun) -and ($updatedCount -gt 0)) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    $writer = New-Object System.IO.StreamWriter($propsFile, $false, $utf8NoBom)
    $propsXml.Save($writer)
    $writer.Close()
    Write-Host "  Saved $updatedCount package update(s) to Directory.Packages.props" -ForegroundColor Green
}

# --- Step 2: Update Aspire.AppHost.Sdk in .csproj files ---
Write-Host ""
Write-Host "--- Updating Aspire.AppHost.Sdk in .csproj files ---" -ForegroundColor Yellow

if ($TargetVersion) {
    $latestSdkVersion = $TargetVersion
    Write-Host "  Using specified SDK version: $latestSdkVersion" -ForegroundColor Green
}
else {
    Write-Host "  Querying NuGet for latest $aspireSdkName version..."
    $latestSdkVersion = Get-LatestNuGetVersion $aspireSdkName
    if (-not $latestSdkVersion) {
        Write-Error "Failed to determine latest SDK version. Use -TargetVersion to specify manually."
        return
    }
    Write-Host "  Latest $aspireSdkName version: $latestSdkVersion" -ForegroundColor Green
}

$csprojFiles = Get-ChildItem -Path $scriptDir -Recurse -Filter "*.csproj"
$appHostCount = 0

foreach ($file in $csprojFiles) {
    $content = Get-Content $file.FullName -Raw
    if ($content -match "Aspire\.AppHost\.Sdk/([\d\.]+)") {
        $currentVersion = $Matches[1]
        $relativePath = $file.FullName.Replace($scriptDir, ".").Replace($repoRoot, "..")

        if ($currentVersion -eq $latestSdkVersion) {
            Write-Host "  [UP-TO-DATE] $relativePath ($currentVersion)" -ForegroundColor DarkGray
        }
        else {
            Write-Host "  [UPDATE] $relativePath : $currentVersion -> $latestSdkVersion" -ForegroundColor White
            if (-not $DryRun) {
                $newContent = $content -replace "Aspire\.AppHost\.Sdk/[\d\.]+", "Aspire.AppHost.Sdk/$latestSdkVersion"
                Set-Content -Path $file.FullName -Value $newContent -NoNewline
                Write-Host "           Updated!" -ForegroundColor Green
            }
        }
        $appHostCount++
    }
}

if ($appHostCount -eq 0) {
    Write-Host "  No AppHost projects found." -ForegroundColor DarkGray
}

# --- Summary ---
Write-Host ""
Write-Host "===== Summary =====" -ForegroundColor Cyan
Write-Host "Packages in Directory.Packages.props: $($packageVersionNodes.Count)"
Write-Host "AppHost projects found: $appHostCount"

if ($DryRun) {
    Write-Host ""
    Write-Host "[DRY RUN] No changes were made. Remove -DryRun to apply updates." -ForegroundColor Yellow
}
else {
    Write-Host ""
    Write-Host "All updates applied. Run 'dotnet build' to verify." -ForegroundColor Green
}
