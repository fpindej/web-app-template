<#
.SYNOPSIS
    Web API Template - Unified Deploy Script

.DESCRIPTION
    Builds and pushes Docker images for the backend API and/or frontend.
    Manages version numbering automatically with auto-increment.

.PARAMETER Target
    What to deploy: backend, frontend, or all

.PARAMETER Patch
    Bump patch version: 0.1.0 → 0.1.1 (default)

.PARAMETER Minor
    Bump minor version: 0.1.0 → 0.2.0

.PARAMETER Major
    Bump major version: 0.1.0 → 1.0.0

.PARAMETER NoBump
    Don't increment version (rebuild same tag)

.PARAMETER NoPush
    Build only, don't push to registry

.PARAMETER NoLatest
    Don't update :latest tag

.PARAMETER Yes
    Skip confirmation prompts

.EXAMPLE
    .\deploy.ps1
    # Interactive mode - shows menu

.EXAMPLE
    .\deploy.ps1 backend -Minor
    # Deploy backend with minor version bump

.EXAMPLE
    .\deploy.ps1 all -NoPush
    # Build both without pushing
#>

param (
    [Parameter(Position = 0)]
    [ValidateSet("backend", "frontend", "all", "")]
    [string]$Target = "",
    
    [switch]$Patch,
    [switch]$Minor,
    [switch]$Major,
    [switch]$NoBump,
    [switch]$NoPush,
    [switch]$NoLatest,
    
    [Alias("y")]
    [switch]$Yes
)

$ErrorActionPreference = "Stop"

# ─────────────────────────────────────────────────────────────────────────────
# Colors and Formatting
# ─────────────────────────────────────────────────────────────────────────────
function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host "══════════════════════════════════════════════════════════════" -ForegroundColor Blue
    Write-Host "  $Text" -ForegroundColor Blue
    Write-Host "══════════════════════════════════════════════════════════════" -ForegroundColor Blue
}

function Write-Step {
    param([string]$Text)
    Write-Host ""
    Write-Host "▶ $Text" -ForegroundColor Cyan
}

function Write-SubStep {
    param([string]$Text)
    Write-Host "  → $Text" -ForegroundColor DarkGray
}

function Write-Success {
    param([string]$Text)
    Write-Host "✓ $Text" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Text)
    Write-Host "⚠ $Text" -ForegroundColor Yellow
}

function Write-ErrorMessage {
    param([string]$Text)
    Write-Host "✗ $Text" -ForegroundColor Red
}

function Write-Info {
    param([string]$Text)
    Write-Host "ℹ $Text" -ForegroundColor DarkGray
}

# ─────────────────────────────────────────────────────────────────────────────
# Helper Functions
# ─────────────────────────────────────────────────────────────────────────────
function Read-YesNo {
    param(
        [string]$Question,
        [bool]$Default = $true
    )
    
    if ($Yes) {
        return $Default
    }
    
    $hint = if ($Default) { "[Y/n]" } else { "[y/N]" }
    $response = Read-Host "$Question $hint"
    
    if ([string]::IsNullOrWhiteSpace($response)) {
        return $Default
    }
    
    return $response.ToLower() -eq "y"
}

function Read-Value {
    param(
        [string]$Question,
        [string]$Default = ""
    )
    
    $prompt = if ([string]::IsNullOrWhiteSpace($Default)) { $Question } else { "$Question [$Default]" }
    $response = Read-Host $prompt
    
    if ([string]::IsNullOrWhiteSpace($response)) {
        return $Default
    }
    return $response
}

function Set-FileContentNoBom {
    param(
        [string]$Path,
        [string]$Content
    )
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $Content, $utf8NoBom)
}

# ─────────────────────────────────────────────────────────────────────────────
# Version Management
# ─────────────────────────────────────────────────────────────────────────────
function Get-BumpedVersion {
    param(
        [string]$Version,
        [string]$BumpType
    )
    
    $parts = $Version.Split('.')
    $majorV = [int]$parts[0]
    $minorV = [int]$parts[1]
    $patchV = [int]$parts[2]
    
    switch ($BumpType) {
        "major" {
            $majorV++
            $minorV = 0
            $patchV = 0
        }
        "minor" {
            $minorV++
            $patchV = 0
        }
        "patch" {
            $patchV++
        }
    }
    
    return "$majorV.$minorV.$patchV"
}

