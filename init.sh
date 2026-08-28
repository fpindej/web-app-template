#!/bin/bash

#══════════════════════════════════════════════════════════════════════════════
#  Project Initialization Script
#══════════════════════════════════════════════════════════════════════════════
#
#  Usage:
#    Interactive:     ./init.sh
#    Non-interactive: ./init.sh --name MyProject --port 14000 --yes
#
#  Flags:
#    --name, -n     Project name (required in non-interactive mode)
#    --port, -p     Base port (default: 13000)
#    --email, -e    Superuser email (default: superuser@test.com)
#    --password     Superuser password (default: Superuser123!)
#    --yes, -y      Accept all defaults, no prompts
#    --no-migration Skip migration creation
#    --no-build     Skip building and running tests
#    --no-commit    Skip git commits
#    --no-aspire    Don't launch Aspire after setup
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
CYAN='\033[0;36m'
BOLD='\033[1m'
DIM='\033[2m'
NC='\033[0m'

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

prompt_yn() {
    local question=$1
    local default=$2

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
    echo "Project Initialization Script"
    echo ""
    echo "Usage:"
    echo "  ./init.sh                     Interactive mode"
    echo "  ./init.sh [options]           Non-interactive mode"
    echo ""
    echo "Options:"
    echo "  -n, --name NAME       Project name (e.g., MyAwesomeApi)"
    echo "  -p, --port PORT       Base port for services (default: 13000)"
    echo "  -e, --email EMAIL     Superuser email (default: superuser@test.com)"
    echo "      --password PASS   Superuser password (default: Superuser123!)"
    echo "  -y, --yes             Accept all defaults without prompting"
    echo "      --no-migration    Skip creating initial migration"
    echo "      --no-build        Skip building and running tests"
    echo "      --no-commit       Skip git commits"
    echo "      --no-aspire       Don't launch Aspire after setup"
    echo "  -h, --help            Show this help message"
    echo ""
    echo "Port allocation:"
    echo "  Frontend:   BASE_PORT      (e.g., 13000)"
    echo "  API:        BASE_PORT + 2  (e.g., 13002)"
    echo "  (Infrastructure ports are managed automatically by Aspire)"
    echo ""
    echo "Examples:"
    echo "  ./init.sh --name MyApi --port 14000 --yes"
    echo "  ./init.sh -n MyApi -y"
    echo "  ./init.sh -n MyApi --email me@example.com --password MyPass123!"
}

