#!/bin/bash

#══════════════════════════════════════════════════════════════════════════════
#  Web API Template - Project Initialization Script
#══════════════════════════════════════════════════════════════════════════════
#
#  Usage:
#    Interactive:     ./init.sh
#    Non-interactive: ./init.sh --name MyProject --port 14000 --yes
#
#  Flags:
#    --name, -n     Project name (required in non-interactive mode)
#    --port, -p     Base port (default: 13000)
#    --yes, -y      Accept all defaults, no prompts
#    --no-migration Skip migration creation
#    --no-commit    Skip git commits
#    --no-docker    Skip starting docker
#    --keep-scripts Keep init scripts after completion
#    --help, -h     Show this help
#
#══════════════════════════════════════════════════════════════════════════════

set -e

# ─────────────────────────────────────────────────────────────────────────────
# Colors and Formatting
# ─────────────────────────────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
MAGENTA='\033[0;35m'
CYAN='\033[0;36m'
BOLD='\033[1m'
DIM='\033[2m'
NC='\033[0m' # No Color

# ─────────────────────────────────────────────────────────────────────────────
# Helper Functions
# ─────────────────────────────────────────────────────────────────────────────
print_header() {
    echo ""
    echo -e "${BLUE}${BOLD}══════════════════════════════════════════════════════════════${NC}"
    echo -e "${BLUE}${BOLD}  $1${NC}"
    echo -e "${BLUE}${BOLD}══════════════════════════════════════════════════════════════${NC}"
}

print_step() {
    echo -e "\n${CYAN}${BOLD}▶ $1${NC}"
}

print_substep() {
    echo -e "  ${DIM}→${NC} $1"
}

print_success() {
    echo -e "${GREEN}✓${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

print_info() {
    echo -e "${DIM}ℹ${NC} $1"
}

# Prompt with default value. Usage: prompt "Question" "default" -> returns answer
# [Y/n] means Enter=Yes, [y/N] means Enter=No
prompt_yn() {
    local question=$1
    local default=$2  # "y" or "n"
    
    if [[ "$YES_TO_ALL" == "true" ]]; then
        [[ "$default" == "y" ]] && echo "y" || echo "n"
        return
    fi
    
    local prompt_hint
    if [[ "$default" == "y" ]]; then
        prompt_hint="[Y/n]"
    else
        prompt_hint="[y/N]"
    fi
    
    read -p "$(echo -e "${BOLD}$question${NC} $prompt_hint: ")" answer
    answer=${answer:-$default}
    echo "$answer" | tr '[:upper:]' '[:lower:]'
}

prompt_value() {
    local question=$1
    local default=$2
    
    if [[ "$YES_TO_ALL" == "true" && -n "$default" ]]; then
        echo "$default"
        return
    fi
    
    local prompt_text="$question"
    if [[ -n "$default" ]]; then
        prompt_text="$question [${default}]"
    fi
    
    read -p "$(echo -e "${BOLD}$prompt_text${NC}: ")" answer
    echo "${answer:-$default}"
}

show_help() {
    echo "Web API Template - Project Initialization Script"
    echo ""
    echo "Usage:"
    echo "  ./init.sh                     Interactive mode"
    echo "  ./init.sh [options]           Non-interactive mode"
    echo ""
    echo "Options:"
    echo "  -n, --name NAME       Project name (e.g., MyAwesomeApi)"
    echo "  -p, --port PORT       Base port for services (default: 13000)"
    echo "  -y, --yes             Accept all defaults without prompting"
    echo "      --no-migration    Skip creating initial migration"
    echo "      --no-commit       Skip git commits"
    echo "      --no-docker       Skip starting docker compose"
    echo "      --keep-scripts    Keep init.sh and init.ps1 after completion"
    echo "  -h, --help            Show this help message"
    echo ""
    echo "Port allocation:"
    echo "  Frontend:   BASE_PORT      (e.g., 13000)"
    echo "  API:        BASE_PORT + 2  (e.g., 13002)"
    echo "  Database:   BASE_PORT + 4  (e.g., 13004)"
    echo ""
    echo "Examples:"
    echo "  ./init.sh --name MyApi --port 14000 --yes"
    echo "  ./init.sh -n MyApi -y --no-docker"
}

check_prerequisites() {
    local missing=()
    
    command -v git >/dev/null 2>&1 || missing+=("git")
    command -v dotnet >/dev/null 2>&1 || missing+=("dotnet")
    command -v docker >/dev/null 2>&1 || missing+=("docker")
    
    if [[ ${#missing[@]} -gt 0 ]]; then
        print_error "Missing required tools: ${missing[*]}"
        echo "Please install them before running this script."
        exit 1
    fi
}

validate_project_name() {
    local name=$1
    
    if [[ -z "$name" ]]; then
        print_error "Project name cannot be empty"
        return 1
    fi
    
    if [[ ! "$name" =~ ^[A-Z][a-zA-Z0-9]*$ ]]; then
        print_error "Project name must start with uppercase letter and contain only alphanumeric characters"
        print_info "Example: MyAwesomeApi, TodoApp, WebApi"
        return 1
    fi
    
    if [[ "$name" == "MyProject" ]]; then
        print_error "Please choose a different name than 'MyProject'"
        return 1
    fi
    
    return 0
}

validate_port() {
    local port=$1
    
    if ! [[ "$port" =~ ^[0-9]+$ ]]; then
        print_error "Port must be a number"
        return 1
    fi
    
    if [[ $port -lt 1024 || $port -gt 65530 ]]; then
        print_error "Port must be between 1024 and 65530"
        return 1
    fi
    
    return 0
}

# ─────────────────────────────────────────────────────────────────────────────
# Parse Command Line Arguments
# ─────────────────────────────────────────────────────────────────────────────
PROJECT_NAME=""
BASE_PORT=13000
YES_TO_ALL="false"
CREATE_MIGRATION="ask"
DO_COMMIT="ask"
START_DOCKER="ask"
DELETE_SCRIPTS="ask"

while [[ $# -gt 0 ]]; do
    case $1 in
        -n|--name)
            PROJECT_NAME="$2"
            shift 2
            ;;
        -p|--port)
            BASE_PORT="$2"
            shift 2
            ;;
        -y|--yes)
            YES_TO_ALL="true"
            shift
            ;;
        --no-migration)
            CREATE_MIGRATION="n"
            shift
            ;;
        --no-commit)
            DO_COMMIT="n"
            shift
            ;;
        --no-docker)
            START_DOCKER="n"
            shift
            ;;
        --keep-scripts)
            DELETE_SCRIPTS="n"
            shift
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# ─────────────────────────────────────────────────────────────────────────────
# Main Script
# ─────────────────────────────────────────────────────────────────────────────
clear
print_header "Web API Template Initialization"