# ─────────────────────────────────────────────────────────────────────────────
# Configuration Management
# ─────────────────────────────────────────────────────────────────────────────
$ConfigFile = "deploy.config.json"

function New-DefaultConfig {
    # Try to detect project name from directory structure
    $detectedName = ""
    $webApiDir = Get-ChildItem -Path "src\backend" -Directory -Filter "*.WebApi" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($webApiDir) {
        $detectedName = $webApiDir.Name -replace "\.WebApi$", ""
    }
    if ([string]::IsNullOrWhiteSpace($detectedName)) {
        $detectedName = "myproject"
    }
    $detectedLower = $detectedName.ToLower()
    
    $config = @{
        registry = "myusername"
        backendImage = "$detectedLower-api"
        frontendImage = "$detectedLower-frontend"
        version = "0.1.0"
        platform = "linux/amd64"
    }
    
    $json = $config | ConvertTo-Json -Depth 10
    Set-FileContentNoBom $ConfigFile $json
    
    return $config
}

function Read-Config {
    if (-not (Test-Path $ConfigFile)) {
        Write-Warning "Config file not found. Creating default..."
        return New-DefaultConfig
    }
    
    $content = Get-Content $ConfigFile -Raw
    return $content | ConvertFrom-Json
}

function Save-Config {
    param($Config)
    
    $json = $Config | ConvertTo-Json -Depth 10
    Set-FileContentNoBom $ConfigFile $json
}

function Show-ConfigureRegistry {
    param($Config)
    
    Write-Header "Deploy Configuration"
    
    Write-Host ""
    Write-Info "Current configuration:"
    Write-Host "  Registry:       " -NoNewline; Write-Host $Config.registry -ForegroundColor Cyan
    Write-Host "  Backend Image:  " -NoNewline; Write-Host $Config.backendImage -ForegroundColor Cyan
    Write-Host "  Frontend Image: " -NoNewline; Write-Host $Config.frontendImage -ForegroundColor Cyan
    Write-Host "  Platform:       " -NoNewline; Write-Host $Config.platform -ForegroundColor Cyan
    Write-Host "  Version:        " -NoNewline; Write-Host $Config.version -ForegroundColor Cyan
    Write-Host ""
    
    $reconfigure = Read-YesNo "Reconfigure settings?" $false
    
    if ($reconfigure) {
        Write-Host ""
        $Config.registry = Read-Value "Docker registry (e.g., myusername, ghcr.io/myuser)" $Config.registry
        $Config.backendImage = Read-Value "Backend image name" $Config.backendImage
        $Config.frontendImage = Read-Value "Frontend image name" $Config.frontendImage
        $Config.platform = Read-Value "Target platform" $Config.platform
        Save-Config $Config
        Write-Success "Configuration saved"
    }
    
    return $Config
}

