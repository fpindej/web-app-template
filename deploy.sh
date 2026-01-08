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
    echo "$answer" | tr '[:upper:]' '[:lower:]'
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
    detected_name=${detected_name:-"MyProject"}
    # Convert PascalCase to kebab-case
    local detected_slug=$(echo "$detected_name" | sed 's/\([a-z]\)\([A-Z]\)/\1-\2/g' | tr '[:upper:]' '[:lower:]')
    
    cat > "$CONFIG_FILE" << EOF
{
  "registry": "myusername",
  "backendImage": "${detected_slug}-api",
  "frontendImage": "${detected_slug}-frontend",
  "backendVersion": "0.1.0",
  "frontendVersion": "0.1.0",
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
    BACKEND_VERSION=$(grep -o '"backendVersion"[[:space:]]*:[[:space:]]*"[^"]*"' "$CONFIG_FILE" | sed 's/.*: *"\([^"]*\)"/\1/')
    FRONTEND_VERSION=$(grep -o '"frontendVersion"[[:space:]]*:[[:space:]]*"[^"]*"' "$CONFIG_FILE" | sed 's/.*: *"\([^"]*\)"/\1/')
    PLATFORM=$(grep -o '"platform"[[:space:]]*:[[:space:]]*"[^"]*"' "$CONFIG_FILE" | sed 's/.*: *"\([^"]*\)"/\1/')
}

save_config() {
    cat > "$CONFIG_FILE" << EOF
{
  "registry": "$REGISTRY",
  "backendImage": "$BACKEND_IMAGE",
  "frontendImage": "$FRONTEND_IMAGE",
  "backendVersion": "$BACKEND_VERSION",
  "frontendVersion": "$FRONTEND_VERSION",
  "platform": "$PLATFORM"
}
EOF
}

configure_registry() {
    print_header "Deploy Configuration"
    
    echo ""
    print_info "Current configuration:"
    echo -e "  Registry:         ${CYAN}$REGISTRY${NC}"
    echo -e "  Backend Image:    ${CYAN}$BACKEND_IMAGE${NC}"
    echo -e "  Backend Version:  ${CYAN}$BACKEND_VERSION${NC}"
    echo -e "  Frontend Image:   ${CYAN}$FRONTEND_IMAGE${NC}"
    echo -e "  Frontend Version: ${CYAN}$FRONTEND_VERSION${NC}"
    echo -e "  Platform:         ${CYAN}$PLATFORM${NC}"
    echo ""
    
    local reconfigure=$(prompt_yn "Reconfigure settings?" "n")
    
    if [[ "$reconfigure" == "y" ]]; then
        echo ""
        REGISTRY=$(prompt_value "Docker registry (e.g., myusername, ghcr.io/myuser)" "$REGISTRY")
        BACKEND_IMAGE=$(prompt_value "Backend image name" "$BACKEND_IMAGE")
        FRONTEND_IMAGE=$(prompt_value "Frontend image name" "$FRONTEND_IMAGE")
        PLATFORM=$(prompt_platform "$PLATFORM")
        save_config
        print_success "Configuration saved"
    fi
}

prompt_platform() {
    local current=$1
    echo "" >&2
    echo -e "${BOLD}Target platform:${NC}" >&2
    echo "" >&2
    echo "  [1] linux/amd64    - Intel/AMD servers, most cloud VMs, WSL2" >&2
    echo "  [2] linux/arm64    - Apple Silicon (M1/M2/M3), AWS Graviton" >&2
    echo "  [3] linux/arm64/v8 - Raspberry Pi 4/5 (64-bit)" >&2
    echo "  [4] linux/arm/v7   - Raspberry Pi 2/3, older ARM devices (32-bit)" >&2
    echo "  [5] Custom         - Enter manually" >&2
    echo "" >&2
    echo -e "  ${DIM}Current: $current${NC}" >&2
    echo "" >&2
    read -p "$(echo -e "${BOLD}Choose [1-5]${NC} (default: keep current): ")" choice
    
    case ${choice} in
        1) echo "linux/amd64" ;;
        2) echo "linux/arm64" ;;
        3) echo "linux/arm64/v8" ;;
        4) echo "linux/arm/v7" ;;
        5) 
            read -p "$(echo -e "${BOLD}Enter platform${NC}: ")" custom_platform
            echo "${custom_platform:-$current}"
            ;;
        *) echo "$current" ;;
    esac
}

