<#
.SYNOPSIS
    Web API Template - Unified Deploy Script

.DESCRIPTION
    Builds and pushes Docker images for the backend API and/or frontend.
    Manages version numbering automatically with auto-increment.

.PARAMETER Target
    What to deploy: backend, frontend, or all

.PARAMETER Patch
    Bump patch version: 0.1.0 -> 0.1.1 (default)

.PARAMETER Minor
    Bump minor version: 0.1.0 -> 0.2.0

.PARAMETER Major
    Bump major version: 0.1.0 -> 1.0.0

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

# Get script directory and ensure we're working from there
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $ScriptDir

try {

# -----------------------------------------------------------------------------
# Colors and Formatting
# -----------------------------------------------------------------------------
function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host "==============================================================" -ForegroundColor Blue
    Write-Host "  $Text" -ForegroundColor Blue
    Write-Host "==============================================================" -ForegroundColor Blue
}

function Write-Step {
    param([string]$Text)
    Write-Host ""
    Write-Host ">> $Text" -ForegroundColor Cyan
}

function Write-SubStep {
    param([string]$Text)
    Write-Host "   -> $Text" -ForegroundColor DarkGray
}

function Write-Success {
    param([string]$Text)
    Write-Host "[OK] $Text" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Text)
    Write-Host "[WARN] $Text" -ForegroundColor Yellow
}

function Write-ErrorMessage {
    param([string]$Text)
    Write-Host "[ERROR] $Text" -ForegroundColor Red
}

function Write-Info {
    param([string]$Text)
    Write-Host "[INFO] $Text" -ForegroundColor DarkGray
}

# -----------------------------------------------------------------------------
# Helper Functions
# -----------------------------------------------------------------------------
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

# -----------------------------------------------------------------------------
# Version Management
# -----------------------------------------------------------------------------
function Get-BumpedVersion {
    param(
        [string]$Version,
        [string]$BumpType
    )
    
    $parts = $Version.Split('.')
    $major = [int]$parts[0]
    $minor = [int]$parts[1]
    $patch = [int]$parts[2]
    
    switch ($BumpType) {
        "major" {
            $major++
            $minor = 0
            $patch = 0
        }
        "minor" {
            $minor++
            $patch = 0
        }
        "patch" {
            $patch++
        }
    }
    
    return "$major.$minor.$patch"
}

# -----------------------------------------------------------------------------
# Configuration Management
# -----------------------------------------------------------------------------
$ConfigFile = Join-Path $ScriptDir "deploy.config.json"

function Get-Config {
    if (-not (Test-Path $ConfigFile)) {
        Write-Warning "Config file not found. Creating default..."
        
        # Try to detect project name
        $detectedName = "MyProject"
        $webApiDir = Get-ChildItem -Path "src\backend" -Directory -Filter "*.WebApi" -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($webApiDir) {
            $detectedName = $webApiDir.Name -replace '\.WebApi$', ''
        }
        # Convert PascalCase to kebab-case
        $detectedSlug = ($detectedName -creplace '([a-z])([A-Z])', '$1-$2').ToLower()
        
        $defaultConfig = @{
            registry = "myusername"
            backendImage = "$detectedSlug-api"
            frontendImage = "$detectedSlug-frontend"
            backendVersion = "0.1.0"
            frontendVersion = "0.1.0"
            platform = "linux/amd64"
        }
        
        $defaultConfig | ConvertTo-Json | Set-Content $ConfigFile -Encoding UTF8
    }
    
    return Get-Content $ConfigFile -Raw | ConvertFrom-Json
}

function Save-Config {
    param($Config)
    $Config | ConvertTo-Json | Set-Content $ConfigFile -Encoding UTF8
}