check_prerequisites() {
    local missing=()

    command -v git >/dev/null 2>&1 || missing+=("git")
    command -v dotnet >/dev/null 2>&1 || missing+=("dotnet")

    if command -v docker &>/dev/null; then
        if docker info &>/dev/null; then
            : # Docker running, ok
        else
            missing+=("docker (installed but not running)")
        fi
    else
        missing+=("docker")
    fi

    command -v node >/dev/null 2>&1 || missing+=("node")
    command -v pnpm >/dev/null 2>&1 || missing+=("pnpm")

    if [[ ${#missing[@]} -gt 0 ]]; then
        print_error "Missing required tools: ${missing[*]}"
        echo "Please install them before running this script."
        if [[ " ${missing[*]} " == *" pnpm "* ]]; then
            print_info "pnpm is managed via corepack. Run: corepack enable"
        fi
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

    return 0
}

validate_port() {
    local port=$1

    if ! [[ "$port" =~ ^[0-9]+$ ]]; then
        print_error "Port must be a number"
        return 1
    fi

    if [[ $port -lt 1024 || $port -gt 65527 ]]; then
        print_error "Port must be between 1024 and 65527"
        return 1
    fi

    return 0
}

# ─────────────────────────────────────────────────────────────────────────────
# Interactive Checklist
# ─────────────────────────────────────────────────────────────────────────────
# Renders a toggleable checklist. User presses 1-N to toggle, Enter to confirm.
# Arguments: option labels as positional args
# Globals:  CHECKLIST_DEFAULTS (array of 0/1 for initial state)
# Returns:  CHECKLIST_RESULTS (array of 0/1)
prompt_checklist() {
    local options=("$@")
    local count=${#options[@]}
    local selected=()

    # Initialize from defaults
    for ((i = 0; i < count; i++)); do
        selected[$i]=${CHECKLIST_DEFAULTS[$i]:-1}
    done

    # Non-interactive: just use defaults
    if [[ "$YES_TO_ALL" == "true" ]]; then
        CHECKLIST_RESULTS=("${selected[@]}")
        return
    fi

    local first_draw=true

    while true; do
        # Clear previous draw
        if [[ "$first_draw" != "true" ]]; then
            local lines_up=$((count + 3))
            for ((j = 0; j < lines_up; j++)); do
                echo -en "\033[A\033[2K"
            done
        fi
        first_draw=false

        echo ""
        echo -e "${BOLD}  Press 1-${count} to toggle, Enter to confirm:${NC}"
        echo ""

        for ((i = 0; i < count; i++)); do
            local num=$((i + 1))
            if [[ "${selected[$i]}" == "1" ]]; then
                echo -e "  ${GREEN}[${num}] ✓${NC} ${options[$i]}"
            else
                echo -e "  ${DIM}[${num}] ○${NC} ${options[$i]}"
            fi
        done

        # Single keypress - no Enter needed to toggle
        read -rsn1 choice

        # Enter (empty) confirms selection
        if [[ -z "$choice" ]]; then
            echo ""
            break
        fi

        if [[ "$choice" =~ ^[0-9]$ ]] && [[ $choice -ge 1 ]] && [[ $choice -le $count ]]; then
            local idx=$((choice - 1))
            if [[ "${selected[$idx]}" == "1" ]]; then
                selected[$idx]=0
            else
                selected[$idx]=1
            fi
        fi
    done

    CHECKLIST_RESULTS=("${selected[@]}")
}

# ─────────────────────────────────────────────────────────────────────────────
# Parse Command Line Arguments
# ─────────────────────────────────────────────────────────────────────────────
PROJECT_NAME=""
BASE_PORT=13000
ADMIN_EMAIL="superuser@test.com"
ADMIN_PASSWORD="Superuser123!"
YES_TO_ALL="false"
CREATE_MIGRATION="ask"
BUILD_TEST="ask"
DO_COMMIT="ask"
START_ASPIRE="ask"

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
        -e|--email)
            ADMIN_EMAIL="$2"
            shift 2
            ;;
        --password)
            ADMIN_PASSWORD="$2"
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
        --no-build)
            BUILD_TEST="n"
            shift
            ;;
        --no-commit)
            DO_COMMIT="n"
            shift
            ;;
        --no-aspire)
            START_ASPIRE="n"
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
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

START_TIME=$(date +%s)

echo ""
print_header "Project Initialization"

# Verify we're in the project root
if [[ ! -d "src/backend" || ! -d "src/frontend" ]]; then
    print_error "This script must be run from the project root directory."
    print_info "Expected to find src/backend and src/frontend directories."
    exit 1
fi

# Check prerequisites
print_step "Checking prerequisites..."
check_prerequisites
print_success "All prerequisites found (git, dotnet, docker, node, pnpm via corepack)"

# ─────────────────────────────────────────────────────────────────────────────
# Step 1: Project Name
# ─────────────────────────────────────────────────────────────────────────────
print_step "Project setup"
echo ""

while true; do
    if [[ -z "$PROJECT_NAME" ]]; then
        PROJECT_NAME=$(prompt_value "Project name (PascalCase, e.g. MyAwesomeApi)" "")
    fi

    if validate_project_name "$PROJECT_NAME"; then
        break
    fi
    PROJECT_NAME=""
done

# ─────────────────────────────────────────────────────────────────────────────
# Step 2: Base Port
# ─────────────────────────────────────────────────────────────────────────────
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

# Show port allocation
echo ""
echo -e "  ${BOLD}Port allocation${NC}"
echo -e "  ─────────────────────────────────────"
echo -e "  Frontend:     ${CYAN}$FRONTEND_PORT${NC}"
echo -e "  API:          ${CYAN}$API_PORT${NC}"

# ─────────────────────────────────────────────────────────────────────────────
# Step 3: Superuser Credentials
# ─────────────────────────────────────────────────────────────────────────────
echo ""
ADMIN_EMAIL=$(prompt_value "Superuser email" "$ADMIN_EMAIL")