prompt_bump_type() {
    echo "" >&2
    echo -e "${BOLD}Version bump type:${NC}" >&2
    echo "" >&2
    echo "  [1] Patch  (0.1.0 → 0.1.1) - bug fixes" >&2
    echo "  [2] Minor  (0.1.0 → 0.2.0) - new features" >&2
    echo "  [3] Major  (0.1.0 → 1.0.0) - breaking changes" >&2
    echo "  [4] None   (rebuild current version)" >&2
    echo "" >&2
    read -p "$(echo -e "${BOLD}Choose [1-4]${NC} (default: 1): ")" choice
    
    case ${choice:-1} in
        1) echo "patch" ;;
        2) echo "minor" ;;
        3) echo "major" ;;
        4) echo "none" ;;
        *) echo "patch" ;;
    esac
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

check_docker_login() {
    local registry=$1
    
    # Docker config file location
    local config_path="$HOME/.docker/config.json"
    
    if [ ! -f "$config_path" ]; then
        return 1
    fi
    
    # Check for Docker Hub auth (simple username = Docker Hub)
    if [[ "$registry" != *"/"* ]]; then
        # Docker Hub stores auth as "https://index.docker.io/v1/"
        if grep -q "index.docker.io" "$config_path" 2>/dev/null; then
            return 0
        fi
    else
        # Custom registry (e.g., ghcr.io/user)
        local registry_host="${registry%%/*}"
        if grep -q "$registry_host" "$config_path" 2>/dev/null; then
            return 0
        fi
    fi
    
    return 1
}

