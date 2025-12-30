#!/bin/bash

#══════════════════════════════════════════════════════════════════════════════
#  Web API Template - Unified Deploy Script
#══════════════════════════════════════════════════════════════════════════════
#
#  Usage:
#    Interactive:     ./deploy.sh
#    Direct deploy:   ./deploy.sh backend|frontend|all [options]
#
#  Options:
#    --patch         Bump patch version (default): 0.1.0 → 0.1.1
#    --minor         Bump minor version: 0.1.0 → 0.2.0
#    --major         Bump major version: 0.1.0 → 1.0.0
#    --no-bump       Don't increment version (rebuild same tag)
#    --no-push       Build only, don't push to registry
#    --no-latest     Don't update :latest tag
#    --yes, -y       Skip confirmation prompts
#    --help, -h      Show this help
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
    echo "${answer,,}"
}

prompt_value() {
    local question=$1
    local default=$2
    
    local prompt_text="$question"
    if [[ -n "$default" ]]; then
        prompt_text="$question [${default}]"
    fi
    
    read -p "$(echo -e "${BOLD}$prompt_text${NC}: ")" answer
    echo "${answer:-$default}"
}

show_help() {
    echo "Web API Template - Unified Deploy Script"
    echo ""
    echo "Usage:"
    echo "  ./deploy.sh                   Interactive mode (menu)"
    echo "  ./deploy.sh <target>          Deploy specific target"
    echo ""
    echo "Targets:"
    echo "  backend                       Deploy backend API only"
    echo "  frontend                      Deploy frontend only"
    echo "  all                           Deploy both"
    echo ""
    echo "Options:"
    echo "  --patch                       Bump patch version: 0.1.0 → 0.1.1 (default)"
    echo "  --minor                       Bump minor version: 0.1.0 → 0.2.0"
    echo "  --major                       Bump major version: 0.1.0 → 1.0.0"
    echo "  --no-bump                     Keep current version (rebuild)"
    echo "  --no-push                     Build only, don't push to registry"
    echo "  --no-latest                   Don't update :latest tag"
    echo "  -y, --yes                     Skip confirmation prompts"
    echo "  -h, --help                    Show this help message"
    echo ""
    echo "Examples:"
    echo "  ./deploy.sh backend --minor   Deploy backend with minor version bump"
    echo "  ./deploy.sh all --no-push     Build both without pushing"
    echo "  ./deploy.sh frontend -y       Deploy frontend, skip prompts"
}

# ─────────────────────────────────────────────────────────────────────────────
# Version Management
# ─────────────────────────────────────────────────────────────────────────────
bump_version() {
    local version=$1
    local bump_type=$2
    
    local major minor patch
    IFS='.' read -r major minor patch <<< "$version"
    
    case $bump_type in
        major)
            major=$((major + 1))
            minor=0
            patch=0
            ;;
        minor)
            minor=$((minor + 1))
            patch=0
            ;;
        patch)
            patch=$((patch + 1))
            ;;
    esac
    
    echo "$major.$minor.$patch"
}

# ─────────────────────────────────────────────────────────────────────────────
# Configuration Management
# ─────────────────────────────────────────────────────────────────────────────
CONFIG_FILE="deploy.config.json"

create_default_config() {
    # Try to detect project name from directory structure
    local detected_name=""
    if [ -d "src/backend" ]; then
        detected_name=$(find src/backend -maxdepth 1 -type d -name "*.WebApi" 2>/dev/null | head -1 | xargs basename 2>/dev/null | sed 's/.WebApi//' || echo "")
    fi
    detected_name=${detected_name:-"myproject"}
    local detected_lower=$(echo "$detected_name" | tr '[:upper:]' '[:lower:]')
    
    cat > "$CONFIG_FILE" << EOF
{
  "registry": "myusername",
  "backendImage": "${detected_lower}-api",
  "frontendImage": "${detected_lower}-frontend",
  "version": "0.1.0",
  "platform": "linux/amd64"
}
EOF
}

read_config() {
    if [ ! -f "$CONFIG_FILE" ]; then
        print_warning "Config file not found. Creating default..."
        create_default_config
    fi
    
    # Parse JSON (portable way without jq dependency)
    REGISTRY=$(grep -o '"registry"[[:space:]]*:[[:space:]]*"[^"]*"' "$CONFIG_FILE" | sed 's/.*: *"\([^"]*\)"/\1/')
    BACKEND_IMAGE=$(grep -o '"backendImage"[[:space:]]*:[[:space:]]*"[^"]*"' "$CONFIG_FILE" | sed 's/.*: *"\([^"]*\)"/\1/')
    FRONTEND_IMAGE=$(grep -o '"frontendImage"[[:space:]]*:[[:space:]]*"[^"]*"' "$CONFIG_FILE" | sed 's/.*: *"\([^"]*\)"/\1/')
    VERSION=$(grep -o '"version"[[:space:]]*:[[:space:]]*"[^"]*"' "$CONFIG_FILE" | sed 's/.*: *"\([^"]*\)"/\1/')
    PLATFORM=$(grep -o '"platform"[[:space:]]*:[[:space:]]*"[^"]*"' "$CONFIG_FILE" | sed 's/.*: *"\([^"]*\)"/\1/')
}

