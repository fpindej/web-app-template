<#
.SYNOPSIS
    Project Initialization Script

.DESCRIPTION
    Initializes a new project from the Web API template by:
    - Renaming the project from MyProject to your chosen name
    - Configuring ports for services
    - Optionally creating initial database migration
    - Optionally committing changes to git

.PARAMETER Name
    The new project name (e.g., MyAwesomeApi). Must start with uppercase letter.

.PARAMETER Port
    Base port for services. Default is 13000.
    Frontend: PORT, API: PORT+2
    (Infrastructure ports are managed automatically by Aspire)

.PARAMETER Yes
    Accept all defaults without prompting (non-interactive mode).

.PARAMETER NoMigration
    Skip creating the initial database migration.

.PARAMETER NoBuild
    Skip building and running tests.

.PARAMETER NoCommit
    Skip git commits.

.PARAMETER NoAspire
    Don't launch Aspire after setup.

.PARAMETER Email
    Superuser email address. Default is superuser@test.com.

.PARAMETER Password
    Superuser password. Default is Superuser123!.

.EXAMPLE
    .\init.ps1
    # Interactive mode - prompts for all options

.EXAMPLE
    .\init.ps1 -Name "MyAwesomeApi" -Port 14000 -Yes
    # Non-interactive mode with custom name and port

.EXAMPLE
    .\init.ps1 -Name "TodoApp" -Yes
    # Non-interactive with defaults

.EXAMPLE
    .\init.ps1 -Name "MyApi" -Email "me@example.com" -Password "MyPass123!"
    # Custom Superuser credentials
#>