# Check prerequisites
print_step "Checking prerequisites..."
check_prerequisites
print_success "All prerequisites found (git, dotnet, docker)"

# ─────────────────────────────────────────────────────────────────────────────
# Configuration Phase
# ─────────────────────────────────────────────────────────────────────────────
print_header "Configuration"

# Project Name
while true; do
    if [[ -z "$PROJECT_NAME" ]]; then
        PROJECT_NAME=$(prompt_value "Project name (e.g., MyAwesomeApi)" "")
    fi
    
    if validate_project_name "$PROJECT_NAME"; then
        break
    fi
    PROJECT_NAME=""
done

# Base Port
while true; do
    if [[ "$YES_TO_ALL" != "true" ]]; then
        BASE_PORT=$(prompt_value "Base port" "$BASE_PORT")
    fi
    
    if validate_port "$BASE_PORT"; then
        break
    fi
done

# Calculate derived ports
FRONTEND_PORT=$BASE_PORT
API_PORT=$((BASE_PORT + 2))
DB_PORT=$((BASE_PORT + 4))

# Additional options (with sensible defaults)
echo ""
print_info "Additional options:"

if [[ "$CREATE_MIGRATION" == "ask" ]]; then
    CREATE_MIGRATION=$(prompt_yn "  Create fresh Initial migration?" "y")
fi

if [[ "$DO_COMMIT" == "ask" ]]; then
    DO_COMMIT=$(prompt_yn "  Auto-commit changes to git?" "y")
fi

if [[ "$START_DOCKER" == "ask" ]]; then
    START_DOCKER=$(prompt_yn "  Start docker compose after setup?" "n")
fi

if [[ "$DELETE_SCRIPTS" == "ask" ]]; then
    DELETE_SCRIPTS=$(prompt_yn "  Delete init scripts when done?" "y")
fi

# ─────────────────────────────────────────────────────────────────────────────
# Summary & Confirmation
# ─────────────────────────────────────────────────────────────────────────────
print_header "Summary"

echo -e "
  ${BOLD}Project Configuration${NC}
  ─────────────────────────────────────
  Project Name:     ${GREEN}$PROJECT_NAME${NC}
  
  ${BOLD}Port Allocation${NC}
  ─────────────────────────────────────
  Frontend:         ${CYAN}$FRONTEND_PORT${NC}
  API:              ${CYAN}$API_PORT${NC}
  Database:         ${CYAN}$DB_PORT${NC}
  
  ${BOLD}Actions${NC}
  ─────────────────────────────────────
  Create migration: $([ "$CREATE_MIGRATION" == "y" ] && echo -e "${GREEN}Yes${NC}" || echo -e "${DIM}No${NC}")
  Git commits:      $([ "$DO_COMMIT" == "y" ] && echo -e "${GREEN}Yes${NC}" || echo -e "${DIM}No${NC}")
  Start docker:     $([ "$START_DOCKER" == "y" ] && echo -e "${GREEN}Yes${NC}" || echo -e "${DIM}No${NC}")
  Delete scripts:   $([ "$DELETE_SCRIPTS" == "y" ] && echo -e "${GREEN}Yes${NC}" || echo -e "${DIM}No${NC}")