request_docker_login() {
    local registry=$1
    
    echo ""
    print_warning "Not logged in to Docker registry"
    echo ""
    
    # Determine which registry to login to
    if [[ "$registry" != *"/"* ]]; then
        # Simple username = Docker Hub
        echo -e "  You need to login to Docker Hub to push images."
        echo ""
        echo -e "  ${DIM}Run this command:${NC}"
        echo -e "    ${CYAN}docker login${NC}"
    elif [[ "$registry" == ghcr.io/* ]]; then
        # GitHub Container Registry
        echo -e "  You need to login to GitHub Container Registry."
        echo ""
        echo -e "  ${DIM}1. Create a Personal Access Token at:${NC}"
        echo -e "     ${CYAN}https://github.com/settings/tokens${NC}"
        echo -e "     ${DIM}(with 'write:packages' scope)${NC}"
        echo ""
        echo -e "  ${DIM}2. Run this command:${NC}"
        echo -e "     ${CYAN}docker login ghcr.io -u YOUR_GITHUB_USERNAME${NC}"
    else
        # Other registry
        local registry_host="${registry%%/*}"
        echo -e "  You need to login to: ${BOLD}$registry_host${NC}"
        echo ""
        echo -e "  ${DIM}Run this command:${NC}"
        echo -e "    ${CYAN}docker login $registry_host${NC}"
    fi
    
    echo ""
    read -p "Press Enter after logging in to continue (or 'q' to quit): " retry
    
    if [[ "$retry" == "q" ]]; then
        return 1
    fi
    
    check_docker_login "$registry"
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
    
    pushd src/backend > /dev/null
    local build_result=0
    docker buildx build $build_args -f "$(basename "$webapi_dir")/Dockerfile" . 2>&1 || build_result=$?
    popd > /dev/null
    
    if [ $build_result -ne 0 ]; then
        print_error "Backend build failed"
        return 1
    fi
    
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
    
    pushd src/frontend > /dev/null
    local build_result=0
    docker buildx build $build_args . 2>&1 || build_result=$?
    popd > /dev/null
    
    if [ $build_result -ne 0 ]; then
        print_error "Frontend build failed"
        return 1
    fi
    
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
BUMP_TYPE=""
DO_PUSH="true"
TAG_LATEST="true"
YES_TO_ALL="false"
INTERACTIVE_MODE="false"

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

# Check Docker login (only if pushing)
if [[ "$DO_PUSH" == "true" ]]; then
    if ! check_docker_login "$REGISTRY"; then
        if ! request_docker_login "$REGISTRY"; then
            print_error "Docker login required to push images"
            exit 1
        fi
        print_success "Docker login verified"
    fi
fi

# Interactive target selection if not specified
if [ -z "$TARGET" ]; then
    INTERACTIVE_MODE="true"
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

# Interactive version bump selection if not specified via CLI
if [ -z "$BUMP_TYPE" ]; then
    if [[ "$INTERACTIVE_MODE" == "true" ]]; then
        BUMP_TYPE=$(prompt_bump_type)
    else
        BUMP_TYPE="patch"
    fi
fi

# Calculate new versions based on target
NEW_BACKEND_VERSION=$BACKEND_VERSION
NEW_FRONTEND_VERSION=$FRONTEND_VERSION

if [[ "$BUMP_TYPE" != "none" ]]; then
    if [[ "$TARGET" == "backend" || "$TARGET" == "all" ]]; then
        NEW_BACKEND_VERSION=$(bump_version "$BACKEND_VERSION" "$BUMP_TYPE")
    fi
    if [[ "$TARGET" == "frontend" || "$TARGET" == "all" ]]; then
        NEW_FRONTEND_VERSION=$(bump_version "$FRONTEND_VERSION" "$BUMP_TYPE")
    fi
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
echo -e "  ${BOLD}Versions${NC}"
echo -e "  ─────────────────────────────────────"
if [[ "$TARGET" == "backend" || "$TARGET" == "all" ]]; then
    echo -e "  Backend:  ${DIM}$BACKEND_VERSION${NC} → ${GREEN}$NEW_BACKEND_VERSION${NC}"
fi
if [[ "$TARGET" == "frontend" || "$TARGET" == "all" ]]; then
    echo -e "  Frontend: ${DIM}$FRONTEND_VERSION${NC} → ${GREEN}$NEW_FRONTEND_VERSION${NC}"
fi
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
    if ! build_backend "$NEW_BACKEND_VERSION" "$DO_PUSH" "$TAG_LATEST"; then
        FAILED="true"
    fi
fi

if [[ "$TARGET" == "frontend" || "$TARGET" == "all" ]]; then
    if ! build_frontend "$NEW_FRONTEND_VERSION" "$DO_PUSH" "$TAG_LATEST"; then
        FAILED="true"
    fi
fi

if [[ "$FAILED" == "true" ]]; then
    print_header "Deploy Failed"
    print_error "One or more builds failed. Version not updated."
    exit 1
fi

# Update versions in config and commit
if [[ "$BUMP_TYPE" != "none" && "$DO_PUSH" == "true" ]]; then
    print_step "Updating version..."
    
    # Update only the versions that were deployed
    if [[ "$TARGET" == "backend" || "$TARGET" == "all" ]]; then
        BACKEND_VERSION=$NEW_BACKEND_VERSION
    fi
    if [[ "$TARGET" == "frontend" || "$TARGET" == "all" ]]; then
        FRONTEND_VERSION=$NEW_FRONTEND_VERSION
    fi
    save_config
    
    # Build commit message
    local commit_msg="chore: bump"
    if [[ "$TARGET" == "backend" ]]; then
        commit_msg="$commit_msg backend to $NEW_BACKEND_VERSION"
    elif [[ "$TARGET" == "frontend" ]]; then
        commit_msg="$commit_msg frontend to $NEW_FRONTEND_VERSION"
    else
        commit_msg="$commit_msg backend to $NEW_BACKEND_VERSION, frontend to $NEW_FRONTEND_VERSION"
    fi
    
    # Commit the version bump
    if git rev-parse --git-dir > /dev/null 2>&1; then
        git add "$CONFIG_FILE" > /dev/null 2>&1
        git commit -m "$commit_msg" > /dev/null 2>&1 || true
        print_success "Version updated (committed)"
    else
        print_success "Version updated"
    fi
fi

# Complete
print_header "Deploy Complete! 🚀"

echo ""
echo -e "  ${BOLD}Deployed Images${NC}"
echo -e "  ─────────────────────────────────────"
if [[ "$TARGET" == "backend" || "$TARGET" == "all" ]]; then
    echo -e "  ${CYAN}$REGISTRY/$BACKEND_IMAGE:$NEW_BACKEND_VERSION${NC}"
fi
if [[ "$TARGET" == "frontend" || "$TARGET" == "all" ]]; then
    echo -e "  ${CYAN}$REGISTRY/$FRONTEND_IMAGE:$NEW_FRONTEND_VERSION${NC}"
fi
echo ""
