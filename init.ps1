#Requires -Version 5.1
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

.PARAMETER Help
    Show usage and exit.

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

[CmdletBinding()]
param (
    [Parameter(Position = 0)]
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
    [switch]$NoAspire,

    [Alias("h")]
    [switch]$Help,

    # Collects anything that did not bind so unknown flags can be rejected with a
    # readable message instead of being silently ignored (init.sh does the same).
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ExtraArgs
)

$ErrorActionPreference = "Stop"

# Get script directory and ensure we're working from there
$ScriptDir = Split-Path -Parent $PSCommandPath
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

# Native command output arrives as an object array; Write-Host would join it with
# spaces onto a single line, so route it through Out-String to keep line breaks.
function Write-CommandOutput {
    param([object]$Output)
    if ($null -eq $Output) { return }
    ($Output | Out-String).TrimEnd() -split "`r?`n" | ForEach-Object {
        Write-Host $_ -ForegroundColor DarkGray
    }
}

# -----------------------------------------------------------------------------
# Usage
# -----------------------------------------------------------------------------
function Show-Usage {
    Write-Host "Project Initialization Script"
    Write-Host ""
    Write-Host "Usage:"
    Write-Host "  .\init.ps1                    Interactive mode"
    Write-Host "  .\init.ps1 [options]          Non-interactive mode"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Name NAME            Project name (e.g., MyAwesomeApi)"
    Write-Host "  -Port PORT            Base port for services (default: 13000)"
    Write-Host "  -Email EMAIL          Superuser email (default: superuser@test.com)"
    Write-Host "  -Password PASS        Superuser password (default: Superuser123!)"
    Write-Host "  -Yes                  Accept all defaults without prompting"
    Write-Host "  -NoMigration          Skip creating initial migration"
    Write-Host "  -NoBuild              Skip building and running tests"
    Write-Host "  -NoCommit             Skip git commits"
    Write-Host "  -NoAspire             Don't launch Aspire after setup"
    Write-Host "  -Help                 Show this help message"
    Write-Host ""
    Write-Host "Port allocation:"
    Write-Host "  Frontend:   BASE_PORT      (e.g., 13000)"
    Write-Host "  API:        BASE_PORT + 2  (e.g., 13002)"
    Write-Host "  (Infrastructure ports are managed automatically by Aspire)"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\init.ps1 -Name MyApi -Port 14000 -Yes"
    Write-Host "  .\init.ps1 -Name MyApi -Yes"
    Write-Host "  .\init.ps1 -Name MyApi -Email me@example.com -Password MyPass123!"
}

if ($Help) {
    Show-Usage
    exit 0
}