if [[ "$YES_TO_ALL" == "true" ]]; then
    : # keep ADMIN_PASSWORD as-is (from CLI flag or default)
else
    read -sp "$(echo -e "${BOLD}Superuser password [${ADMIN_PASSWORD}]${NC}: ")" ADMIN_PASSWORD_INPUT
    echo
    ADMIN_PASSWORD="${ADMIN_PASSWORD_INPUT:-$ADMIN_PASSWORD}"
fi

# Convert PascalCase to kebab-case (MyAwesomeApi -> my-awesome-api)
to_kebab_case() {
    echo "$1" | sed 's/\([a-z]\)\([A-Z]\)/\1-\2/g' | tr '[:upper:]' '[:lower:]'
}
PROJECT_SLUG=$(to_kebab_case "$PROJECT_NAME")

# ─────────────────────────────────────────────────────────────────────────────
# Step 3: Options Checklist
# ─────────────────────────────────────────────────────────────────────────────
# Determine which options to ask about
CHECKLIST_OPTIONS=()
CHECKLIST_DEFAULTS=()
CHECKLIST_MAP=()

if [[ "$CREATE_MIGRATION" == "ask" ]]; then
    CHECKLIST_OPTIONS+=("Create initial database migration")
    CHECKLIST_DEFAULTS+=(1)
    CHECKLIST_MAP+=("migration")
fi

if [[ "$BUILD_TEST" == "ask" ]]; then
    CHECKLIST_OPTIONS+=("Build and run tests")
    CHECKLIST_DEFAULTS+=(1)
    CHECKLIST_MAP+=("build")
fi

if [[ "$DO_COMMIT" == "ask" ]]; then
    CHECKLIST_OPTIONS+=("Auto-commit changes to git")
    CHECKLIST_DEFAULTS+=(1)
    CHECKLIST_MAP+=("commit")
fi

if [[ "$START_ASPIRE" == "ask" ]]; then
    CHECKLIST_OPTIONS+=("Launch Aspire after setup")
    CHECKLIST_DEFAULTS+=(1)
    CHECKLIST_MAP+=("aspire")
fi