save_config() {
    cat > "$CONFIG_FILE" << EOF
{
  "registry": "$REGISTRY",
  "backendImage": "$BACKEND_IMAGE",
  "frontendImage": "$FRONTEND_IMAGE",
  "version": "$VERSION",
  "platform": "$PLATFORM"
}
EOF
}

configure_registry() {
    print_header "Deploy Configuration"
    
    echo ""
    print_info "Current configuration:"
    echo -e "  Registry:       ${CYAN}$REGISTRY${NC}"
    echo -e "  Backend Image:  ${CYAN}$BACKEND_IMAGE${NC}"
    echo -e "  Frontend Image: ${CYAN}$FRONTEND_IMAGE${NC}"
    echo -e "  Platform:       ${CYAN}$PLATFORM${NC}"
    echo -e "  Version:        ${CYAN}$VERSION${NC}"
    echo ""
    
    local reconfigure=$(prompt_yn "Reconfigure settings?" "n")
    
    if [[ "$reconfigure" == "y" ]]; then
        echo ""
        REGISTRY=$(prompt_value "Docker registry (e.g., myusername, ghcr.io/myuser)" "$REGISTRY")
        BACKEND_IMAGE=$(prompt_value "Backend image name" "$BACKEND_IMAGE")
        FRONTEND_IMAGE=$(prompt_value "Frontend image name" "$FRONTEND_IMAGE")
        PLATFORM=$(prompt_value "Target platform" "$PLATFORM")
        save_config
        print_success "Configuration saved"
    fi
}

# ─────────────────────────────────────────────────────────────────────────────
# Build Functions
# ─────────────────────────────────────────────────────────────────────────────
check_docker() {
    if ! docker system info > /dev/null 2>&1; then
        print_error "Docker is not running"
        exit 1
    fi
    
    # Setup buildx if needed
    if ! docker buildx inspect default > /dev/null 2>&1; then
        docker buildx create --use > /dev/null 2>&1
    fi
}

build_backend() {
    local version=$1
    local push=$2
    local tag_latest=$3
    
    print_step "Building Backend API..."
    
    local full_image="$REGISTRY/$BACKEND_IMAGE"
    
    # Find the WebApi directory
    local webapi_dir=$(find src/backend -maxdepth 1 -type d -name "*.WebApi" 2>/dev/null | head -1)
    if [ -z "$webapi_dir" ]; then
        print_error "WebApi directory not found in src/backend"
        return 1
    fi
    
    local dockerfile="$webapi_dir/Dockerfile"
    if [ ! -f "$dockerfile" ]; then
        print_error "Dockerfile not found: $dockerfile"
        return 1
    fi
    
    print_substep "Image: $full_image:$version"
    
    local build_args="--platform $PLATFORM -t $full_image:$version"
    
    if [[ "$tag_latest" == "true" ]]; then
        build_args="$build_args -t $full_image:latest"
    fi
    
    if [[ "$push" == "true" ]]; then
        build_args="$build_args --push"
    else
        build_args="$build_args --load"
    fi
    
    cd src/backend
    if ! docker buildx build $build_args -f "$(basename "$webapi_dir")/Dockerfile" . 2>&1; then
        cd ../..
        print_error "Backend build failed"
        return 1
    fi
    cd ../..
    
    if [[ "$push" == "true" ]]; then
        print_success "Backend pushed: $full_image:$version"
    else
        print_success "Backend built: $full_image:$version"
    fi
}

build_frontend() {
    local version=$1
    local push=$2
    local tag_latest=$3
    
    print_step "Building Frontend..."
    
    local full_image="$REGISTRY/$FRONTEND_IMAGE"
    
    if [ ! -f "src/frontend/Dockerfile" ]; then
        print_error "Dockerfile not found: src/frontend/Dockerfile"
        return 1
    fi
    
    print_substep "Image: $full_image:$version"
    
    local build_args="--platform $PLATFORM -t $full_image:$version"
    
    if [[ "$tag_latest" == "true" ]]; then
        build_args="$build_args -t $full_image:latest"
    fi
    
    if [[ "$push" == "true" ]]; then
        build_args="$build_args --push"
    else
        build_args="$build_args --load"
    fi
    
    cd src/frontend
    if ! docker buildx build $build_args . 2>&1; then
        cd ../..
        print_error "Frontend build failed"
        return 1
    fi
    cd ../..
    
    if [[ "$push" == "true" ]]; then
        print_success "Frontend pushed: $full_image:$version"
    else
        print_success "Frontend built: $full_image:$version"
    fi
}

# ─────────────────────────────────────────────────────────────────────────────
# Parse Command Line Arguments
# ─────────────────────────────────────────────────────────────────────────────
TARGET=""
BUMP_TYPE="patch"
DO_PUSH="true"
TAG_LATEST="true"
YES_TO_ALL="false"