param (
    [Alias("n")]
    [string]$Name,

    [Alias("p")]
    [int]$Port = 13000,

    [Alias("y")]
    [switch]$Yes,

    [Alias("e")]
    [string]$Email = "superuser@test.com",

    [string]$Password = "Superuser123!",

    [switch]$NoMigration,
    [switch]$NoBuild,
    [switch]$NoCommit,
    [switch]$NoAspire
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

function Write-WarnMsg {
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

    if (Get-Command docker -ErrorAction SilentlyContinue) {
        $null = docker info 2>$null
        if ($LASTEXITCODE -ne 0) {
            $missing += "docker (installed but not running - start Docker Desktop)"
        }
    } else {
        $missing += "docker"
    }

    if (-not (Get-Command node -ErrorAction SilentlyContinue)) { $missing += "node" }
    if (-not (Get-Command pnpm -ErrorAction SilentlyContinue)) { $missing += "pnpm (run: corepack enable)" }

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

    if ($PortNumber -lt 1024 -or $PortNumber -gt 65527) {
        Write-ErrorMessage "Port must be between 1024 and 65527"
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

function ConvertTo-KebabCase {
    param([string]$Text)
    ($Text -creplace '([a-z])([A-Z])', '$1-$2').ToLower()
}

# -----------------------------------------------------------------------------
# Interactive Checklist
# -----------------------------------------------------------------------------
# Renders a toggleable checklist. User presses 1-N to toggle, Enter to confirm.
function Read-Checklist {
    param(
        [string[]]$Options,
        [bool[]]$Defaults
    )

    # Non-interactive: just use defaults
    if ($Yes) {
        return $Defaults
    }

    $selected = [bool[]]::new($Options.Count)
    for ($i = 0; $i -lt $Options.Count; $i++) {
        $selected[$i] = $Defaults[$i]
    }

    $firstDraw = $true

    while ($true) {
        # Clear previous draw (except first time)
        if (-not $firstDraw) {
            $linesToClear = $Options.Count + 3
            for ($j = 0; $j -lt $linesToClear; $j++) {
                [Console]::SetCursorPosition(0, [Console]::CursorTop - 1)
                [Console]::Write((" " * [Console]::WindowWidth))
                [Console]::SetCursorPosition(0, [Console]::CursorTop)
            }
        }
        $firstDraw = $false

        Write-Host ""
        Write-Host "  Press 1-$($Options.Count) to toggle, Enter to confirm:" -ForegroundColor White
        Write-Host ""

        for ($i = 0; $i -lt $Options.Count; $i++) {
            $num = $i + 1
            if ($selected[$i]) {
                Write-Host "  " -NoNewline
                Write-Host "[$num]" -ForegroundColor Green -NoNewline
                Write-Host " " -NoNewline
                Write-Host $Options[$i] -ForegroundColor Green
            }
            else {
                Write-Host "  " -NoNewline
                Write-Host "[$num]" -ForegroundColor DarkGray -NoNewline
                Write-Host " " -NoNewline
                Write-Host $Options[$i] -ForegroundColor DarkGray
            }
        }

        # Single keypress - no Enter needed to toggle
        $key = [Console]::ReadKey($true)

        if ($key.Key -eq [ConsoleKey]::Enter) {
            Write-Host ""
            return $selected
        }

        $num = 0
        if ([int]::TryParse($key.KeyChar, [ref]$num) -and $num -ge 1 -and $num -le $Options.Count) {
            $idx = $num - 1
            $selected[$idx] = -not $selected[$idx]
        }
    }
}

# -----------------------------------------------------------------------------
# Main Script
# -----------------------------------------------------------------------------
$startTime = Get-Date

Write-Host ""
Write-Header "Project Initialization"

# Verify we're in the project root
if (-not (Test-Path (Join-Path $ScriptDir "src/backend")) -or -not (Test-Path (Join-Path $ScriptDir "src/frontend"))) {
    Write-ErrorMessage "This script must be run from the project root directory."
    Write-Info "Expected to find src/backend and src/frontend directories."
    exit 1
}

# Check prerequisites
Write-Step "Checking prerequisites..."
Test-Prerequisites
Write-Success "All prerequisites found (git, dotnet, docker, node)"

# -----------------------------------------------------------------------------
# Step 1: Project Name
# -----------------------------------------------------------------------------
Write-Step "Project setup"
Write-Host ""

while ($true) {
    if ([string]::IsNullOrWhiteSpace($Name)) {
        $Name = Read-Value "Project name (PascalCase, e.g. MyAwesomeApi)"
    }

    if (Test-ProjectName $Name) {
        break
    }
    $Name = ""
}

# -----------------------------------------------------------------------------
# Step 2: Base Port
# -----------------------------------------------------------------------------
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

# Show port allocation
Write-Host ""
Write-Host "  Port allocation" -ForegroundColor White
Write-Host "  -------------------------------------"
Write-Host "  Frontend:     " -NoNewline; Write-Host $FrontendPort -ForegroundColor Cyan
Write-Host "  API:          " -NoNewline; Write-Host $ApiPort -ForegroundColor Cyan

# Superuser credentials
Write-Host ""
$Email = Read-Value "Superuser email" $Email
$Password = Read-Value "Superuser password" $Password

$ProjectSlug = ConvertTo-KebabCase $Name

# -----------------------------------------------------------------------------
# Step 3: Options Checklist
# -----------------------------------------------------------------------------
$checklistOptions = @()
$checklistDefaults = @()
$checklistMap = @()

if (-not $NoMigration) {
    $checklistOptions += "Create initial database migration"
    $checklistDefaults += $true
    $checklistMap += "migration"
}

if (-not $NoBuild) {
    $checklistOptions += "Build and run tests"
    $checklistDefaults += $true
    $checklistMap += "build"
}

if (-not $NoCommit) {
    $checklistOptions += "Auto-commit changes to git"
    $checklistDefaults += $true
    $checklistMap += "commit"
}

if (-not $NoAspire) {
    $checklistOptions += "Launch Aspire after setup"
    $checklistDefaults += $true
    $checklistMap += "aspire"
}

# Initialize from CLI flags
$CreateMigration = -not $NoMigration
$BuildTest = -not $NoBuild
$DoCommit = -not $NoCommit
$StartAspire = -not $NoAspire

# Only show checklist if there are options to configure
if ($checklistOptions.Count -gt 0) {
    $results = Read-Checklist -Options $checklistOptions -Defaults $checklistDefaults

    for ($i = 0; $i -lt $checklistMap.Count; $i++) {
        switch ($checklistMap[$i]) {
            "migration" { $CreateMigration = $results[$i] }
            "build" { $BuildTest = $results[$i] }
            "commit" { $DoCommit = $results[$i] }
            "aspire" { $StartAspire = $results[$i] }
        }
    }
}

# -----------------------------------------------------------------------------
# Summary and Confirmation
# -----------------------------------------------------------------------------
Write-Header "Summary"

Write-Host ""
Write-Host "  Project" -ForegroundColor White
Write-Host "  -------------------------------------"
Write-Host "  Name:             " -NoNewline; Write-Host $Name -ForegroundColor Green
Write-Host "  Slug:             " -NoNewline; Write-Host $ProjectSlug -ForegroundColor Green
Write-Host ""
Write-Host "  Ports" -ForegroundColor White
Write-Host "  -------------------------------------"
Write-Host "  Frontend:         " -NoNewline; Write-Host $FrontendPort -ForegroundColor Cyan
Write-Host "  API:              " -NoNewline; Write-Host $ApiPort -ForegroundColor Cyan
Write-Host ""
Write-Host "  Superuser" -ForegroundColor White
Write-Host "  -------------------------------------"
Write-Host "  Email:            " -NoNewline; Write-Host $Email -ForegroundColor Cyan
Write-Host "  Password:         " -NoNewline; Write-Host $Password -ForegroundColor Cyan
Write-Host ""
Write-Host "  Options" -ForegroundColor White
Write-Host "  -------------------------------------"
Write-Host "  Create migration: " -NoNewline
if ($CreateMigration) { Write-Host "Yes" -ForegroundColor Green } else { Write-Host "No" -ForegroundColor DarkGray }
Write-Host "  Build and test:   " -NoNewline
if ($BuildTest) { Write-Host "Yes" -ForegroundColor Green } else { Write-Host "No" -ForegroundColor DarkGray }
Write-Host "  Git commits:      " -NoNewline
if ($DoCommit) { Write-Host "Yes" -ForegroundColor Green } else { Write-Host "No" -ForegroundColor DarkGray }
Write-Host "  Launch Aspire:    " -NoNewline
if ($StartAspire) { Write-Host "Yes" -ForegroundColor Green } else { Write-Host "No" -ForegroundColor DarkGray }
Write-Host ""

$proceed = Read-YesNo "Proceed with initialization?" $true
if (-not $proceed) {
    Write-WarnMsg "Aborted by user"
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

# Generate random secrets
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$jwtBytes = New-Object byte[] 64
$rng.GetBytes($jwtBytes)
$encBytes = New-Object byte[] 64
$rng.GetBytes($encBytes)
$rng.Dispose()
$JwtSecret = [Convert]::ToBase64String($jwtBytes) -replace '[/+=]', '' | ForEach-Object { $_.Substring(0, [Math]::Min(64, $_.Length)) }
$EncryptionKey = [Convert]::ToBase64String($encBytes) -replace '[/+=]', '' | ForEach-Object { $_.Substring(0, [Math]::Min(64, $_.Length)) }

Write-SubStep "Replacing placeholders..."
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

        if ($content -match "\{INIT_FRONTEND_PORT\}|\{INIT_API_PORT\}|\{INIT_PROJECT_SLUG\}|\{INIT_JWT_SECRET\}|\{INIT_ENCRYPTION_KEY\}|\{INIT_SUPERUSER_EMAIL\}|\{INIT_SUPERUSER_PASSWORD\}") {
            $content = $content -replace "\{INIT_FRONTEND_PORT\}", $FrontendPort
            $content = $content -replace "\{INIT_API_PORT\}", $ApiPort
            $content = $content -replace "\{INIT_PROJECT_SLUG\}", $ProjectSlug
            $content = $content -replace "\{INIT_JWT_SECRET\}", $JwtSecret
            $content = $content -replace "\{INIT_ENCRYPTION_KEY\}", $EncryptionKey
            $content = $content -replace "\{INIT_SUPERUSER_EMAIL\}", $Email
            $content = $content -replace "\{INIT_SUPERUSER_PASSWORD\}", $Password

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
    $null = git commit -m "chore: configure project (slug: $ProjectSlug, ports: $FrontendPort/$ApiPort)" 2>&1
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

    $migrationDir = Join-Path $ScriptDir "src/backend/$NewName.Infrastructure/Persistence/Migrations"

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
        $output = dotnet ef migrations add Initial --project "src/backend/$NewName.Infrastructure" --startup-project "src/backend/$NewName.WebApi" --output-dir Persistence/Migrations --no-build 2>&1

        if ($LASTEXITCODE -ne 0) {
            $migrationFailed = $true
            Write-ErrorMessage "Migration creation failed"
            Write-Host $output -ForegroundColor DarkGray
        }
    }

    $ErrorActionPreference = "Stop"

    if ($migrationFailed) {
        Write-WarnMsg "Migration step failed. You can create it manually later with:"
        Write-Host "  dotnet ef migrations add Initial --project src/backend/$NewName.Infrastructure --startup-project src/backend/$NewName.WebApi --output-dir Persistence/Migrations" -ForegroundColor DarkGray
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

# Step 5: Build and Run Tests
if ($BuildTest) {
    Write-Step "Building solution..."
    $ErrorActionPreference = "Continue"
    $output = dotnet build "src/backend/$NewName.slnx" -c Debug --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Build succeeded"
    } else {
        Write-ErrorMessage "Build failed"
        Write-Host $output -ForegroundColor DarkGray
    }

    Write-Step "Running tests..."
    $output = dotnet test "src/backend/$NewName.slnx" -c Release --verbosity quiet --no-restore 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "All tests passed"
    } else {
        Write-WarnMsg "Some tests failed - check output above"
        Write-Host $output -ForegroundColor DarkGray
    }
    $ErrorActionPreference = "Stop"
}

# Step 6: Delete template-specific files (always, fire and forget)
Write-Step "Cleaning up template files..."

$initPs1 = Join-Path $ScriptDir "init.ps1"
$templateFiles = @(
    "init.sh"
    ".github/workflows/claude.yml"
    ".github/workflows/claude-code-review.yml"
)

$templateDirs = @(
    "docs/sessions"
)

$ErrorActionPreference = "Continue"

# Stage files for deletion in git BEFORE physically removing them
if ($DoCommit) {
    foreach ($f in $templateFiles) {
        $fullPath = Join-Path $ScriptDir $f
        if (Test-Path $fullPath) {
            $null = git rm -f $f 2>&1
        }
    }
    foreach ($d in $templateDirs) {
        $fullPath = Join-Path $ScriptDir $d
        if (Test-Path $fullPath) {
            $null = git rm -rf $d 2>&1
        }
    }
    # Stage init.ps1 for deletion but keep it on disk for now (script is still running)
    $null = git rm --cached "init.ps1" 2>&1

    $null = git commit -m "chore: remove template-specific files" 2>&1
    Write-Success "Deletion committed to git"
}
else {
    foreach ($f in $templateFiles) {
        $fullPath = Join-Path $ScriptDir $f
        if (Test-Path $fullPath) {
            Remove-Item $fullPath -Force
        }
    }
    foreach ($d in $templateDirs) {
        $fullPath = Join-Path $ScriptDir $d
        if (Test-Path $fullPath) {
            Remove-Item $fullPath -Recurse -Force
        }
    }
}

$ErrorActionPreference = "Stop"

Write-Success "Template files removed"

# Schedule self-deletion after script exits (cross-platform)
$pwshExe = (Get-Process -Id $PID).Path
$cleanupScript = "Start-Sleep -Seconds 2; Remove-Item -LiteralPath '$initPs1' -Force -ErrorAction SilentlyContinue"
$encodedCmd = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($cleanupScript))
$startArgs = @{
    FilePath     = $pwshExe
    ArgumentList = "-NoProfile", "-EncodedCommand", $encodedCmd
}
if ($env:OS -eq 'Windows_NT') { $startArgs.WindowStyle = "Hidden" }
Start-Process @startArgs

# -----------------------------------------------------------------------------
# Complete!
# -----------------------------------------------------------------------------
Write-Header "Setup Complete!"

$elapsed = [math]::Round(((Get-Date) - $startTime).TotalSeconds)

if ($StartAspire) {
    Write-Host ""
    Write-Host "  Your project is ready!" -ForegroundColor White
    Write-Host ""
    Write-Host "  Completed in ${elapsed}s" -ForegroundColor DarkGray
    Write-Host ""
    Write-Step "Launching Aspire..."
    Write-Host "  Opening Aspire Dashboard in your browser. Press Ctrl+C to stop." -ForegroundColor DarkGray
    Write-Host ""
    Start-Job -ScriptBlock {
        for ($i = 0; $i -lt 30; $i++) {
            try {
                $null = Invoke-WebRequest -Uri "http://localhost:15244" -UseBasicParsing -TimeoutSec 1 -ErrorAction Stop
                Start-Process "http://localhost:15244"
                break
            } catch { Start-Sleep -Seconds 1 }
        }
    } | Out-Null
    $env:DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS = "true"
    & dotnet run --project "src\backend\$NewName.AppHost"
}
else {
    Write-Host ""
    Write-Host "  Your project is ready!" -ForegroundColor White
    Write-Host ""
    Write-Host "  Quick Start" -ForegroundColor White
    Write-Host "  -------------------------------------"
    Write-Host "  dotnet run --project src\backend\$NewName.AppHost"
    Write-Host ""
    Write-Host "  The Aspire Dashboard URL appears in the console." -ForegroundColor DarkGray
    Write-Host "  All service URLs (API, pgAdmin, MinIO) are visible in the Dashboard." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "  Completed in ${elapsed}s" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "  Happy coding!" -ForegroundColor DarkGray
    Write-Host ""
}

}
finally {
    Pop-Location
}
