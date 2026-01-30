<#
.SYNOPSIS
    Web API Template - Project Initialization Script

.DESCRIPTION
    Initializes a new project from the Web API template by:
    - Renaming the project from MyProject to your chosen name
    - Configuring ports for Docker services
    - Optionally creating initial database migration
    - Optionally committing changes to git

.PARAMETER Name
    The new project name (e.g., MyAwesomeApi). Must start with uppercase letter.

.PARAMETER Port
    Base port for Docker services. Default is 13000.
    Frontend: PORT, API: PORT+2, Database: PORT+4, Redis: PORT+6, Seq: PORT+8

.PARAMETER Yes
    Accept all defaults without prompting (non-interactive mode).

.PARAMETER NoMigration
    Skip creating the initial database migration.

.PARAMETER NoCommit
    Skip git commits.

.PARAMETER NoDocker
    Skip starting docker compose after setup.

.PARAMETER KeepScripts
    Keep init.ps1 and init.sh after completion.

.EXAMPLE
    .\init.ps1
    # Interactive mode - prompts for all options

.EXAMPLE
    .\init.ps1 -Name "MyAwesomeApi" -Port 14000 -Yes
    # Non-interactive mode with custom name and port

.EXAMPLE
    .\init.ps1 -Name "TodoApp" -Yes -NoDocker
    # Non-interactive, don't start docker after setup
#>

param (
    [Alias("n")]
    [string]$Name,

    [Alias("p")]
    [int]$Port = 13000,

    [Alias("y")]
    [switch]$Yes,

    [switch]$NoMigration,
    [switch]$NoCommit,
    [switch]$NoDocker,
    [switch]$KeepScripts
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

    if ($Yes -and -not [string]::IsNullOrWhiteSpace($Default)) {
        return $Default
    }

    $prompt = if ([string]::IsNullOrWhiteSpace($Default)) { $Question } else { "$Question [$Default]" }
    $response = Read-Host $prompt

    if ([string]::IsNullOrWhiteSpace($response)) {
        return $Default
    }
    return $response
}

function Test-Prerequisites {
    $missing = @()

    if (-not (Get-Command git -ErrorAction SilentlyContinue)) { $missing += "git" }
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { $missing += "dotnet" }
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { $missing += "docker" }

    if ($missing.Count -gt 0) {
        Write-ErrorMessage "Missing required tools: $($missing -join ', ')"
        Write-Host "Please install them before running this script."
        exit 1
    }
}

function Test-ProjectName {
    param([string]$ProjectName)

    if ([string]::IsNullOrWhiteSpace($ProjectName)) {
        Write-ErrorMessage "Project name cannot be empty"
        return $false
    }

    if ($ProjectName -notmatch "^[A-Z][a-zA-Z0-9]*$") {
        Write-ErrorMessage "Project name must start with uppercase letter and contain only alphanumeric characters"
        Write-Info "Example: MyAwesomeApi, TodoApp, WebApi"
        return $false
    }

    return $true
}

function Test-Port {
    param([int]$PortNumber)

    if ($PortNumber -lt 1024 -or $PortNumber -gt 65530) {
        Write-ErrorMessage "Port must be between 1024 and 65530"
        return $false
    }

    return $true
}

function Set-FileContent {
    param(
        [string]$Path,
        [string]$Content
    )
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $Content, $utf8NoBom)
}

# -----------------------------------------------------------------------------
# Main Script
# -----------------------------------------------------------------------------
Clear-Host
Write-Header "Web API Template Initialization"

# Check prerequisites
Write-Step "Checking prerequisites..."
Test-Prerequisites
Write-Success "All prerequisites found (git, dotnet, docker)"

# -----------------------------------------------------------------------------
# Configuration Phase
# -----------------------------------------------------------------------------
Write-Header "Configuration"

# Project Name
while ($true) {
    if ([string]::IsNullOrWhiteSpace($Name)) {
        $Name = Read-Value "Project name (e.g., MyAwesomeApi)"
    }

    if (Test-ProjectName $Name) {
        break
    }
    $Name = ""
}