function Show-ConfigureRegistry {
    param($Config)
    
    Write-Header "Deploy Configuration"
    
    Write-Host ""
    Write-Info "Current configuration:"
    Write-Host "  Registry:         " -NoNewline; Write-Host $Config.registry -ForegroundColor Cyan
    Write-Host "  Backend Image:    " -NoNewline; Write-Host $Config.backendImage -ForegroundColor Cyan
    Write-Host "  Backend Version:  " -NoNewline; Write-Host $Config.backendVersion -ForegroundColor Cyan
    Write-Host "  Frontend Image:   " -NoNewline; Write-Host $Config.frontendImage -ForegroundColor Cyan
    Write-Host "  Frontend Version: " -NoNewline; Write-Host $Config.frontendVersion -ForegroundColor Cyan
    Write-Host "  Platform:         " -NoNewline; Write-Host $Config.platform -ForegroundColor Cyan
    Write-Host ""
    
    $reconfigure = Read-YesNo "Reconfigure settings?" $false
    
    if ($reconfigure) {
        Write-Host ""
        $Config.registry = Read-Value "Docker registry (e.g., myusername, ghcr.io/myuser)" $Config.registry
        $Config.backendImage = Read-Value "Backend image name" $Config.backendImage
        $Config.frontendImage = Read-Value "Frontend image name" $Config.frontendImage
        $Config.platform = Read-Platform $Config.platform
        Save-Config $Config
        Write-Success "Configuration saved"
    }
    
    return $Config
}

function Read-Platform {
    param([string]$Current)
    
    Write-Host ""
    Write-Host "Target platform:" -ForegroundColor White
    Write-Host ""
    Write-Host "  [1] linux/amd64    - Intel/AMD servers, most cloud VMs, WSL2"
    Write-Host "  [2] linux/arm64    - Apple Silicon (M1/M2/M3), AWS Graviton"
    Write-Host "  [3] linux/arm64/v8 - Raspberry Pi 4/5 (64-bit)"
    Write-Host "  [4] linux/arm/v7   - Raspberry Pi 2/3, older ARM devices (32-bit)"
    Write-Host "  [5] Custom         - Enter manually"
    Write-Host ""
    Write-Host "  Current: $Current" -ForegroundColor DarkGray
    Write-Host ""
    $choice = Read-Host "Choose [1-5] (default: keep current)"
    
    switch ($choice) {
        "1" { return "linux/amd64" }
        "2" { return "linux/arm64" }
        "3" { return "linux/arm64/v8" }
        "4" { return "linux/arm/v7" }
        "5" { 
            $custom = Read-Host "Enter platform"
            if ([string]::IsNullOrWhiteSpace($custom)) { return $Current }
            return $custom
        }
        default { return $Current }
    }
}

function Read-BumpType {
    Write-Host ""
    Write-Host "Version bump type:" -ForegroundColor White
    Write-Host ""
    Write-Host "  [1] Patch  (0.1.0 -> 0.1.1) - bug fixes"
    Write-Host "  [2] Minor  (0.1.0 -> 0.2.0) - new features"
    Write-Host "  [3] Major  (0.1.0 -> 1.0.0) - breaking changes"
    Write-Host "  [4] None   (rebuild current version)"
    Write-Host ""
    $choice = Read-Host "Choose [1-4] (default: 1)"
    
    switch ($choice) {
        "2" { return "minor" }
        "3" { return "major" }
        "4" { return "none" }
        default { return "patch" }
    }
}

# -----------------------------------------------------------------------------
# Build Functions
# -----------------------------------------------------------------------------
function Test-Docker {
    $ErrorActionPreference = "Continue"
    docker system info 2>&1 | Out-Null
    $result = $LASTEXITCODE -eq 0
    $ErrorActionPreference = "Stop"
    return $result
}

