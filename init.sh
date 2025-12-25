#!/bin/bash

# Exit on error
set -e

echo "--------------------------------------------------"
echo "      Web API Template Initialization Script      "
echo "--------------------------------------------------"

# 1. Get Project Name
read -p "Enter new project name (e.g. MyAwesomeApi): " NEW_NAME
if [ -z "$NEW_NAME" ]; then
    echo "Error: Project name cannot be empty."
    exit 1
fi

# 2. Get Base Port
DEFAULT_BASE_PORT=13000
read -p "Enter base port for Docker services (default $DEFAULT_BASE_PORT): " BASE_PORT
BASE_PORT=${BASE_PORT:-$DEFAULT_BASE_PORT}

FRONTEND_PORT=$BASE_PORT
API_PORT=$((BASE_PORT + 2))
DB_PORT=$((BASE_PORT + 4))

echo "--------------------------------------------------"
echo "Configuration:"
echo "  Project Name:  $NEW_NAME"
echo "  Frontend Port: $FRONTEND_PORT"
echo "  API Port:      $API_PORT"
echo "  DB Port:       $DB_PORT"
echo "--------------------------------------------------"

read -p "Proceed with initialization? (y/n): " CONFIRM
if [[ "$CONFIRM" != "y" && "$CONFIRM" != "Y" ]]; then
    echo "Aborted."
    exit 0
fi

# 3. Update Docker Ports
echo "Updating Docker ports..."
# We use a simple sed here assuming the structure of docker-compose.local.yml is known and consistent
# Frontend Port: "13000:3000" -> "$FRONTEND_PORT:3000"
# API Port: "13002:8080" -> "$API_PORT:8080"
# DB Port: "13004:5432" -> "$DB_PORT:5432"

OS=$(uname)
if [ "$OS" = "Darwin" ]; then
    sed -i '' "s/13000:3000/$FRONTEND_PORT:3000/g" docker-compose.local.yml
    sed -i '' "s/13002:8080/$API_PORT:8080/g" docker-compose.local.yml
    sed -i '' "s/13004:5432/$DB_PORT:5432/g" docker-compose.local.yml
    
    # Update appsettings.Development.json (DB Port)
    sed -i '' "s/Port=13004/Port=$DB_PORT/g" src/backend/MyProject.WebApi/appsettings.Development.json
    
    # Update http-client.env.json (API Port)
    sed -i '' "s/localhost:13002/localhost:$API_PORT/g" src/backend/MyProject.WebApi/http-client.env.json

    # Create and update frontend .env.local
    cp src/frontend/.env.example src/frontend/.env.local
    sed -i '' "s/localhost:13002/localhost:$API_PORT/g" src/frontend/.env.local
else
    sed -i "s/13000:3000/$FRONTEND_PORT:3000/g" docker-compose.local.yml
    sed -i "s/13002:8080/$API_PORT:8080/g" docker-compose.local.yml
    sed -i "s/13004:5432/$DB_PORT:5432/g" docker-compose.local.yml

    # Update appsettings.Development.json (DB Port)
    sed -i "s/Port=13004/Port=$DB_PORT/g" src/backend/MyProject.WebApi/appsettings.Development.json

    # Update http-client.env.json (API Port)
    sed -i "s/localhost:13002/localhost:$API_PORT/g" src/backend/MyProject.WebApi/http-client.env.json

    # Create and update frontend .env.local
    cp src/frontend/.env.example src/frontend/.env.local
    sed -i "s/localhost:13002/localhost:$API_PORT/g" src/frontend/.env.local
fi

# 4. Rename Project
OLD_NAME="MyProject"
OLD_NAME_LOWER="myproject"
NEW_NAME_LOWER=$(echo "$NEW_NAME" | tr '[:upper:]' '[:lower:]')

echo "Renaming project from '$OLD_NAME' to '$NEW_NAME'..."