# Base Port
while ($true) {
    if (-not $Yes) {
        $portInput = Read-Value "Base port" $Port.ToString()
        $Port = [int]$portInput
    }

    if (Test-Port $Port) {
        break
    }
}

# Calculate derived ports
$FrontendPort = $Port
$ApiPort = $Port + 2
$DbPort = $Port + 4
$RedisPort = $Port + 6
$SeqPort = $Port + 8

# Convert PascalCase to kebab-case (MyAwesomeApi -> my-awesome-api)
function ConvertTo-KebabCase {
    param([string]$Text)
    ($Text -creplace '([a-z])([A-Z])', '$1-$2').ToLower()
}
$ProjectSlug = ConvertTo-KebabCase $Name

# Additional options
Write-Host ""
Write-Info "Additional options:"

$CreateMigration = if ($NoMigration) { $false } else { Read-YesNo "  Create fresh Initial migration?" $true }
$DoCommit = if ($NoCommit) { $false } else { Read-YesNo "  Auto-commit changes to git?" $true }
$StartDocker = if ($NoDocker) { $false } else { Read-YesNo "  Start docker compose after setup?" $false }
$DeleteScripts = if ($KeepScripts) { $false } else { Read-YesNo "  Delete init scripts when done?" $true }

# -----------------------------------------------------------------------------
# Summary and Confirmation
# -----------------------------------------------------------------------------
Write-Header "Summary"

Write-Host ""
Write-Host "  Project Configuration" -ForegroundColor White
Write-Host "  -------------------------------------"
Write-Host "  Project Name:     " -NoNewline; Write-Host $Name -ForegroundColor Green
Write-Host "  Docker Slug:      " -NoNewline; Write-Host $ProjectSlug -ForegroundColor Green
Write-Host ""
Write-Host "  Port Allocation" -ForegroundColor White
Write-Host "  -------------------------------------"
Write-Host "  Frontend:         " -NoNewline; Write-Host $FrontendPort -ForegroundColor Cyan
Write-Host "  API:              " -NoNewline; Write-Host $ApiPort -ForegroundColor Cyan
Write-Host "  Database:         " -NoNewline; Write-Host $DbPort -ForegroundColor Cyan
Write-Host "  Redis:            " -NoNewline; Write-Host $RedisPort -ForegroundColor Cyan
Write-Host "  Seq:              " -NoNewline; Write-Host $SeqPort -ForegroundColor Cyan
Write-Host ""
Write-Host "  Actions" -ForegroundColor White
Write-Host "  -------------------------------------"
Write-Host "  Create migration: " -NoNewline
if ($CreateMigration) { Write-Host "Yes" -ForegroundColor Green } else { Write-Host "No" -ForegroundColor DarkGray }
Write-Host "  Git commits:      " -NoNewline
if ($DoCommit) { Write-Host "Yes" -ForegroundColor Green } else { Write-Host "No" -ForegroundColor DarkGray }
Write-Host "  Start docker:     " -NoNewline
if ($StartDocker) { Write-Host "Yes" -ForegroundColor Green } else { Write-Host "No" -ForegroundColor DarkGray }
Write-Host "  Delete scripts:   " -NoNewline
if ($DeleteScripts) { Write-Host "Yes" -ForegroundColor Green } else { Write-Host "No" -ForegroundColor DarkGray }
Write-Host ""

$proceed = Read-YesNo "Proceed with initialization?" $true
if (-not $proceed) {
    Write-Warning "Aborted by user"
    exit 0
}

# -----------------------------------------------------------------------------
# Execution Phase
# -----------------------------------------------------------------------------
Write-Header "Executing"

$OldName = "MyProject"
$OldNameLower = "myproject"
$NewName = $Name
$NewNameLower = $Name.ToLower()

# Step 1: Update Ports (substitute placeholders across all files)
Write-Step "Updating port configuration..."

