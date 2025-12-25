param (
    [string]$NewName,
    [int]$BasePort = 13000
)

$ErrorActionPreference = "Stop"

Write-Host "--------------------------------------------------"
Write-Host "      Web API Template Initialization Script      "
Write-Host "--------------------------------------------------"

# 1. Get Project Name
if ([string]::IsNullOrWhiteSpace($NewName)) {
    $NewName = Read-Host "Enter new project name (e.g. MyAwesomeApi)"
}
if ([string]::IsNullOrWhiteSpace($NewName)) {
    Write-Error "Project name cannot be empty."
    exit 1
}

# 2. Get Base Port
if ($BasePort -eq 13000) {
    $inputPort = Read-Host "Enter base port for Docker services (default 13000)"
    if (-not [string]::IsNullOrWhiteSpace($inputPort)) {
        $BasePort = [int]$inputPort
    }
}

$ApiPort = $BasePort + 2
$DbPort = $BasePort + 4

Write-Host "--------------------------------------------------"
Write-Host "Configuration:"
Write-Host "  Project Name: $NewName"
Write-Host "  API Port:     $ApiPort"
Write-Host "  DB Port:      $DbPort"
Write-Host "--------------------------------------------------"

$confirm = Read-Host "Proceed with initialization? (y/n)"
if ($confirm -ne "y" -and $confirm -ne "Y") {
    Write-Host "Aborted."
    exit 0
}

# 3. Update Docker Ports
Write-Host "Updating Docker ports..."
$dockerFile = "docker-compose.local.yml"
if (Test-Path $dockerFile) {
    $content = Get-Content $dockerFile -Raw
    $content = $content -replace "13002:8080", "$ApiPort`:8080"
    $content = $content -replace "13004:5432", "$DbPort`:5432"
    Set-Content $dockerFile $content -NoNewline -Encoding UTF8
} else {
    Write-Warning "docker-compose.local.yml not found."
}

# Update appsettings.Development.json (DB Port)
$appSettingsDev = "src\MyProject.WebApi\appsettings.Development.json"
if (Test-Path $appSettingsDev) {
    $content = Get-Content $appSettingsDev -Raw
    $content = $content -replace "Port=13004", "Port=$DbPort"
    Set-Content $appSettingsDev $content -NoNewline -Encoding UTF8
}

# Update http-client.env.json (API Port)
$httpClientEnv = "src\MyProject.WebApi\http-client.env.json"
if (Test-Path $httpClientEnv) {
    $content = Get-Content $httpClientEnv -Raw
    $content = $content -replace "localhost:13002", "localhost:$ApiPort"
    Set-Content $httpClientEnv $content -NoNewline -Encoding UTF8
}

# 4. Rename Project
$OldName = "MyProject"
$OldNameLower = "myproject"
$NewNameLower = $NewName.ToLower()

Write-Host "Renaming project from '$OldName' to '$NewName'..."

# Function to replace text in files
function Replace-TextInFiles {
    param (
        [string]$Path,
        [string]$Old,
        [string]$New
    )
    Get-ChildItem -Path $Path -Recurse -File | Where-Object { 
        $_.FullName -notmatch "\\.git\\" -and 
        $_.FullName -notmatch "\\bin\\" -and 
        $_.FullName -notmatch "\\obj\\" 
    } | ForEach-Object {
         try {
            $content = Get-Content $_.FullName -Raw
            if ($content -match $Old) {
                $content = $content -replace $Old, $New
                Set-Content $_.FullName $content -NoNewline -Encoding UTF8
            }
        }
        catch {
            Write-Warning "Could not read/write file: $($_.FullName)"
        }
    }
}

Write-Host "Replacing text content..."
Replace-TextInFiles -Path . -Old $OldName -New $NewName
Replace-TextInFiles -Path . -Old $OldNameLower -New $NewNameLower

Write-Host "Renaming files and directories..."
Get-ChildItem -Path . -Recurse | Sort-Object FullName -Descending | ForEach-Object {
    if ($_.FullName -notmatch "\\.git\\" -and $_.FullName -notmatch "\\bin\\" -and $_.FullName -notmatch "\\obj\\") {
        if ($_.Name -match $OldName) {
            $newName = $_.Name -replace $OldName, $NewName
            $newPath = Join-Path $_.Parent.FullName $newName
            Rename-Item -Path $_.FullName -NewName $newName
            Write-Host "Renamed: $($_.FullName) -> $newPath"
        }
        elseif ($_.Name -match $OldNameLower) {
            $newName = $_.Name -replace $OldNameLower, $NewNameLower
            $newPath = Join-Path $_.Parent.FullName $newName
            Rename-Item -Path $_.FullName -NewName $newName
            Write-Host "Renamed: $($_.FullName) -> $newPath"
        }
    }
}

# 5. Git Commit (Rename)
Write-Host "--------------------------------------------------"
$gitRenameConfirm = Read-Host "Do you want to commit the project rename changes? (y/n)"
if ($gitRenameConfirm -eq "y" -or $gitRenameConfirm -eq "Y") {
    git add .
    git commit -m "Renamed project from $OldName to $NewName"
    Write-Host "Changes committed."
}

# 6. Migrations
Write-Host "--------------------------------------------------"
$migConfirm = Read-Host "Do you want to reset and create a fresh Initial Migration? (y/n)"
if ($migConfirm -eq "y" -or $migConfirm -eq "Y") {
    Write-Host "Resetting migrations..."
    
    $migrationDir = "src\$NewName.Infrastructure\Features\Postgres\Migrations"
    
    if (Test-Path $migrationDir) {
        Remove-Item "$migrationDir\*" -Recurse -Force
    } else {
        New-Item -ItemType Directory -Path $migrationDir -Force | Out-Null
    }

    Write-Host "Building project and adding Initial migration..."
    
    # Restore local tools
    Write-Host "Restoring local tools..."
    dotnet tool restore

    # Restore and build explicitly
    Write-Host "Restoring dependencies..."
    dotnet restore "src\$NewName.WebApi"

    Write-Host "Building project..."
    dotnet build "src\$NewName.WebApi" --no-restore

    Write-Host "Running migrations..."
    dotnet ef migrations add Initial --project "src\$NewName.Infrastructure" --startup-project "src\$NewName.WebApi" --output-dir Features/Postgres/Migrations --no-build
    
    Write-Host "Migration 'Initial' created successfully."

    # 7. Git Commit (Migration)
    Write-Host "--------------------------------------------------"
    $gitMigConfirm = Read-Host "Do you want to commit the initial migration? (y/n)"
    if ($gitMigConfirm -eq "y" -or $gitMigConfirm -eq "Y") {
        git add .
        git commit -m "Add initial migration"
        Write-Host "Migration changes committed."
    }
}

Write-Host "--------------------------------------------------"
Write-Host "Initialization complete!"
Write-Host "You can now run: docker compose -f docker-compose.local.yml up -d"