# Reject unrecognized arguments. init.sh has an explicit `*) Unknown option` arm;
# without this a PowerShell script silently swallows anything that did not bind,
# so `--no-commit` would be ignored and the script would commit anyway.
if ($ExtraArgs -and $ExtraArgs.Count -gt 0) {
    $shorthand = @{
        "--no-migration" = "-NoMigration"
        "--no-build"     = "-NoBuild"
        "--no-commit"    = "-NoCommit"
        "--no-aspire"    = "-NoAspire"
        "--name"         = "-Name"
        "--port"         = "-Port"
        "--email"        = "-Email"
        "--password"     = "-Password"
        "--yes"          = "-Yes"
        "--help"         = "-Help"
    }
    Write-ErrorMessage "Unknown option: $($ExtraArgs[0])"
    foreach ($argument in $ExtraArgs) {
        $key = $argument.ToLowerInvariant()
        if ($shorthand.ContainsKey($key)) {
            Write-Info "This is the PowerShell script - use $($shorthand[$key]) instead of $argument"
        }
    }
    Write-Host ""
    Show-Usage
    exit 1
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

    return $response.ToLowerInvariant() -eq "y"
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

# Reads without echoing, matching init.sh's `read -sp`.
function Read-Secret {
    param(
        [string]$Question,
        [string]$Default = ""
    )

    if ($Yes) {
        return $Default
    }

    $prompt = if ([string]::IsNullOrWhiteSpace($Default)) { $Question } else { "$Question [$Default]" }

    # -AsSecureString cannot read redirected input: with stdin piped it terminates
    # the host process outright rather than returning. Fall back to a plain read
    # there, which is what the visible prompt did before masking was added.
    if (Test-InteractiveConsole) {
        $secure = Read-Host $prompt -AsSecureString
        $credential = New-Object System.Net.NetworkCredential("", $secure)
        $response = $credential.Password
    }
    else {
        $response = Read-Host $prompt
    }

    if ([string]::IsNullOrWhiteSpace($response)) {
        return $Default
    }
    return $response
}

function Test-Prerequisites {
    # Docker is only needed to launch Aspire. Migrations, build and tests all run
    # without a daemon (there are no Testcontainers in the solution), so -NoAspire
    # runs should not be blocked by a stopped daemon.
    param([bool]$RequireDocker = $true)

    $missing = @()

    if (-not (Get-Command git -ErrorAction SilentlyContinue)) { $missing += "git" }
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { $missing += "dotnet" }

    if (-not $RequireDocker) {
        # Skip the daemon probe entirely.
    }
    elseif (Get-Command docker -ErrorAction SilentlyContinue) {
        # Needed on both hosts, for different reasons: Windows PowerShell 5.1
        # surfaces native stderr as a terminating NativeCommandError, and PowerShell
        # 7.3+ applies $ErrorActionPreference to a native non-zero exit code via
        # $PSNativeCommandUseErrorActionPreference. Either way a stopped Docker
        # daemon would abort the run instead of reporting the message below.
        $previousPreference = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        try {
            $null = docker info 2>&1
            $dockerRunning = ($LASTEXITCODE -eq 0)
        }
        finally {
            $ErrorActionPreference = $previousPreference
        }

        if (-not $dockerRunning) {
            $missing += "docker (installed but not running - start Docker)"
        }
    }
    else {
        $missing += "docker"
    }

    if (-not (Get-Command node -ErrorAction SilentlyContinue)) { $missing += "node" }
    if (-not (Get-Command pnpm -ErrorAction SilentlyContinue)) { $missing += "pnpm" }

    if ($missing.Count -gt 0) {
        Write-ErrorMessage "Missing required tools: $($missing -join ', ')"
        Write-Host "Please install them before running this script."
        if ($missing -contains "pnpm") {
            Write-Info "pnpm is managed via corepack. Run: corepack enable"
        }
        exit 1
    }
}

function Test-ProjectName {
    param([string]$ProjectName)

    if ([string]::IsNullOrWhiteSpace($ProjectName)) {
        Write-ErrorMessage "Project name cannot be empty"
        return $false
    }

    # -cnotmatch, not -notmatch: PowerShell's regex operators are case-insensitive by
    # default, so [A-Z] would happily accept "myapi" where init.sh's bash =~ rejects it.
    if ($ProjectName -cnotmatch "^[A-Z][a-zA-Z0-9]*$") {
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

# Reads a file as text, preserving whether it carried a UTF-8 BOM, and reports
# binary content the way `grep -I` does so it is never rewritten.
function Read-TextFile {
    param([string]$Path)

    $bytes = [System.IO.File]::ReadAllBytes($Path)

    $probeLength = [Math]::Min(8192, $bytes.Length)
    for ($i = 0; $i -lt $probeLength; $i++) {
        if ($bytes[$i] -eq 0) { return $null }
    }

    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $offset = if ($hasBom) { 3 } else { 0 }
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false

    return [PSCustomObject]@{
        Content = $utf8NoBom.GetString($bytes, $offset, $bytes.Length - $offset)
        HasBom  = $hasBom
    }
}

function Set-FileContent {
    param(
        [string]$Path,
        [string]$Content,
        [bool]$WithBom = $false
    )
    $encoding = New-Object System.Text.UTF8Encoding $WithBom
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

# The superuser credentials are substituted into appsettings.Development.json, so
# they must be encoded for JSON. Without this a password containing a backslash
# changes value silently (pass\told parses back as pass<TAB>old) and one
# containing a double quote produces a file that no longer parses.
function ConvertTo-JsonStringContent {
    param([string]$Value)
    $quoted = $Value | ConvertTo-Json -Compress
    return $quoted.Substring(1, $quoted.Length - 2)
}

function ConvertTo-KebabCase {
    param([string]$Text)
    # ToLowerInvariant, not ToLower: under a tr-TR culture "I" lowercases to a dotless
    # "i", which is not valid in Docker volume or MinIO bucket names.
    ($Text -creplace '([a-z])([A-Z])', '$1-$2').ToLowerInvariant()
}

# Walks the tree while pruning excluded directories, mirroring init.sh's
# `grep -r --exclude-dir=...`. Get-ChildItem -Recurse would enumerate all of
# node_modules and .git before filtering, which is slow and, on Windows
# PowerShell 5.1, throws PathTooLongException on paths past MAX_PATH. Errors are
# non-fatal here: an unreadable subdirectory must not abort initialization.
function Get-TemplateItems {
    param(
        [string]$Root,
        [switch]$IncludeDirectories
    )

    $excludedDirectories = @(".git", "bin", "obj", "node_modules")
    $excludedFiles = @("init.ps1", "init.sh")

    $items = [System.Collections.Generic.List[object]]::new()
    $pending = [System.Collections.Generic.Queue[string]]::new()
    $pending.Enqueue($Root)

    while ($pending.Count -gt 0) {
        $current = $pending.Dequeue()

        # -Force is required: on Linux and macOS PowerShell treats every dot-prefixed
        # entry as hidden, so .claude/, .github/ and src/frontend/.env.example would
        # be skipped entirely. Excluding .git by name above makes this safe.
        foreach ($child in @(Get-ChildItem -LiteralPath $current -Force -ErrorAction SilentlyContinue)) {
            if ($child.PSIsContainer) {
                if ($excludedDirectories -contains $child.Name) { continue }
                $pending.Enqueue($child.FullName)
                if ($IncludeDirectories) { $items.Add($child) }
            }
            elseif ($excludedFiles -notcontains $child.Name) {
                $items.Add($child)
            }
        }
    }

    return $items
}

# OrderedDictionary defaults to a case-insensitive comparer, which would reject
# "MyProject" and "myproject" as duplicate keys. Ordinal keeps them distinct and
# matches sed's case-sensitive behaviour in init.sh.
function New-ReplacementMap {
    return [System.Collections.Specialized.OrderedDictionary]::new([System.StringComparer]::Ordinal)
}

# Applies literal, ordinal replacements to every text file under the repo root.
# String.Replace is used rather than -replace because -replace's second operand is
# a .NET substitution template: a password containing $&, $1 or $_ would otherwise
# be rewritten into something else entirely.
function Update-TemplateFiles {
    param(
        [System.Collections.Specialized.OrderedDictionary]$Replacements
    )

    $failures = @()

    foreach ($file in @(Get-TemplateItems -Root $ScriptDir)) {
        $document = $null
        try {
            $document = Read-TextFile $file.FullName
        }
        catch {
            $failures += "could not read $($file.FullName): $($_.Exception.Message)"
            continue
        }

        # Binary file - skip it the way `grep -I` would.
        if ($null -eq $document) { continue }

        $content = $document.Content
        $originalContent = $content

        foreach ($token in $Replacements.Keys) {
            $content = $content.Replace($token, $Replacements[$token])
        }

        if ($content -ne $originalContent) {
            try {
                Set-FileContent $file.FullName $content $document.HasBom
            }
            catch {
                $failures += "could not write $($file.FullName): $($_.Exception.Message)"
            }
        }
    }

    return $failures
}

function Write-Failures {
    param(
        [string[]]$Failures,
        [string]$Summary
    )

    if (-not $Failures -or $Failures.Count -eq 0) { return }

    Write-WarnMsg "$Summary ($($Failures.Count)):"
    foreach ($failure in $Failures) {
        Write-Info "  $failure"
    }
}

# -----------------------------------------------------------------------------
# Interactive Checklist
# -----------------------------------------------------------------------------
# True only when the console can deliver single keypresses. The ISE and any
# redirected stdin (piping, CI, remoting) make [Console]::ReadKey throw.
function Test-InteractiveConsole {
    if ($Host.Name -eq "Windows PowerShell ISE Host") { return $false }

    try {
        if ([Console]::IsInputRedirected) { return $false }
    }
    catch {
        return $false
    }

    return $true
}

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

    if (-not (Test-InteractiveConsole)) {
        Write-Host ""
        Write-Info "No interactive console detected - using the default options."
        Write-Info "Pass -Yes to select defaults without this notice."
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
            try {
                # WindowWidth - 1: writing exactly WindowWidth characters wraps the
                # cursor on the legacy Windows console, which cancels the move up and
                # stacks a fresh copy of the menu under the stale one.
                $clearWidth = [Math]::Max(1, [Console]::WindowWidth - 1)
                $linesToClear = $Options.Count + 3
                for ($j = 0; $j -lt $linesToClear; $j++) {
                    if ([Console]::CursorTop -le 0) { break }
                    [Console]::SetCursorPosition(0, [Console]::CursorTop - 1)
                    [Console]::Write((" " * $clearWidth))
                    [Console]::SetCursorPosition(0, [Console]::CursorTop)
                }
            }
            catch {
                # Console cannot be repositioned - redraw below the previous menu.
                Write-Host ""
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
        try {
            $key = [Console]::ReadKey($true)
        }
        catch {
            Write-Host ""
            Write-Info "Console does not support key input - using the default options."
            return $Defaults
        }

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
Test-Prerequisites -RequireDocker (-not $NoAspire)
if ($NoAspire) {
    Write-Success "All prerequisites found (git, dotnet, node, pnpm)"
    Write-Info "Docker not checked: -NoAspire means the stack is never launched"
}
else {
    Write-Success "All prerequisites found (git, dotnet, docker, node, pnpm)"
}

# -----------------------------------------------------------------------------
# Step 1: Project Name
# -----------------------------------------------------------------------------
Write-Step "Project setup"
Write-Host ""

while ($true) {
    if ([string]::IsNullOrWhiteSpace($Name)) {
        # Non-interactive: there is nobody to answer the prompt, so fail with a
        # usable message instead of blocking forever on Read-Host.
        if ($Yes) {
            Write-ErrorMessage "A project name is required with -Yes. Pass -Name <PascalCaseName>."
            exit 1
        }
        $Name = Read-Value "Project name (PascalCase, e.g. MyAwesomeApi)"
    }

    if (Test-ProjectName $Name) {
        break
    }

    if ($Yes) { exit 1 }
    $Name = ""
}

# -----------------------------------------------------------------------------
# Step 2: Base Port
# -----------------------------------------------------------------------------
while ($true) {
    if ($Yes) {
        # Non-interactive: there is no prompt to retry, so fail instead of looping.
        if (-not (Test-Port $Port)) { exit 1 }
        break
    }

    $portInput = Read-Value "Base port" $Port.ToString()

    # TryParse rather than [int]: a bare cast raises a terminating error under
    # $ErrorActionPreference = "Stop", so one typo would kill the whole run.
    $parsedPort = 0
    if (-not [int]::TryParse($portInput, [ref]$parsedPort)) {
        Write-ErrorMessage "Port must be a number"
        continue
    }

    $Port = $parsedPort
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
$Password = Read-Secret "Superuser password" $Password

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
    $results = @(Read-Checklist -Options $checklistOptions -Defaults $checklistDefaults)

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
$NewNameLower = $Name.ToLowerInvariant()
$initPs1 = Join-Path $ScriptDir "init.ps1"

# Step 1: Update Ports (substitute placeholders across all files)
Write-Step "Updating port configuration..."

$frontendEnvExample = Join-Path $ScriptDir "src/frontend/.env.example"
$frontendEnvLocal = Join-Path $ScriptDir "src/frontend/.env.local"
if (Test-Path -LiteralPath $frontendEnvExample) {
    Copy-Item -LiteralPath $frontendEnvExample -Destination $frontendEnvLocal -Force
    Write-SubStep "Created frontend .env.local from .env.example"
}

# Generate random secrets
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$jwtBytes = New-Object byte[] 64
$rng.GetBytes($jwtBytes)
$encBytes = New-Object byte[] 64
$rng.GetBytes($encBytes)
$rng.Dispose()
$JwtSecret = ([Convert]::ToBase64String($jwtBytes) -replace '[/+=]', '')
$JwtSecret = $JwtSecret.Substring(0, [Math]::Min(64, $JwtSecret.Length))
$EncryptionKey = ([Convert]::ToBase64String($encBytes) -replace '[/+=]', '')
$EncryptionKey = $EncryptionKey.Substring(0, [Math]::Min(64, $EncryptionKey.Length))

Write-SubStep "Replacing placeholders..."
$placeholders = New-ReplacementMap
$placeholders.Add("{INIT_FRONTEND_PORT}", "$FrontendPort")
$placeholders.Add("{INIT_API_PORT}", "$ApiPort")
$placeholders.Add("{INIT_PROJECT_SLUG}", $ProjectSlug)
$placeholders.Add("{INIT_JWT_SECRET}", $JwtSecret)
$placeholders.Add("{INIT_ENCRYPTION_KEY}", $EncryptionKey)

$placeholderFailures = Update-TemplateFiles -Replacements $placeholders
Write-Failures -Failures $placeholderFailures -Summary "Some files could not be updated"

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
# -ceq, not -eq: a case-insensitive compare would treat "Myproject" as a match and
# silently skip the rename, discarding the name the user asked for.
if ($NewName -ceq $OldName) {
    Write-Step "Skipping rename (project name is already MyProject)"
} else {
    Write-Step "Renaming project..."

    Write-SubStep "Replacing text content..."
    $renameReplacements = New-ReplacementMap
    $renameReplacements.Add($OldName, $NewName)
    $renameReplacements.Add($OldNameLower, $NewNameLower)
    $renameFailures = Update-TemplateFiles -Replacements $renameReplacements
    Write-Failures -Failures $renameFailures -Summary "Some files could not be renamed in content"

    Write-SubStep "Renaming files and directories..."
    # Sorted by path length descending so children are renamed before their parents.
    $items = @(Get-TemplateItems -Root $ScriptDir -IncludeDirectories) |
        Where-Object { $_.Name.Contains($OldName) -or $_.Name.Contains($OldNameLower) } |
        Sort-Object { $_.FullName.Length } -Descending

    $pathFailures = @()
    foreach ($item in $items) {
        $newItemName = $item.Name.Replace($OldName, $NewName).Replace($OldNameLower, $NewNameLower)

        # -ceq: with a case-insensitive compare a rename that only changes case
        # (e.g. MyProject.WebApi -> Myproject.WebApi) would be skipped as a no-op.
        if ($newItemName -ceq $item.Name) { continue }

        try {
            if ($newItemName -ieq $item.Name) {
                # NTFS and APFS are case-insensitive, so a direct case-only rename
                # does nothing. Route it through a temporary name.
                $parent = Split-Path -Parent $item.FullName
                $tempName = "$newItemName.init-tmp"
                Rename-Item -LiteralPath $item.FullName -NewName $tempName -ErrorAction Stop
                Rename-Item -LiteralPath (Join-Path $parent $tempName) -NewName $newItemName -ErrorAction Stop
            }
            else {
                Rename-Item -LiteralPath $item.FullName -NewName $newItemName -ErrorAction Stop
            }
        }
        catch {
            $pathFailures += "$($item.FullName) -> $newItemName : $($_.Exception.Message)"
        }
    }
    Write-Failures -Failures $pathFailures -Summary "Some paths could not be renamed"

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

# Step 3b: Seed superuser credentials
#
# Deliberately after the rename. The rename pass rewrites every occurrence of
# MyProject and myproject across the tree, so substituting credentials earlier
# silently mangles any value containing either token: --email admin@myproject.com
# with -Name MyApi would seed admin@myapi.com while the summary showed the
# address the user actually typed.
Write-Step "Seeding superuser credentials..."

$credentials = New-ReplacementMap
$credentials.Add("{INIT_SUPERUSER_EMAIL}", (ConvertTo-JsonStringContent $Email))
$credentials.Add("{INIT_SUPERUSER_PASSWORD}", (ConvertTo-JsonStringContent $Password))

$credentialFailures = Update-TemplateFiles -Replacements $credentials
Write-Failures -Failures $credentialFailures -Summary "Some credentials could not be written"

Write-Success "Superuser credentials configured"

if ($DoCommit) {
    $ErrorActionPreference = "Continue"
    $null = git add . 2>&1
    $null = git commit -m "chore: seed superuser credentials" 2>&1
    $ErrorActionPreference = "Stop"
}

# Step 4: Create Migration
if ($CreateMigration) {
    Write-Step "Creating initial migration..."

    $migrationDir = Join-Path $ScriptDir "src/backend/$NewName.Infrastructure/Persistence/Migrations"

    if (Test-Path -LiteralPath $migrationDir) {
        Write-SubStep "Clearing existing migrations..."
        Remove-Item "$migrationDir/*" -Recurse -Force -ErrorAction SilentlyContinue
    }
    else {
        New-Item -ItemType Directory -Path $migrationDir -Force | Out-Null
    }

    # Temporarily allow errors for external commands
    $ErrorActionPreference = "Continue"

    Write-SubStep "Restoring dotnet tools..."
    # Try tool restore with explicit config file since root may not have NuGet sources
    $null = dotnet tool restore --configfile "src/backend/nuget.config" 2>&1
    if ($LASTEXITCODE -ne 0) {
        # Fallback: try without config (maybe global sources exist). A failure here is
        # not fatal - dotnet-ef may still resolve from a global install, so only a
        # build failure skips the migration (this matches init.sh).
        $null = dotnet tool restore 2>&1
    }

    Write-SubStep "Restoring dependencies..."
    $restoreOutput = dotnet restore "src/backend/$NewName.WebApi" 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-ErrorMessage "Failed to restore dependencies"
        Write-CommandOutput $restoreOutput
        Write-Info "You can run manually: dotnet restore src/backend/$NewName.WebApi"
    }

    Write-SubStep "Building project..."
    $buildOutput = dotnet build "src/backend/$NewName.WebApi" --no-restore -v q 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-ErrorMessage "Build failed. Migration will be skipped."
        Write-CommandOutput $buildOutput
        Write-Info "Fix build errors and run manually:"
        Write-Info "  dotnet ef migrations add Initial --project src/backend/$NewName.Infrastructure --startup-project src/backend/$NewName.WebApi --output-dir Persistence/Migrations"
    }
    else {
        Write-SubStep "Running ef migrations add..."
        $efOutput = dotnet ef migrations add Initial --project "src/backend/$NewName.Infrastructure" --startup-project "src/backend/$NewName.WebApi" --output-dir Persistence/Migrations --no-build 2>&1

        if ($LASTEXITCODE -ne 0) {
            Write-ErrorMessage "Migration creation failed"
            Write-CommandOutput $efOutput
            Write-Info "Run manually after fixing any issues:"
            Write-Info "  dotnet ef migrations add Initial --project src/backend/$NewName.Infrastructure --startup-project src/backend/$NewName.WebApi --output-dir Persistence/Migrations"
        }
        else {
            Write-Success "Migration 'Initial' created"

            if ($DoCommit) {
                Write-SubStep "Committing migration..."
                $null = git add . 2>&1
                $null = git commit -m "chore: add initial database migration" 2>&1
                Write-Success "Migration committed"
            }
        }
    }

    $ErrorActionPreference = "Stop"
}

# Step 5: Build and Run Tests
if ($BuildTest) {
    $ErrorActionPreference = "Continue"

    # Streamed, not captured: buffering into a variable and passing it to Write-Host
    # joins every compiler error onto a single unreadable line.
    Write-Step "Building solution..."
    dotnet build "src/backend/$NewName.slnx" -c Debug --verbosity quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Build succeeded"
    } else {
        Write-ErrorMessage "Build failed"
    }

    Write-Step "Running tests..."
    dotnet test "src/backend/$NewName.slnx" -c Release --verbosity quiet --no-restore
    if ($LASTEXITCODE -eq 0) {
        Write-Success "All tests passed"
    } else {
        Write-WarnMsg "Some tests failed - check output above"
    }

    $ErrorActionPreference = "Stop"
}

# Step 6: Delete template-specific files (always, fire and forget)
Write-Step "Cleaning up template files..."

$templateFiles = @(
    "init.sh"
    ".github/workflows/claude.yml"
    ".github/workflows/claude-code-review.yml"
    ".github/workflows/init-scripts.yml"
)

$templateDirs = @(
    "docs/sessions/assets"
    ".claude/skills/init-verify"
)

$ErrorActionPreference = "Continue"

# init.sh guards its git calls with `git rev-parse --git-dir`. Without the guard a
# ZIP download or a repo with .git removed makes every `git rm` a silent no-op and
# no template file is ever deleted. Removal happens on disk either way; git only
# decides whether the deletion can also be staged.
$null = git rev-parse --git-dir 2>&1
$inGitRepo = ($LASTEXITCODE -eq 0)

# Dated session docs are template history; README.md stays for the convention
$sessionDocs = @(
    Get-ChildItem (Join-Path $ScriptDir "docs/sessions") -Filter "2*.md" -ErrorAction SilentlyContinue |
        ForEach-Object { "docs/sessions/$($_.Name)" }
)

$cleanupFailures = @()

foreach ($relativePath in (@($templateFiles) + $sessionDocs)) {
    $fullPath = Join-Path $ScriptDir $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { continue }

    if ($inGitRepo) { $null = git rm -f -- $relativePath 2>&1 }
    if (Test-Path -LiteralPath $fullPath) {
        try { Remove-Item -LiteralPath $fullPath -Force -ErrorAction Stop }
        catch { $cleanupFailures += "$relativePath : $($_.Exception.Message)" }
    }
}

foreach ($relativePath in $templateDirs) {
    $fullPath = Join-Path $ScriptDir $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { continue }

    if ($inGitRepo) { $null = git rm -r -f -- $relativePath 2>&1 }
    if (Test-Path -LiteralPath $fullPath) {
        try { Remove-Item -LiteralPath $fullPath -Recurse -Force -ErrorAction Stop }
        catch { $cleanupFailures += "$relativePath : $($_.Exception.Message)" }
    }
}

# PowerShell parses the whole script before running it, so init.ps1 can delete
# itself here the same way init.sh does. The detached fallback below only matters
# if something still holds the file open.
$initRemoved = $false
try {
    Remove-Item -LiteralPath $initPs1 -Force -ErrorAction Stop
    $initRemoved = $true
}
catch {
    $initRemoved = $false
}

if ($DoCommit -and $inGitRepo) {
    $null = git add -A 2>&1
    $null = git commit -m "chore: remove template-specific files" 2>&1
    Write-Success "Deletion committed to git"
}

Write-Failures -Failures $cleanupFailures -Summary "Some template files could not be removed"

if (-not $initRemoved) {
    # Escape apostrophes: a repo path like C:\Users\O'Brien\src would otherwise
    # terminate the single-quoted string and the detached cleanup would never run.
    try {
        $pwshExe = (Get-Process -Id $PID).Path
        # Doubling apostrophes is not enough: the PowerShell tokenizer also accepts
        # U+2018, U+2019, U+201A and U+201B as single-quote delimiters, and macOS
        # and Word both autocorrect ' to U+2019 in folder names.
        $escapedInitPath = [System.Management.Automation.Language.CodeGeneration]::EscapeSingleQuotedStringContent($initPs1)
        $cleanupScript = "Start-Sleep -Seconds 2; Remove-Item -LiteralPath '$escapedInitPath' -Force -ErrorAction SilentlyContinue"
        $encodedCmd = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($cleanupScript))
        $startArgs = @{
            FilePath     = $pwshExe
            ArgumentList = @("-NoProfile", "-EncodedCommand", $encodedCmd)
        }
        if ($env:OS -eq 'Windows_NT') { $startArgs.WindowStyle = "Hidden" }
        Start-Process @startArgs | Out-Null
    }
    catch {
        Write-WarnMsg "Could not remove init.ps1 automatically - delete it manually."
    }
}

$ErrorActionPreference = "Stop"

Write-Success "Template files removed"

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

    try {
        Start-Job -ScriptBlock {
            for ($i = 0; $i -lt 30; $i++) {
                try {
                    $null = Invoke-WebRequest -Uri "http://localhost:15244" -UseBasicParsing -TimeoutSec 1 -ErrorAction Stop
                    Start-Process "http://localhost:15244"
                    break
                } catch { Start-Sleep -Seconds 1 }
            }
        } | Out-Null
    }
    catch {
        Write-Info "Could not start the browser opener - open http://localhost:15244 manually."
    }

    # Scoped to this run: init.sh uses a POSIX prefix assignment, so the setting must
    # not outlive the script and silently unsecure later `dotnet run` invocations.
    $previousDashboardSetting = $env:DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS
    try {
        $env:DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS = "true"
        & dotnet run --project "src/backend/$NewName.AppHost"
    }
    finally {
        $env:DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS = $previousDashboardSetting
    }
}
else {
    Write-Host ""
    Write-Host "  Your project is ready!" -ForegroundColor White
    Write-Host ""
    Write-Host "  Quick Start" -ForegroundColor White
    Write-Host "  -------------------------------------"
    Write-Host "  dotnet run --project src/backend/$NewName.AppHost"
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
catch {
    # Without this the script-wide `try` had only a `finally`, so any terminating
    # error surfaced as a raw .NET exception with a caret diagram and no indication
    # of which step failed.
    Write-Host ""
    Write-Host "[ERROR] Initialization failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.InvocationInfo) {
        Write-Host "        at line $($_.InvocationInfo.ScriptLineNumber): $($_.InvocationInfo.Line.Trim())" -ForegroundColor DarkGray
    }
    Write-Host "        The repository may be partially initialized - check 'git status' before retrying." -ForegroundColor DarkGray
    exit 1
}
finally {
    Pop-Location
}