$frontendEnvExample = Join-Path $ScriptDir "src\frontend\.env.example"
$frontendEnvLocal = Join-Path $ScriptDir "src\frontend\.env.local"
if (Test-Path $frontendEnvExample) {
    Copy-Item $frontendEnvExample $frontendEnvLocal -Force
    Write-SubStep "Created frontend .env.local from .env.example"
}

Write-SubStep "Replacing port placeholders..."
$files = Get-ChildItem -Path $ScriptDir -Recurse -File | Where-Object {
    $_.FullName -notmatch "[\\/]\.git[\\/]" -and
    $_.FullName -notmatch "[\\/]bin[\\/]" -and
    $_.FullName -notmatch "[\\/]obj[\\/]" -and
    $_.FullName -notmatch "[\\/]node_modules[\\/]" -and
    $_.Name -ne "init.ps1" -and
    $_.Name -ne "init.sh" -and
    $_.Extension -notmatch "\.(png|jpg|jpeg|ico|gif|woff|woff2|ttf|eot)$"
}

foreach ($file in $files) {
    try {
        $content = [System.IO.File]::ReadAllText($file.FullName)
        $originalContent = $content

        if ($content -match "\{INIT_FRONTEND_PORT\}|\{INIT_API_PORT\}|\{INIT_DB_PORT\}|\{INIT_REDIS_PORT\}|\{INIT_SEQ_PORT\}|\{INIT_PROJECT_SLUG\}") {
            $content = $content -replace "\{INIT_FRONTEND_PORT\}", $FrontendPort
            $content = $content -replace "\{INIT_API_PORT\}", $ApiPort
            $content = $content -replace "\{INIT_DB_PORT\}", $DbPort
            $content = $content -replace "\{INIT_REDIS_PORT\}", $RedisPort
            $content = $content -replace "\{INIT_SEQ_PORT\}", $SeqPort
            $content = $content -replace "\{INIT_PROJECT_SLUG\}", $ProjectSlug

            if ($content -ne $originalContent) {
                Set-FileContent $file.FullName $content
            }
        }
    }
    catch {
        # Skip files that can't be read
    }
}

Write-Success "Port configuration complete"

# Commit port configuration changes
if ($DoCommit) {
    Write-Step "Committing port configuration..."
    $ErrorActionPreference = "Continue"
    $null = git add . 2>&1
    $null = git commit -m "chore: configure project (slug: $ProjectSlug, ports: $FrontendPort/$ApiPort/$DbPort/$RedisPort/$SeqPort)" 2>&1
    $ErrorActionPreference = "Stop"
    Write-Success "Port configuration committed"
}

# Step 2: Rename Project (skip if name is already MyProject)
if ($NewName -eq "MyProject") {
    Write-Step "Skipping rename (project name is already MyProject)"
} else {
    Write-Step "Renaming project..."

    Write-SubStep "Replacing text content..."
    $files = Get-ChildItem -Path $ScriptDir -Recurse -File | Where-Object {
        $_.FullName -notmatch "[\\/]\.git[\\/]" -and
        $_.FullName -notmatch "[\\/]bin[\\/]" -and
        $_.FullName -notmatch "[\\/]obj[\\/]" -and
        $_.FullName -notmatch "[\\/]node_modules[\\/]" -and
        $_.Name -ne "init.ps1" -and
        $_.Name -ne "init.sh" -and
        $_.Extension -notmatch "\.(png|jpg|jpeg|ico|gif|woff|woff2|ttf|eot)$"
    }

    foreach ($file in $files) {
        try {
            $content = [System.IO.File]::ReadAllText($file.FullName)
            $originalContent = $content

            if ($content -match $OldName -or $content -match $OldNameLower) {
                $content = $content -replace $OldName, $NewName
                $content = $content -replace $OldNameLower, $NewNameLower

                if ($content -ne $originalContent) {
                    Set-FileContent $file.FullName $content
                }
            }
        }
        catch {
            # Skip files that can't be read (binary, locked, etc.)
        }
    }

    Write-SubStep "Renaming files and directories..."
    $items = Get-ChildItem -Path $ScriptDir -Recurse | Where-Object {
        $_.FullName -notmatch "[\\/]\.git[\\/]" -and
        $_.FullName -notmatch "[\\/]bin[\\/]" -and
        $_.FullName -notmatch "[\\/]obj[\\/]" -and
        $_.FullName -notmatch "[\\/]node_modules[\\/]" -and
        $_.Name -ne "init.ps1" -and
        $_.Name -ne "init.sh" -and
        ($_.Name -match $OldName -or $_.Name -match $OldNameLower)
    } | Sort-Object { $_.FullName.Length } -Descending

    foreach ($item in $items) {
        $newItemName = $item.Name -replace $OldName, $NewName
        $newItemName = $newItemName -replace $OldNameLower, $NewNameLower

        if ($newItemName -ne $item.Name) {
            try {
                Rename-Item -Path $item.FullName -NewName $newItemName -ErrorAction Stop
            }
            catch {
                # Item may have already been moved as part of parent directory rename
            }
        }
    }

    Write-Success "Project renamed to $NewName"

    # Step 3: Git Commit (Rename)
    if ($DoCommit) {
        Write-Step "Committing rename changes..."
        $ErrorActionPreference = "Continue"
        $null = git add . 2>&1
        $null = git commit -m "chore: rename project from $OldName to $NewName" 2>&1
        $ErrorActionPreference = "Stop"
        Write-Success "Changes committed"
    }
}