"

echo ""
PROCEED=$(prompt_yn "Proceed with initialization?" "y")
if [[ "$PROCEED" != "y" ]]; then
    print_warning "Aborted by user"
    exit 0
fi

# ─────────────────────────────────────────────────────────────────────────────
# Execution Phase
# ─────────────────────────────────────────────────────────────────────────────
print_header "Executing"

# Detect OS for sed compatibility
OS=$(uname)
sed_inplace() {
    if [ "$OS" = "Darwin" ]; then
        sed -i '' "$@"
    else
        sed -i "$@"
    fi
}

# Step 1: Update Docker Ports
print_step "Updating port configuration..."

if [ -f "docker-compose.local.yml" ]; then
    sed_inplace "s/13000:3000/$FRONTEND_PORT:3000/g" docker-compose.local.yml
    sed_inplace "s/13002:8080/$API_PORT:8080/g" docker-compose.local.yml
    sed_inplace "s/13004:5432/$DB_PORT:5432/g" docker-compose.local.yml
    print_substep "Updated docker-compose.local.yml"
fi

if [ -f "src/backend/MyProject.WebApi/appsettings.Development.json" ]; then
    sed_inplace "s/Port=13004/Port=$DB_PORT/g" src/backend/MyProject.WebApi/appsettings.Development.json
    print_substep "Updated appsettings.Development.json"
fi

if [ -f "src/backend/MyProject.WebApi/http-client.env.json" ]; then
    sed_inplace "s/localhost:13002/localhost:$API_PORT/g" src/backend/MyProject.WebApi/http-client.env.json
    print_substep "Updated http-client.env.json"
fi

if [ -f "src/frontend/.env.example" ]; then
    cp src/frontend/.env.example src/frontend/.env.local
    sed_inplace "s/localhost:13002/localhost:$API_PORT/g" src/frontend/.env.local
    print_substep "Created frontend .env.local"
fi

# Update deploy.config.json with new project name
if [ -f "deploy.config.json" ]; then
    NEW_NAME_LOWER=$(echo "$PROJECT_NAME" | tr '[:upper:]' '[:lower:]')
    sed_inplace "s/myproject-api/${NEW_NAME_LOWER}-api/g" deploy.config.json
    sed_inplace "s/myproject-frontend/${NEW_NAME_LOWER}-frontend/g" deploy.config.json
    print_substep "Updated deploy.config.json"
fi

print_success "Port configuration complete"

# Step 2: Rename Project
print_step "Renaming project..."

OLD_NAME="MyProject"
OLD_NAME_LOWER="myproject"
NEW_NAME="$PROJECT_NAME"
NEW_NAME_LOWER=$(echo "$NEW_NAME" | tr '[:upper:]' '[:lower:]')

# Files to exclude from content replacement (binary files, init scripts, git)
EXCLUDE_PATTERNS="--exclude-dir=.git --exclude-dir=bin --exclude-dir=obj --exclude-dir=node_modules --exclude=*.png --exclude=*.jpg --exclude=*.ico --exclude=*.woff --exclude=*.woff2 --exclude=init.sh --exclude=init.ps1"

# Replace text content in files
print_substep "Replacing text content..."
if [ "$OS" = "Darwin" ]; then
    # macOS
    grep -rIl --null "$OLD_NAME" . $EXCLUDE_PATTERNS 2>/dev/null | xargs -0 sed -i '' "s/$OLD_NAME/$NEW_NAME/g" 2>/dev/null || true
    grep -rIl --null "$OLD_NAME_LOWER" . $EXCLUDE_PATTERNS 2>/dev/null | xargs -0 sed -i '' "s/$OLD_NAME_LOWER/$NEW_NAME_LOWER/g" 2>/dev/null || true
else
    # Linux
    grep -rIl --null "$OLD_NAME" . $EXCLUDE_PATTERNS 2>/dev/null | xargs -0 sed -i "s/$OLD_NAME/$NEW_NAME/g" 2>/dev/null || true
    grep -rIl --null "$OLD_NAME_LOWER" . $EXCLUDE_PATTERNS 2>/dev/null | xargs -0 sed -i "s/$OLD_NAME_LOWER/$NEW_NAME_LOWER/g" 2>/dev/null || true
fi