# ─────────────────────────────────────────────────────────────────────────────
# Build Functions
# ─────────────────────────────────────────────────────────────────────────────
function Test-Docker {
    try {
        docker system info 2>$null | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

function Initialize-Buildx {
    try {
        docker buildx inspect default 2>$null | Out-Null
    }
    catch {
        docker buildx create --use 2>$null | Out-Null
    }
}

function Build-Backend {
    param(
        [string]$Version,
        [bool]$Push,
        [bool]$TagLatest,
        $Config
    )
    
    Write-Step "Building Backend API..."
    
    $fullImage = "$($Config.registry)/$($Config.backendImage)"
    
    # Find the WebApi directory
    $webApiDir = Get-ChildItem -Path "src\backend" -Directory -Filter "*.WebApi" -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $webApiDir) {
        Write-ErrorMessage "WebApi directory not found in src\backend"
        return $false
    }
    
    $dockerfile = Join-Path $webApiDir.FullName "Dockerfile"
    if (-not (Test-Path $dockerfile)) {
        Write-ErrorMessage "Dockerfile not found: $dockerfile"
        return $false
    }
    
    Write-SubStep "Image: ${fullImage}:${Version}"
    
    $buildArgs = @(
        "buildx", "build",
        "--platform", $Config.platform,
        "-t", "${fullImage}:${Version}"
    )
    
    if ($TagLatest) {
        $buildArgs += @("-t", "${fullImage}:latest")
    }
    
    if ($Push) {
        $buildArgs += "--push"
    }
    else {
        $buildArgs += "--load"
    }
    
    $buildArgs += @("-f", "$($webApiDir.Name)/Dockerfile", ".")
    
    Push-Location "src\backend"
    try {
        $result = & docker @buildArgs 2>&1
        if ($LASTEXITCODE -ne 0) {
            Pop-Location
            Write-ErrorMessage "Backend build failed"
            Write-Host $result -ForegroundColor Red
            return $false
        }
    }
    finally {
        Pop-Location
    }
    
    if ($Push) {
        Write-Success "Backend pushed: ${fullImage}:${Version}"
    }
    else {
        Write-Success "Backend built: ${fullImage}:${Version}"
    }
    
    return $true
}

function Build-Frontend {
    param(
        [string]$Version,
        [bool]$Push,
        [bool]$TagLatest,
        $Config
    )
    
    Write-Step "Building Frontend..."
    
    $fullImage = "$($Config.registry)/$($Config.frontendImage)"
    
    if (-not (Test-Path "src\frontend\Dockerfile")) {
        Write-ErrorMessage "Dockerfile not found: src\frontend\Dockerfile"
        return $false
    }
    
    Write-SubStep "Image: ${fullImage}:${Version}"
    
    $buildArgs = @(
        "buildx", "build",
        "--platform", $Config.platform,
        "-t", "${fullImage}:${Version}"
    )
    
    if ($TagLatest) {
        $buildArgs += @("-t", "${fullImage}:latest")
    }
    
    if ($Push) {
        $buildArgs += "--push"
    }
    else {
        $buildArgs += "--load"
    }
    
    $buildArgs += "."
    
    Push-Location "src\frontend"
    try {
        $result = & docker @buildArgs 2>&1
        if ($LASTEXITCODE -ne 0) {
            Pop-Location
            Write-ErrorMessage "Frontend build failed"
            Write-Host $result -ForegroundColor Red
            return $false
        }
    }
    finally {
        Pop-Location
    }
    
    if ($Push) {
        Write-Success "Frontend pushed: ${fullImage}:${Version}"
    }
    else {
        Write-Success "Frontend built: ${fullImage}:${Version}"
    }
    
    return $true
}

# ─────────────────────────────────────────────────────────────────────────────
# Main Script
# ─────────────────────────────────────────────────────────────────────────────

# Determine bump type
$BumpType = "patch"
if ($Major) { $BumpType = "major" }
elseif ($Minor) { $BumpType = "minor" }
elseif ($NoBump) { $BumpType = "none" }

$DoPush = -not $NoPush
$TagLatestFlag = -not $NoLatest

# Change to script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

Write-Header "Deploy"

# Check prerequisites
Write-Step "Checking prerequisites..."
if (-not (Test-Docker)) {
    Write-ErrorMessage "Docker is not running"
    exit 1
}
Initialize-Buildx
Write-Success "Docker is running"

# Load configuration
$Config = Read-Config

# Check if registry is default
if ($Config.registry -eq "myusername") {
    Write-Warning "Registry is set to default 'myusername'"
    $Config = Show-ConfigureRegistry $Config
    $Config = Read-Config
}

# Interactive target selection if not specified
if ([string]::IsNullOrWhiteSpace($Target)) {
    $Config = Show-ConfigureRegistry $Config
    
    Write-Host ""
    Write-Host "What would you like to deploy?" -ForegroundColor White
    Write-Host ""
    Write-Host "  [1] Backend API"
    Write-Host "  [2] Frontend"
    Write-Host "  [3] Both"
    Write-Host ""
    $choice = Read-Host "Choose [1-3]"
    
    switch ($choice) {
        "1" { $Target = "backend" }
        "2" { $Target = "frontend" }
        "3" { $Target = "all" }
        default {
            Write-ErrorMessage "Invalid choice"
            exit 1
        }
    }
}

# Calculate new version
$NewVersion = $Config.version
if ($BumpType -ne "none") {
    $NewVersion = Get-BumpedVersion $Config.version $BumpType
}

# Summary
Write-Header "Summary"

Write-Host ""
Write-Host "  Deploy Target" -ForegroundColor White
Write-Host "  ─────────────────────────────────────"
switch ($Target) {
    "backend"  { Write-Host "  Target:   " -NoNewline; Write-Host "Backend API" -ForegroundColor Cyan }
    "frontend" { Write-Host "  Target:   " -NoNewline; Write-Host "Frontend" -ForegroundColor Cyan }
    "all"      { Write-Host "  Target:   " -NoNewline; Write-Host "Backend + Frontend" -ForegroundColor Cyan }
}
Write-Host ""
Write-Host "  Version" -ForegroundColor White
Write-Host "  ─────────────────────────────────────"
Write-Host "  Current:  " -NoNewline; Write-Host $Config.version -ForegroundColor DarkGray
Write-Host "  New:      " -NoNewline; Write-Host $NewVersion -ForegroundColor Green
Write-Host ""
Write-Host "  Options" -ForegroundColor White
Write-Host "  ─────────────────────────────────────"
Write-Host "  Push to registry: " -NoNewline
if ($DoPush) { Write-Host "Yes" -ForegroundColor Green } else { Write-Host "No (build only)" -ForegroundColor Yellow }
Write-Host "  Update :latest:   " -NoNewline
if ($TagLatestFlag) { Write-Host "Yes" -ForegroundColor Green } else { Write-Host "No" -ForegroundColor DarkGray }
Write-Host ""

# Confirmation
$proceed = Read-YesNo "Proceed with deployment?" $true
if (-not $proceed) {
    Write-Warning "Aborted by user"
    exit 0
}

# Execute
Write-Header "Building"

$Failed = $false

if ($Target -eq "backend" -or $Target -eq "all") {
    if (-not (Build-Backend -Version $NewVersion -Push $DoPush -TagLatest $TagLatestFlag -Config $Config)) {
        $Failed = $true
    }
}

if ($Target -eq "frontend" -or $Target -eq "all") {
    if (-not (Build-Frontend -Version $NewVersion -Push $DoPush -TagLatest $TagLatestFlag -Config $Config)) {
        $Failed = $true
    }
}

if ($Failed) {
    Write-Header "Deploy Failed"
    Write-ErrorMessage "One or more builds failed. Version not updated."
    exit 1
}

# Update version in config and commit
if ($BumpType -ne "none" -and $DoPush) {
    Write-Step "Updating version..."
    $Config.version = $NewVersion
    Save-Config $Config
    
    # Commit the version bump
    try {
        git rev-parse --git-dir 2>$null | Out-Null
        git add $ConfigFile 2>$null
        git commit -m "chore: bump version to $NewVersion" 2>$null
        Write-Success "Version bumped to $NewVersion (committed)"
    }
    catch {
        Write-Success "Version bumped to $NewVersion"
    }
}

# Complete
Write-Header "Deploy Complete! 🚀"

Write-Host ""
Write-Host "  Deployed Images" -ForegroundColor White
Write-Host "  ─────────────────────────────────────"
if ($Target -eq "backend" -or $Target -eq "all") {
    Write-Host "  $($Config.registry)/$($Config.backendImage):$NewVersion" -ForegroundColor Cyan
}
if ($Target -eq "frontend" -or $Target -eq "all") {
    Write-Host "  $($Config.registry)/$($Config.frontendImage):$NewVersion" -ForegroundColor Cyan
}
Write-Host ""