# Step 4: Create Migration
if ($CreateMigration) {
    Write-Step "Creating initial migration..."

    $migrationDir = Join-Path $ScriptDir "src/backend/$NewName.Infrastructure/Features/Postgres/Migrations"

    if (Test-Path $migrationDir) {
        Write-SubStep "Clearing existing migrations..."
        Remove-Item "$migrationDir/*" -Recurse -Force -ErrorAction SilentlyContinue
    }
    else {
        New-Item -ItemType Directory -Path $migrationDir -Force | Out-Null
    }

    # Temporarily allow errors for external commands
    $ErrorActionPreference = "Continue"
    $migrationFailed = $false

    Write-SubStep "Restoring dotnet tools..."
    # Try tool restore with explicit config file since root may not have NuGet sources
    $output = dotnet tool restore --configfile "src/backend/nuget.config" 2>&1
    if ($LASTEXITCODE -ne 0) {
        # Fallback: try without config (maybe global sources exist)
        $output = dotnet tool restore 2>&1
        if ($LASTEXITCODE -ne 0) {
            $migrationFailed = $true
            Write-ErrorMessage "Failed to restore dotnet tools"
            Write-Host $output -ForegroundColor DarkGray
        }
    }

    if (-not $migrationFailed) {
        Write-SubStep "Restoring dependencies..."
        $output = dotnet restore "src/backend/$NewName.WebApi" 2>&1
        if ($LASTEXITCODE -ne 0) {
            $migrationFailed = $true
            Write-ErrorMessage "Restore failed"
            Write-Host $output -ForegroundColor DarkGray
        }
    }

    if (-not $migrationFailed) {
        Write-SubStep "Building project..."
        $output = dotnet build "src/backend/$NewName.WebApi" --no-restore -v q 2>&1
        if ($LASTEXITCODE -ne 0) {
            $migrationFailed = $true
            Write-ErrorMessage "Build failed"
            Write-Host $output -ForegroundColor DarkGray
        }
    }

    if (-not $migrationFailed) {
        Write-SubStep "Running ef migrations add..."
        $output = dotnet ef migrations add Initial --project "src/backend/$NewName.Infrastructure" --startup-project "src/backend/$NewName.WebApi" --output-dir Features/Postgres/Migrations --no-build 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            $migrationFailed = $true
            Write-ErrorMessage "Migration creation failed"
            Write-Host $output -ForegroundColor DarkGray
        }
    }

    $ErrorActionPreference = "Stop"

    if ($migrationFailed) {
        Write-Warning "Migration step failed. You can create it manually later with:"
        Write-Host "  dotnet ef migrations add Initial --project src/backend/$NewName.Infrastructure --startup-project src/backend/$NewName.WebApi --output-dir Features/Postgres/Migrations" -ForegroundColor DarkGray
    }
    else {
        Write-Success "Migration 'Initial' created"

        if ($DoCommit) {
            Write-SubStep "Committing migration..."
            $ErrorActionPreference = "Continue"
            $null = git add . 2>&1
            $null = git commit -m "chore: add initial database migration" 2>&1
            $ErrorActionPreference = "Stop"
            Write-Success "Migration committed"
        }
    }
}