# Only show checklist if there are options to configure
if [[ ${#CHECKLIST_OPTIONS[@]} -gt 0 ]]; then
    prompt_checklist "${CHECKLIST_OPTIONS[@]}"

    # Map results back to variables
    for ((i = 0; i < ${#CHECKLIST_MAP[@]}; i++)); do
        case "${CHECKLIST_MAP[$i]}" in
            migration)
                [[ "${CHECKLIST_RESULTS[$i]}" == "1" ]] && CREATE_MIGRATION="y" || CREATE_MIGRATION="n"
                ;;
            build)
                [[ "${CHECKLIST_RESULTS[$i]}" == "1" ]] && BUILD_TEST="y" || BUILD_TEST="n"
                ;;
            commit)
                [[ "${CHECKLIST_RESULTS[$i]}" == "1" ]] && DO_COMMIT="y" || DO_COMMIT="n"
                ;;
            aspire)
                [[ "${CHECKLIST_RESULTS[$i]}" == "1" ]] && START_ASPIRE="y" || START_ASPIRE="n"
                ;;
        esac
    done
fi

# Apply defaults for any options set via CLI flags
[[ "$CREATE_MIGRATION" == "ask" ]] && CREATE_MIGRATION="y"
[[ "$BUILD_TEST" == "ask" ]] && BUILD_TEST="y"
[[ "$DO_COMMIT" == "ask" ]] && DO_COMMIT="y"
[[ "$START_ASPIRE" == "ask" ]] && START_ASPIRE="y"

# ─────────────────────────────────────────────────────────────────────────────
# Summary & Confirmation
# ─────────────────────────────────────────────────────────────────────────────
print_header "Summary"

echo -e "
  ${BOLD}Project${NC}
  ─────────────────────────────────────
  Name:             ${GREEN}$PROJECT_NAME${NC}
  Slug:             ${GREEN}$PROJECT_SLUG${NC}

  ${BOLD}Ports${NC}
  ─────────────────────────────────────
  Frontend:         ${CYAN}$FRONTEND_PORT${NC}
  API:              ${CYAN}$API_PORT${NC}

  ${BOLD}Superuser${NC}
  ─────────────────────────────────────
  Email:            ${CYAN}$ADMIN_EMAIL${NC}
  Password:         ${CYAN}$ADMIN_PASSWORD${NC}

  ${BOLD}Options${NC}
  ─────────────────────────────────────
  Create migration: $([ "$CREATE_MIGRATION" == "y" ] && echo -e "${GREEN}Yes${NC}" || echo -e "${DIM}No${NC}")
  Build and test:   $([ "$BUILD_TEST" == "y" ] && echo -e "${GREEN}Yes${NC}" || echo -e "${DIM}No${NC}")
  Git commits:      $([ "$DO_COMMIT" == "y" ] && echo -e "${GREEN}Yes${NC}" || echo -e "${DIM}No${NC}")
  Launch Aspire:    $([ "$START_ASPIRE" == "y" ] && echo -e "${GREEN}Yes${NC}" || echo -e "${DIM}No${NC}")
"

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

# Files to exclude from content replacement (binary files, init scripts, git)
EXCLUDE_PATTERNS="--exclude-dir=.git --exclude-dir=bin --exclude-dir=obj --exclude-dir=node_modules --exclude=*.png --exclude=*.jpg --exclude=*.ico --exclude=*.woff --exclude=*.woff2 --exclude=init.sh --exclude=init.ps1"

# Step 1: Update Ports (substitute placeholders across all files)
print_step "Updating port configuration..."

if [ -f "src/frontend/.env.example" ]; then
    cp src/frontend/.env.example src/frontend/.env.local
    print_substep "Created frontend .env.local from .env.example"
fi

# Generate random secrets
JWT_SECRET=$(openssl rand -base64 64 | tr -d '\n/+=' | cut -c1-64)
ENCRYPTION_KEY=$(openssl rand -base64 64 | tr -d '\n/+=' | cut -c1-64)

# Escape special sed characters in the password (/, &, \)
ESCAPED_ADMIN_PASSWORD=$(printf '%s\n' "$ADMIN_PASSWORD" | sed 's/[&/\]/\\&/g')

print_substep "Replacing placeholders..."
if [ "$OS" = "Darwin" ]; then
    grep -rIl --null "{INIT_FRONTEND_PORT}\|{INIT_API_PORT}\|{INIT_PROJECT_SLUG}\|{INIT_JWT_SECRET}\|{INIT_ENCRYPTION_KEY}\|{INIT_SUPERUSER_EMAIL}\|{INIT_SUPERUSER_PASSWORD}" . $EXCLUDE_PATTERNS 2>/dev/null | xargs -0 sed -i '' \
        -e "s/{INIT_FRONTEND_PORT}/$FRONTEND_PORT/g" \
        -e "s/{INIT_API_PORT}/$API_PORT/g" \
        -e "s/{INIT_PROJECT_SLUG}/$PROJECT_SLUG/g" \
        -e "s/{INIT_JWT_SECRET}/$JWT_SECRET/g" \
        -e "s/{INIT_ENCRYPTION_KEY}/$ENCRYPTION_KEY/g" \
        -e "s/{INIT_SUPERUSER_EMAIL}/$ADMIN_EMAIL/g" \
        -e "s/{INIT_SUPERUSER_PASSWORD}/$ESCAPED_ADMIN_PASSWORD/g" 2>/dev/null || true
else
    grep -rIl --null "{INIT_FRONTEND_PORT}\|{INIT_API_PORT}\|{INIT_PROJECT_SLUG}\|{INIT_JWT_SECRET}\|{INIT_ENCRYPTION_KEY}\|{INIT_SUPERUSER_EMAIL}\|{INIT_SUPERUSER_PASSWORD}" . $EXCLUDE_PATTERNS 2>/dev/null | xargs -0 sed -i \
        -e "s/{INIT_FRONTEND_PORT}/$FRONTEND_PORT/g" \
        -e "s/{INIT_API_PORT}/$API_PORT/g" \
        -e "s/{INIT_PROJECT_SLUG}/$PROJECT_SLUG/g" \
        -e "s/{INIT_JWT_SECRET}/$JWT_SECRET/g" \
        -e "s/{INIT_ENCRYPTION_KEY}/$ENCRYPTION_KEY/g" \
        -e "s/{INIT_SUPERUSER_EMAIL}/$ADMIN_EMAIL/g" \
        -e "s/{INIT_SUPERUSER_PASSWORD}/$ESCAPED_ADMIN_PASSWORD/g" 2>/dev/null || true
fi

print_success "Port configuration complete"

# Commit port configuration changes
if [[ "$DO_COMMIT" == "y" ]]; then
    print_step "Committing port configuration..."
    git add . >/dev/null 2>&1
    git commit -m "chore: configure project (slug: $PROJECT_SLUG, ports: $FRONTEND_PORT/$API_PORT)" >/dev/null 2>&1
    print_success "Port configuration committed"
fi

# Project name variables (needed throughout the script)
OLD_NAME="MyProject"
OLD_NAME_LOWER="myproject"
NEW_NAME="$PROJECT_NAME"
NEW_NAME_LOWER=$(echo "$NEW_NAME" | tr '[:upper:]' '[:lower:]')

# Step 2: Rename Project (skip if name is already MyProject)
if [[ "$PROJECT_NAME" == "MyProject" ]]; then
    print_step "Skipping rename (project name is already MyProject)"
else
    print_step "Renaming project..."

    print_substep "Replacing text content..."
    if [ "$OS" = "Darwin" ]; then
        grep -rIl --null "$OLD_NAME" . $EXCLUDE_PATTERNS 2>/dev/null | xargs -0 sed -i '' "s/$OLD_NAME/$NEW_NAME/g" 2>/dev/null || true
        grep -rIl --null "$OLD_NAME_LOWER" . $EXCLUDE_PATTERNS 2>/dev/null | xargs -0 sed -i '' "s/$OLD_NAME_LOWER/$NEW_NAME_LOWER/g" 2>/dev/null || true
    else
        grep -rIl --null "$OLD_NAME" . $EXCLUDE_PATTERNS 2>/dev/null | xargs -0 sed -i "s/$OLD_NAME/$NEW_NAME/g" 2>/dev/null || true
        grep -rIl --null "$OLD_NAME_LOWER" . $EXCLUDE_PATTERNS 2>/dev/null | xargs -0 sed -i "s/$OLD_NAME_LOWER/$NEW_NAME_LOWER/g" 2>/dev/null || true
    fi

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
fi

# Step 4: Create Migration
if [[ "$CREATE_MIGRATION" == "y" ]]; then
    print_step "Creating initial migration..."

    MIGRATION_DIR="src/backend/$NEW_NAME.Infrastructure/Persistence/Migrations"

    if [ -d "$MIGRATION_DIR" ]; then
        print_substep "Clearing existing migrations..."
        rm -rf "$MIGRATION_DIR"/*
    else
        mkdir -p "$MIGRATION_DIR"
    fi

    print_substep "Restoring dotnet tools..."
    # Use explicit config file since root may not have NuGet sources, fallback to default
    if ! dotnet tool restore --configfile "src/backend/nuget.config" >/dev/null 2>&1; then
        dotnet tool restore >/dev/null 2>&1 || true
    fi

    print_substep "Restoring dependencies..."
    if ! dotnet restore "src/backend/$NEW_NAME.WebApi" >/dev/null 2>&1; then
        print_error "Failed to restore dependencies"
        print_info "You can run manually: dotnet restore src/backend/$NEW_NAME.WebApi"
    fi

    print_substep "Building project..."
    if ! dotnet build "src/backend/$NEW_NAME.WebApi" --no-restore -v q >/dev/null 2>&1; then
        print_error "Build failed. Migration will be skipped."
        print_info "Fix build errors and run manually:"
        print_info "  dotnet ef migrations add Initial \\"
        print_info "    --project src/backend/$NEW_NAME.Infrastructure \\"
        print_info "    --startup-project src/backend/$NEW_NAME.WebApi \\"
        print_info "    --output-dir Persistence/Migrations"
    else
        print_substep "Running ef migrations add..."
        if dotnet ef migrations add Initial \
            --project "src/backend/$NEW_NAME.Infrastructure" \
            --startup-project "src/backend/$NEW_NAME.WebApi" \
            --output-dir Persistence/Migrations \
            --no-build >/dev/null 2>&1; then

            print_success "Migration 'Initial' created"

            # Commit migration
            if [[ "$DO_COMMIT" == "y" ]]; then
                print_substep "Committing migration..."
                git add . >/dev/null 2>&1
                git commit -m "chore: add initial database migration" >/dev/null 2>&1
                print_success "Migration committed"
            fi
        else
            print_error "Migration creation failed"
            print_info "Run manually after fixing any issues:"
            print_info "  dotnet ef migrations add Initial \\"
            print_info "    --project src/backend/$NEW_NAME.Infrastructure \\"
            print_info "    --startup-project src/backend/$NEW_NAME.WebApi \\"
            print_info "    --output-dir Persistence/Migrations"
        fi
    fi
fi

# Step 5: Build and Run Tests
if [[ "$BUILD_TEST" == "y" ]]; then
    print_step "Building solution..."
    if dotnet build "src/backend/$NEW_NAME.slnx" -c Debug --verbosity quiet; then
        print_success "Build succeeded"
    else
        print_error "Build failed"
    fi

    print_step "Running tests..."
    if dotnet test "src/backend/$NEW_NAME.slnx" -c Release --verbosity quiet --no-restore; then
        print_success "All tests passed"
    else
        print_warning "Some tests failed - check output above"
    fi
fi

# Step 6: Delete template-specific files (always, fire and forget)
print_step "Cleaning up template files..."

TEMPLATE_FILES=(
    "init.sh"
    "init.ps1"
    ".github/workflows/claude.yml"
    ".github/workflows/claude-code-review.yml"
    ".github/workflows/init-scripts.yml"
)

TEMPLATE_DIRS=(
    "docs/sessions/assets"
    ".claude/skills/init-verify"
)

# Dated session docs are template history; the README stays so the
# session-doc convention keeps working in the initialized project.
if git rev-parse --git-dir > /dev/null 2>&1; then
    for f in "${TEMPLATE_FILES[@]}"; do
        git rm -f "$f" >/dev/null 2>&1 || rm -f "$f"
    done
    for d in "${TEMPLATE_DIRS[@]}"; do
        git rm -rf "$d" >/dev/null 2>&1 || rm -rf "$d"
    done
    git rm -f "docs/sessions/2*.md" >/dev/null 2>&1 || rm -f docs/sessions/2*.md
else
    for f in "${TEMPLATE_FILES[@]}"; do
        rm -f "$f"
    done
    for d in "${TEMPLATE_DIRS[@]}"; do
        rm -rf "$d"
    done
    rm -f docs/sessions/2*.md
fi

if [[ "$DO_COMMIT" == "y" ]]; then
    git commit -m "chore: remove template-specific files" >/dev/null 2>&1
fi

print_success "Template files removed"

# ─────────────────────────────────────────────────────────────────────────────
# Complete!
# ─────────────────────────────────────────────────────────────────────────────
print_header "Setup Complete!"

if [[ "$START_ASPIRE" == "y" ]]; then
    echo -e "
  ${BOLD}Your project is ready!${NC}

  ${DIM}Completed in $(($(date +%s) - START_TIME))s${NC}
"
    print_step "Launching Aspire..."
    echo -e "  ${DIM}Opening Aspire Dashboard in your browser. Press Ctrl+C to stop.${NC}"
    echo ""
    (for i in $(seq 1 30); do
        if curl -s -o /dev/null -w '' http://localhost:15244 2>/dev/null; then
            if command -v open &>/dev/null; then open "http://localhost:15244";
            elif command -v xdg-open &>/dev/null; then xdg-open "http://localhost:15244"; fi
            break
        fi
        sleep 1
    done) &
    DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true exec dotnet run --project "src/backend/$NEW_NAME.AppHost"
else
    echo -e "
  ${BOLD}Your project is ready!${NC}

  ${BOLD}Quick Start${NC}
  ─────────────────────────────────────
  dotnet run --project src/backend/$NEW_NAME.AppHost

  The Aspire Dashboard URL appears in the console.
  All service URLs (API, pgAdmin, MinIO) are visible in the Dashboard.

  ${DIM}Completed in $(($(date +%s) - START_TIME))s${NC}

  ${DIM}Happy coding!${NC}
"
fi
