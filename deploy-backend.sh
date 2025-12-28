#!/bin/bash
set -e

# Configuration
# --------------------------------------------------
# Docker Hub Username or Registry URL (e.g., "myusername" or "ghcr.io/myusername")
DOCKER_REGISTRY="myusername" 
# Image Name
IMAGE_NAME="myproject-api"
PLATFORM="linux/amd64"
# --------------------------------------------------

FULL_IMAGE="$DOCKER_REGISTRY/$IMAGE_NAME"

# Ensure we are in the script directory (root of repo)
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

echo "🚀 Starting build and push process for Backend API"
echo "--------------------------------------------------"
echo "📦 Target Image: $FULL_IMAGE"
echo "--------------------------------------------------"

# Check if Docker is running
if ! docker system info > /dev/null 2>&1; then
    echo "❌ Error: Docker is not running."
    exit 1
fi

# Warn if using default registry
if [ "$DOCKER_REGISTRY" = "myusername" ]; then
    echo "⚠️  Warning: DOCKER_REGISTRY is set to default 'myusername'."
    echo "   Please edit this script to set your Docker Hub username."
    echo "   Continuing in 5 seconds..."
    sleep 5
fi

if [ ! -d "src/backend" ]; then
    echo "❌ Error: src/backend directory not found in $PWD"
    exit 1
fi

cd src/backend

# Check if Dockerfile exists
if [ ! -f "MyProject.WebApi/Dockerfile" ]; then
    echo "❌ Error: MyProject.WebApi/Dockerfile not found in $PWD"
    exit 1
fi

# Use buildx for multi-arch support
if ! docker buildx inspect default > /dev/null 2>&1; then
     docker buildx create --use
fi

echo "🔨 Building and Pushing..."
# Build context is src/backend, Dockerfile is inside MyProject.WebApi
if ! docker buildx build --platform "$PLATFORM" -t "$FULL_IMAGE:latest" -f MyProject.WebApi/Dockerfile --push .; then
    echo "--------------------------------------------------"
    echo "❌ Error: Build or Push failed."
    echo "   Possible reasons:"
    echo "   1. You are not logged in. Run 'docker login'."
    echo "   2. You don't have permission to push to '$DOCKER_REGISTRY'."
    echo "   3. Docker daemon is not reachable."
    exit 1
fi

echo "✅ API built and pushed successfully to $FULL_IMAGE:latest"
echo "--------------------------------------------------"