# Step 5: Delete init scripts
if ($DeleteScripts) {
    Write-Step "Cleaning up init scripts..."
    
    $initPs1 = Join-Path $ScriptDir "init.ps1"
    $initSh = Join-Path $ScriptDir "init.sh"
    
    $ErrorActionPreference = "Continue"
    
    # Stage both files for deletion in git BEFORE physically removing them
    # This ensures both show up in the commit
    if ($DoCommit) {
        # Use git rm to stage deletions (files will be removed from index but we handle physical deletion separately)
        if (Test-Path $initSh) {
            $null = git rm -f "init.sh" 2>&1
        }
        # Stage init.ps1 for deletion but keep it on disk for now (script is still running)
        $null = git rm --cached "init.ps1" 2>&1
        
        $null = git commit -m "chore: remove initialization scripts" 2>&1
        Write-Success "Deletion committed to git"
    }
    else {
        # No commit - just delete init.sh directly
        if (Test-Path $initSh) {
            Remove-Item $initSh -Force
        }
    }
    
    $ErrorActionPreference = "Stop"
    
    Write-Success "Init scripts will be removed"
    
    # Schedule self-deletion after script exits
    # Use -EncodedCommand to avoid escaping issues with paths
    $cleanupScript = "Start-Sleep -Seconds 2; Remove-Item -LiteralPath '$initPs1' -Force -ErrorAction SilentlyContinue"
    $encodedCmd = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($cleanupScript))
    Start-Process powershell -ArgumentList "-NoProfile", "-WindowStyle", "Hidden", "-EncodedCommand", $encodedCmd
}

# Step 6: Start Docker
if ($StartDocker) {
    Write-Step "Starting Docker containers..."
    $ErrorActionPreference = "Continue"
    docker compose -f docker-compose.local.yml up -d --build
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Docker failed to start. Is Docker Desktop running?"
        Write-Info "You can start containers manually later with:"
        Write-Host "  docker compose -f docker-compose.local.yml up -d --build" -ForegroundColor DarkGray
    }
    else {
        Write-Success "Docker containers started"
    }
    $ErrorActionPreference = "Stop"
}

# -----------------------------------------------------------------------------
# Complete!
# -----------------------------------------------------------------------------
Write-Header "Setup Complete!"

Write-Host ""
Write-Host "  Your project is ready!" -ForegroundColor White
Write-Host ""
Write-Host "  Quick Start" -ForegroundColor White
Write-Host "  -------------------------------------"
Write-Host "  # Start the development environment" -ForegroundColor DarkGray
Write-Host "  docker compose -f docker-compose.local.yml up -d --build"
Write-Host ""
Write-Host "  # Or run the API directly" -ForegroundColor DarkGray
Write-Host "  cd src\backend\$NewName.WebApi"
Write-Host "  dotnet run"
Write-Host ""
Write-Host "  URLs" -ForegroundColor White
Write-Host "  -------------------------------------"
Write-Host "  Frontend:  " -NoNewline; Write-Host "http://localhost:$FrontendPort" -ForegroundColor Cyan
Write-Host "  API:       " -NoNewline; Write-Host "http://localhost:$ApiPort" -ForegroundColor Cyan
Write-Host "  API Docs:  " -NoNewline; Write-Host "http://localhost:$ApiPort/scalar" -ForegroundColor Cyan
Write-Host "  Seq:       " -NoNewline; Write-Host "http://localhost:$SeqPort" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Happy coding!" -ForegroundColor DarkGray
Write-Host ""

}
finally {
    Pop-Location
}