# Rename files and directories (deepest first)
print_substep "Renaming files and directories..."
find . -depth -name "*$OLD_NAME*" \
    -not -path "./.git/*" \
    -not -path "./bin/*" \
    -not -path "./obj/*" \
    -not -path "./node_modules/*" \
    -not -name "init.sh" \
    -not -name "init.ps1" \
    2>/dev/null | while IFS= read -r path; do
    dir=$(dirname "$path")
    filename=$(basename "$path")
    new_filename=$(echo "$filename" | sed "s/$OLD_NAME/$NEW_NAME/g")
    mv "$path" "$dir/$new_filename" 2>/dev/null || true
done

find . -depth -name "*$OLD_NAME_LOWER*" \
    -not -path "./.git/*" \
    -not -path "./bin/*" \
    -not -path "./obj/*" \
    -not -path "./node_modules/*" \
    2>/dev/null | while IFS= read -r path; do
    dir=$(dirname "$path")
    filename=$(basename "$path")
    new_filename=$(echo "$filename" | sed "s/$OLD_NAME_LOWER/$NEW_NAME_LOWER/g")
    mv "$path" "$dir/$new_filename" 2>/dev/null || true
done

print_success "Project renamed to $NEW_NAME"

# Step 3: Git Commit (Rename)
if [[ "$DO_COMMIT" == "y" ]]; then
    print_step "Committing rename changes..."
    git add . >/dev/null 2>&1
    git commit -m "chore: rename project from $OLD_NAME to $NEW_NAME" >/dev/null 2>&1
    print_success "Changes committed"
fi

# Step 4: Create Migration
if [[ "$CREATE_MIGRATION" == "y" ]]; then
    print_step "Creating initial migration..."
    
    MIGRATION_DIR="src/backend/$NEW_NAME.Infrastructure/Features/Postgres/Migrations"
    
    if [ -d "$MIGRATION_DIR" ]; then
        print_substep "Clearing existing migrations..."
        rm -rf "$MIGRATION_DIR"/*
    else
        mkdir -p "$MIGRATION_DIR"
    fi
    
    print_substep "Restoring dotnet tools..."
    # Use explicit config file since root may not have NuGet sources
    dotnet tool restore --configfile "src/backend/NuGet.Config" >/dev/null 2>&1 || dotnet tool restore >/dev/null 2>&1
    
    print_substep "Restoring dependencies..."
    dotnet restore "src/backend/$NEW_NAME.WebApi" >/dev/null 2>&1
    
    print_substep "Building project..."
    dotnet build "src/backend/$NEW_NAME.WebApi" --no-restore -v q >/dev/null 2>&1
    
    print_substep "Running ef migrations add..."
    dotnet ef migrations add Initial \
        --project "src/backend/$NEW_NAME.Infrastructure" \
        --startup-project "src/backend/$NEW_NAME.WebApi" \
        --output-dir Features/Postgres/Migrations \
        --no-build >/dev/null 2>&1
    
    print_success "Migration 'Initial' created"
    
    # Commit migration
    if [[ "$DO_COMMIT" == "y" ]]; then
        print_substep "Committing migration..."
        git add . >/dev/null 2>&1
        git commit -m "chore: add initial database migration" >/dev/null 2>&1
        print_success "Migration committed"
    fi
fi

# Step 5: Delete init scripts
if [[ "$DELETE_SCRIPTS" == "y" ]]; then
    print_step "Cleaning up init scripts..."
    rm -f init.sh init.ps1
    
    if [[ "$DO_COMMIT" == "y" ]]; then
        git add . >/dev/null 2>&1
        git commit -m "chore: remove initialization scripts" >/dev/null 2>&1
    fi
    
    print_success "Init scripts removed"
fi

# Step 6: Start Docker
if [[ "$START_DOCKER" == "y" ]]; then
    print_step "Starting Docker containers..."
    docker compose -f docker-compose.local.yml up -d --build
    print_success "Docker containers started"
fi

# ─────────────────────────────────────────────────────────────────────────────
# Complete!
# ─────────────────────────────────────────────────────────────────────────────
print_header "Setup Complete! 🎉"

echo -e "
  ${BOLD}Your project is ready!${NC}
  
  ${BOLD}Quick Start${NC}
  ─────────────────────────────────────
  ${DIM}# Start the development environment${NC}
  docker compose -f docker-compose.local.yml up -d --build
  
  ${DIM}# Or run the API directly${NC}
  cd src/backend/$NEW_NAME.WebApi
  dotnet run
  
  ${BOLD}URLs${NC}
  ─────────────────────────────────────
  Frontend:  ${CYAN}http://localhost:$FRONTEND_PORT${NC}
  API:       ${CYAN}http://localhost:$API_PORT${NC}
  API Docs:  ${CYAN}http://localhost:$API_PORT/scalar${NC}
  
  ${DIM}Happy coding! 🚀${NC}
"