while [[ $# -gt 0 ]]; do
    case $1 in
        backend|frontend|all)
            TARGET="$1"
            shift
            ;;
        --patch)
            BUMP_TYPE="patch"
            shift
            ;;
        --minor)
            BUMP_TYPE="minor"
            shift
            ;;
        --major)
            BUMP_TYPE="major"
            shift
            ;;
        --no-bump)
            BUMP_TYPE="none"
            shift
            ;;
        --no-push)
            DO_PUSH="false"
            shift
            ;;
        --no-latest)
            TAG_LATEST="false"
            shift
            ;;
        -y|--yes)
            YES_TO_ALL="true"
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
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

print_header "Deploy"

# Check prerequisites
print_step "Checking prerequisites..."
check_docker
print_success "Docker is running"

# Load configuration
read_config

# Check if registry is default
if [ "$REGISTRY" = "myusername" ]; then
    print_warning "Registry is set to default 'myusername'"
    configure_registry
    read_config
fi

# Interactive target selection if not specified
if [ -z "$TARGET" ]; then
    configure_registry
    
    echo ""
    echo -e "${BOLD}What would you like to deploy?${NC}"
    echo ""
    echo "  [1] Backend API"
    echo "  [2] Frontend"
    echo "  [3] Both"
    echo ""
    read -p "$(echo -e "${BOLD}Choose [1-3]${NC}: ")" choice
    
    case $choice in
        1) TARGET="backend" ;;
        2) TARGET="frontend" ;;
        3) TARGET="all" ;;
        *)
            print_error "Invalid choice"
            exit 1
            ;;
    esac
fi

# Calculate new version
NEW_VERSION=$VERSION
if [[ "$BUMP_TYPE" != "none" ]]; then
    NEW_VERSION=$(bump_version "$VERSION" "$BUMP_TYPE")
fi

# Summary
print_header "Summary"

echo ""
echo -e "  ${BOLD}Deploy Target${NC}"
echo -e "  ─────────────────────────────────────"
case $TARGET in
    backend)  echo -e "  Target:   ${CYAN}Backend API${NC}" ;;
    frontend) echo -e "  Target:   ${CYAN}Frontend${NC}" ;;
    all)      echo -e "  Target:   ${CYAN}Backend + Frontend${NC}" ;;
esac
echo ""
echo -e "  ${BOLD}Version${NC}"
echo -e "  ─────────────────────────────────────"
echo -e "  Current:  ${DIM}$VERSION${NC}"
echo -e "  New:      ${GREEN}$NEW_VERSION${NC}"
echo ""
echo -e "  ${BOLD}Options${NC}"
echo -e "  ─────────────────────────────────────"
echo -e "  Push to registry: $([ "$DO_PUSH" == "true" ] && echo -e "${GREEN}Yes${NC}" || echo -e "${YELLOW}No (build only)${NC}")"
echo -e "  Update :latest:   $([ "$TAG_LATEST" == "true" ] && echo -e "${GREEN}Yes${NC}" || echo -e "${DIM}No${NC}")"
echo ""

# Confirmation
PROCEED=$(prompt_yn "Proceed with deployment?" "y")
if [[ "$PROCEED" != "y" ]]; then
    print_warning "Aborted by user"
    exit 0
fi

# Execute
print_header "Building"

FAILED="false"

if [[ "$TARGET" == "backend" || "$TARGET" == "all" ]]; then
    if ! build_backend "$NEW_VERSION" "$DO_PUSH" "$TAG_LATEST"; then
        FAILED="true"
    fi
fi

if [[ "$TARGET" == "frontend" || "$TARGET" == "all" ]]; then
    if ! build_frontend "$NEW_VERSION" "$DO_PUSH" "$TAG_LATEST"; then
        FAILED="true"
    fi
fi

if [[ "$FAILED" == "true" ]]; then
    print_header "Deploy Failed"
    print_error "One or more builds failed. Version not updated."
    exit 1
fi

# Update version in config and commit
if [[ "$BUMP_TYPE" != "none" && "$DO_PUSH" == "true" ]]; then
    print_step "Updating version..."
    VERSION=$NEW_VERSION
    save_config
    
    # Commit the version bump
    if git rev-parse --git-dir > /dev/null 2>&1; then
        git add "$CONFIG_FILE" > /dev/null 2>&1
        git commit -m "chore: bump version to $NEW_VERSION" > /dev/null 2>&1 || true
        print_success "Version bumped to $NEW_VERSION (committed)"
    else
        print_success "Version bumped to $NEW_VERSION"
    fi
fi

# Complete
print_header "Deploy Complete! 🚀"

echo ""
echo -e "  ${BOLD}Deployed Images${NC}"
echo -e "  ─────────────────────────────────────"
if [[ "$TARGET" == "backend" || "$TARGET" == "all" ]]; then
    echo -e "  ${CYAN}$REGISTRY/$BACKEND_IMAGE:$NEW_VERSION${NC}"
fi
if [[ "$TARGET" == "frontend" || "$TARGET" == "all" ]]; then
    echo -e "  ${CYAN}$REGISTRY/$FRONTEND_IMAGE:$NEW_VERSION${NC}"
fi
echo ""