function Test-DockerLogin {
    param([string]$Registry)
    
    # For Docker Hub (no hostname or docker.io), check if logged in
    $ErrorActionPreference = "Continue"
    
    # Try to get auth status - docker config contains auth info
    $configPath = Join-Path $env:USERPROFILE ".docker\config.json"
    if (Test-Path $configPath) {
        $dockerConfig = Get-Content $configPath -Raw | ConvertFrom-Json
        
        # Check for Docker Hub auth (stored as "https://index.docker.io/v1/")
        if ($Registry -notmatch "/") {
            # Simple username = Docker Hub
            if ($dockerConfig.auths.PSObject.Properties.Name -contains "https://index.docker.io/v1/") {
                $ErrorActionPreference = "Stop"
                return $true
            }
        }
        else {
            # Custom registry (e.g., ghcr.io/user)
            $registryHost = ($Registry -split "/")[0]
            foreach ($auth in $dockerConfig.auths.PSObject.Properties.Name) {
                if ($auth -match $registryHost) {
                    $ErrorActionPreference = "Stop"
                    return $true
                }
            }
        }
    }
    
    $ErrorActionPreference = "Stop"
    return $false
}

function Request-DockerLogin {
    param([string]$Registry)
    
    Write-Host ""
    Write-Warning "Not logged in to Docker registry"
    Write-Host ""
    
    # Determine which registry to login to
    if ($Registry -notmatch "/") {
        # Simple username = Docker Hub
        Write-Host "  You need to login to Docker Hub to push images." -ForegroundColor White
        Write-Host ""
        Write-Host "  Run this command:" -ForegroundColor DarkGray
        Write-Host "    docker login" -ForegroundColor Cyan
    }
    elseif ($Registry -match "^ghcr\.io/") {
        # GitHub Container Registry
        Write-Host "  You need to login to GitHub Container Registry." -ForegroundColor White
        Write-Host ""
        Write-Host "  1. Create a Personal Access Token at:" -ForegroundColor DarkGray
        Write-Host "     https://github.com/settings/tokens" -ForegroundColor Cyan
        Write-Host "     (with 'write:packages' scope)" -ForegroundColor DarkGray
        Write-Host ""
        Write-Host "  2. Run this command:" -ForegroundColor DarkGray
        Write-Host "     docker login ghcr.io -u YOUR_GITHUB_USERNAME" -ForegroundColor Cyan
    }
    else {
        # Other registry
        $registryHost = ($Registry -split "/")[0]
        Write-Host "  You need to login to: $registryHost" -ForegroundColor White
        Write-Host ""
        Write-Host "  Run this command:" -ForegroundColor DarkGray
        Write-Host "    docker login $registryHost" -ForegroundColor Cyan
    }
    
    Write-Host ""
    
    $retry = Read-Host "Press Enter after logging in to continue (or 'q' to quit)"
    if ($retry -eq 'q') {
        return $false
    }
    
    return Test-DockerLogin $Registry
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
    
    # Find WebApi directory
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
    
    Write-SubStep "Image: ${fullImage}:$Version"
    
    $buildArgs = @(
        "buildx", "build",
        "--platform", $Config.platform,
        "-t", "${fullImage}:$Version"
    )
    
    if ($TagLatest) {
        $buildArgs += "-t"
        $buildArgs += "${fullImage}:latest"
    }
    
    if ($Push) {
        $buildArgs += "--push"
    }
    else {
        $buildArgs += "--load"
    }
    
    $buildArgs += "-f"
    $buildArgs += "$($webApiDir.Name)/Dockerfile"
    $buildArgs += "."
    
    Push-Location "src\backend"
    try {
        $ErrorActionPreference = "Continue"
        & docker @buildArgs
        $buildExitCode = $LASTEXITCODE
        $ErrorActionPreference = "Stop"
        
        if ($buildExitCode -ne 0) {
            Write-ErrorMessage "Backend build failed"
            return $false
        }
    }
    finally {
        Pop-Location
    }
    
    if ($Push) {
        Write-Success "Backend pushed: ${fullImage}:$Version"
    }
    else {
        Write-Success "Backend built: ${fullImage}:$Version"
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
    
    $dockerfile = Join-Path $ScriptDir "src\frontend\Dockerfile"
    if (-not (Test-Path $dockerfile)) {
        Write-ErrorMessage "Dockerfile not found: $dockerfile"
        return $false
    }
    
    Write-SubStep "Image: ${fullImage}:$Version"
    
    $buildArgs = @(
        "buildx", "build",
        "--platform", $Config.platform,
        "-t", "${fullImage}:$Version"
    )
    
    if ($TagLatest) {
        $buildArgs += "-t"
        $buildArgs += "${fullImage}:latest"
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
        $ErrorActionPreference = "Continue"
        & docker @buildArgs
        $buildExitCode = $LASTEXITCODE
        $ErrorActionPreference = "Stop"
        
        if ($buildExitCode -ne 0) {
            Write-ErrorMessage "Frontend build failed"
            return $false
        }
    }
    finally {
        Pop-Location
    }
    
    if ($Push) {
        Write-Success "Frontend pushed: ${fullImage}:$Version"
    }
    else {
        Write-Success "Frontend built: ${fullImage}:$Version"
    }
    
    return $true
}

# -----------------------------------------------------------------------------
# Main Script
# -----------------------------------------------------------------------------
Write-Header "Deploy"

# Check prerequisites
Write-Step "Checking prerequisites..."
if (-not (Test-Docker)) {
    Write-ErrorMessage "Docker is not running"
    exit 1
}
Write-Success "Docker is running"

# Load configuration
$Config = Get-Config

# Check if registry is default
if ($Config.registry -eq "myusername") {
    Write-Warning "Registry is set to default 'myusername'"
    $Config = Show-ConfigureRegistry $Config
}

# Determine push and latest flags
$DoPush = -not $NoPush
$TagLatest = -not $NoLatest

# Track if we're in interactive mode
$InteractiveMode = $false

# Interactive target selection if not specified
if ([string]::IsNullOrWhiteSpace($Target)) {
    $InteractiveMode = $true
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

# Determine bump type - interactive if not specified via CLI
$BumpType = $null
if ($Major) { $BumpType = "major" }
elseif ($Minor) { $BumpType = "minor" }
elseif ($Patch) { $BumpType = "patch" }
elseif ($NoBump) { $BumpType = "none" }

if ($null -eq $BumpType) {
    if ($InteractiveMode) {
        $BumpType = Read-BumpType
    }
    else {
        $BumpType = "patch"
    }
}

# Calculate new versions based on target
$NewBackendVersion = $Config.backendVersion
$NewFrontendVersion = $Config.frontendVersion

if ($BumpType -ne "none") {
    if ($Target -eq "backend" -or $Target -eq "all") {
        $NewBackendVersion = Get-BumpedVersion $Config.backendVersion $BumpType
    }
    if ($Target -eq "frontend" -or $Target -eq "all") {
        $NewFrontendVersion = Get-BumpedVersion $Config.frontendVersion $BumpType
    }
}

# Summary
Write-Header "Summary"

Write-Host ""
Write-Host "  Deploy Target" -ForegroundColor White
Write-Host "  -------------------------------------"
switch ($Target) {
    "backend" { Write-Host "  Target:   " -NoNewline; Write-Host "Backend API" -ForegroundColor Cyan }
    "frontend" { Write-Host "  Target:   " -NoNewline; Write-Host "Frontend" -ForegroundColor Cyan }
    "all" { Write-Host "  Target:   " -NoNewline; Write-Host "Backend + Frontend" -ForegroundColor Cyan }
}
Write-Host ""
Write-Host "  Versions" -ForegroundColor White
Write-Host "  -------------------------------------"
if ($Target -eq "backend" -or $Target -eq "all") {
    Write-Host "  Backend:  " -NoNewline
    Write-Host $Config.backendVersion -ForegroundColor DarkGray -NoNewline
    Write-Host " -> " -NoNewline
    Write-Host $NewBackendVersion -ForegroundColor Green
}
if ($Target -eq "frontend" -or $Target -eq "all") {
    Write-Host "  Frontend: " -NoNewline
    Write-Host $Config.frontendVersion -ForegroundColor DarkGray -NoNewline
    Write-Host " -> " -NoNewline
    Write-Host $NewFrontendVersion -ForegroundColor Green
}
Write-Host ""
Write-Host "  Options" -ForegroundColor White
Write-Host "  -------------------------------------"
Write-Host "  Push to registry: " -NoNewline
if ($DoPush) { Write-Host "Yes" -ForegroundColor Green } else { Write-Host "No (build only)" -ForegroundColor Yellow }
Write-Host "  Update :latest:   " -NoNewline
if ($TagLatest) { Write-Host "Yes" -ForegroundColor Green } else { Write-Host "No" -ForegroundColor DarkGray }
Write-Host ""

# Confirmation
$proceed = Read-YesNo "Proceed with deployment?" $true
if (-not $proceed) {
    Write-Warning "Aborted by user"
    exit 0
}

# Check Docker login if pushing
if ($DoPush) {
    if (-not (Test-DockerLogin $Config.registry)) {
        $loggedIn = Request-DockerLogin $Config.registry
        if (-not $loggedIn) {
            Write-ErrorMessage "Docker login required to push images"
            exit 1
        }
        Write-Success "Docker login verified"
    }
}

# Execute
Write-Header "Building"

$Failed = $false

if ($Target -eq "backend" -or $Target -eq "all") {
    if (-not (Build-Backend $NewBackendVersion $DoPush $TagLatest $Config)) {
        $Failed = $true
    }
}

if ($Target -eq "frontend" -or $Target -eq "all") {
    if (-not (Build-Frontend $NewFrontendVersion $DoPush $TagLatest $Config)) {
        $Failed = $true
    }
}

if ($Failed) {
    Write-Header "Deploy Failed"
    Write-ErrorMessage "One or more builds failed. Version not updated."
    exit 1
}

# Update versions in config and commit
if ($BumpType -ne "none" -and $DoPush) {
    Write-Step "Updating version..."
    
    # Update only the versions that were deployed
    if ($Target -eq "backend" -or $Target -eq "all") {
        $Config.backendVersion = $NewBackendVersion
    }
    if ($Target -eq "frontend" -or $Target -eq "all") {
        $Config.frontendVersion = $NewFrontendVersion
    }
    Save-Config $Config
    
    # Build commit message
    $commitMsg = "chore: bump"
    if ($Target -eq "backend") {
        $commitMsg = "$commitMsg backend to $NewBackendVersion"
    }
    elseif ($Target -eq "frontend") {
        $commitMsg = "$commitMsg frontend to $NewFrontendVersion"
    }
    else {
        $commitMsg = "$commitMsg backend to $NewBackendVersion, frontend to $NewFrontendVersion"
    }
    
    # Commit the version bump
    $ErrorActionPreference = "Continue"
    git rev-parse --git-dir 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        git add $ConfigFile 2>&1 | Out-Null
        git commit -m $commitMsg 2>&1 | Out-Null
        $ErrorActionPreference = "Stop"
        Write-Success "Version updated (committed)"
    }
    else {
        $ErrorActionPreference = "Stop"
        Write-Success "Version updated"
    }
}

# Complete
Write-Header "Deploy Complete!"

Write-Host ""
Write-Host "  Deployed Images" -ForegroundColor White
Write-Host "  -------------------------------------"
if ($Target -eq "backend" -or $Target -eq "all") {
    Write-Host "  $($Config.registry)/$($Config.backendImage):$NewBackendVersion" -ForegroundColor Cyan
}
if ($Target -eq "frontend" -or $Target -eq "all") {
    Write-Host "  $($Config.registry)/$($Config.frontendImage):$NewFrontendVersion" -ForegroundColor Cyan
}
Write-Host ""

}
finally {
    Pop-Location
}