# Function to replace text in files
replace_text() {
    local search=$1
    local replace=$2
    if [ "$OS" = "Darwin" ]; then
        grep -rIl --null "$search" . --exclude-dir=.git --exclude-dir=bin --exclude-dir=obj | xargs -0 sed -i '' "s/$search/$replace/g"
    else
        grep -rIl --null "$search" . --exclude-dir=.git --exclude-dir=bin --exclude-dir=obj | xargs -0 sed -i "s/$search/$replace/g"
    fi
}

echo "Replacing text content..."
replace_text "$OLD_NAME" "$NEW_NAME"
replace_text "$OLD_NAME_LOWER" "$NEW_NAME_LOWER"

echo "Renaming files and directories..."
find . -depth -name "*$OLD_NAME*" -not -path "./.git/*" -not -path "./bin/*" -not -path "./obj/*" | while IFS= read -r path; do
    dir=$(dirname "$path")
    filename=$(basename "$path")
    new_filename=$(echo "$filename" | sed "s/$OLD_NAME/$NEW_NAME/g")
    mv "$path" "$dir/$new_filename"
    echo "Renamed: $path -> $dir/$new_filename"
done

find . -depth -name "*$OLD_NAME_LOWER*" -not -path "./.git/*" -not -path "./bin/*" -not -path "./obj/*" | while IFS= read -r path; do
    dir=$(dirname "$path")
    filename=$(basename "$path")
    new_filename=$(echo "$filename" | sed "s/$OLD_NAME_LOWER/$NEW_NAME_LOWER/g")
    mv "$path" "$dir/$new_filename"
    echo "Renamed: $path -> $dir/$new_filename"
done

# 5. Git Commit (Rename)
echo "--------------------------------------------------"
read -p "Do you want to commit the project rename changes? (y/n): " GIT_RENAME_CONFIRM
if [[ "$GIT_RENAME_CONFIRM" == "y" || "$GIT_RENAME_CONFIRM" == "Y" ]]; then
    git add .
    git commit -m "Renamed project from $OLD_NAME to $NEW_NAME"
    echo "Changes committed."
fi

# 6. Migrations
echo "--------------------------------------------------"
read -p "Do you want to reset and create a fresh Initial Migration? (y/n): " MIGRATION_CONFIRM
if [[ "$MIGRATION_CONFIRM" == "y" || "$MIGRATION_CONFIRM" == "Y" ]]; then
    echo "Resetting migrations..."
    
    MIGRATION_DIR="src/backend/$NEW_NAME.Infrastructure/Features/Postgres/Migrations"
    
    if [ -d "$MIGRATION_DIR" ]; then
        echo "Removing existing migrations in $MIGRATION_DIR..."
        rm -rf "$MIGRATION_DIR"/*
    else
        echo "Migration directory not found, creating..."
        mkdir -p "$MIGRATION_DIR"
    fi

    echo "Building project and adding Initial migration..."
    
    # Restore local tools
    echo "Restoring local tools..."
    dotnet tool restore

    # Restore and build explicitly
    echo "Restoring dependencies..."
    dotnet restore "src/backend/$NEW_NAME.WebApi"

    echo "Building project..."
    dotnet build "src/backend/$NEW_NAME.WebApi" --no-restore

    echo "Running migrations..."
    dotnet ef migrations add Initial \
        --project "src/backend/$NEW_NAME.Infrastructure" \
        --startup-project "src/backend/$NEW_NAME.WebApi" \
        --output-dir Features/Postgres/Migrations \
        --no-build

    echo "Migration 'Initial' created successfully."

    # 7. Git Commit (Migration)
    echo "--------------------------------------------------"
    read -p "Do you want to commit the initial migration? (y/n): " GIT_MIGRATION_CONFIRM
    if [[ "$GIT_MIGRATION_CONFIRM" == "y" || "$GIT_MIGRATION_CONFIRM" == "Y" ]]; then
        git add .
        git commit -m "Add initial migration"
        echo "Migration changes committed."
    fi
fi

echo "--------------------------------------------------"
echo "Initialization complete!"
echo "You can now run: docker compose -f docker-compose.local.yml up -d"
