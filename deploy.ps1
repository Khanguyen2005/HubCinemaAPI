# Quick Deploy Script for HubCinema API
# Ch?y script này ?? deploy nhanh lên server

# ============================================
# C?U HÌNH - Thay ??i theo môi tr??ng c?a b?n
# ============================================

# Docker Hub username
$DOCKER_USERNAME = "nguyenxuanbac88"
$IMAGE_NAME = "hubcinema-api"
$IMAGE_TAG = "latest"
$FULL_IMAGE_NAME = "${DOCKER_USERNAME}/${IMAGE_NAME}:${IMAGE_TAG}"

# Container settings
$CONTAINER_NAME = "hubcinema-api"
$HOST_PORT = "5000"
$CONTAINER_PORT = "8080"

# ============================================
# FUNCTIONS
# ============================================

function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

function Show-Menu {
    Clear-Host
    Write-ColorOutput Green "=========================================="
    Write-ColorOutput Green "   HubCinema API - Deploy Script"
    Write-ColorOutput Green "=========================================="
    Write-Host ""
    Write-Host "1. Build Docker Image"
    Write-Host "2. Push to Docker Hub"
    Write-Host "3. Build & Push (1 + 2)"
    Write-Host "4. Run Container Locally"
    Write-Host "5. Stop & Remove Container"
    Write-Host "6. View Container Logs"
    Write-Host "7. Deploy to Server (SSH)"
    Write-Host "8. Clean up (Remove images & containers)"
    Write-Host "9. Exit"
    Write-Host ""
}

function Build-DockerImage {
    Write-ColorOutput Yellow "Building Docker image..."
    docker build -t ${FULL_IMAGE_NAME} .
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput Green "? Build successfully!"
    } else {
        Write-ColorOutput Red "? Build failed!"
    }
}

function Push-DockerImage {
    Write-ColorOutput Yellow "Pushing image to Docker Hub..."
    
    # Login to Docker Hub
    Write-Host "Logging in to Docker Hub..."
    docker login
    
    if ($LASTEXITCODE -eq 0) {
        docker push ${FULL_IMAGE_NAME}
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput Green "? Push successfully!"
            Write-Host "Image available at: https://hub.docker.com/r/${DOCKER_USERNAME}/${IMAGE_NAME}"
        } else {
            Write-ColorOutput Red "? Push failed!"
        }
    }
}

function Run-Container {
    Write-ColorOutput Yellow "Running container..."
    
    # Stop existing container if exists
    docker stop ${CONTAINER_NAME} 2>$null
    docker rm ${CONTAINER_NAME} 2>$null
    
    # Run new container
    docker run -d `
        --name ${CONTAINER_NAME} `
        --restart unless-stopped `
        -p ${HOST_PORT}:${CONTAINER_PORT} `
        -e ASPNETCORE_ENVIRONMENT=Production `
        -e ConnectionStrings__DefaultConnection="$env:DB_CONNECTION" `
        -e Redis__ConnectionString="$env:REDIS_CONNECTION" `
        ${FULL_IMAGE_NAME}
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput Green "? Container started successfully!"
        Write-Host "API: http://localhost:${HOST_PORT}"
        Write-Host "Swagger: http://localhost:${HOST_PORT}/swagger"
    } else {
        Write-ColorOutput Red "? Container failed to start!"
    }
}

function Stop-Container {
    Write-ColorOutput Yellow "Stopping and removing container..."
    docker stop ${CONTAINER_NAME}
    docker rm ${CONTAINER_NAME}
    Write-ColorOutput Green "? Container removed!"
}

function Show-Logs {
    Write-ColorOutput Yellow "Showing container logs (Ctrl+C to exit)..."
    docker logs -f ${CONTAINER_NAME}
}

function Deploy-ToServer {
    Write-ColorOutput Yellow "Deploy to Server via SSH..."
    
    $SERVER_IP = Read-Host "Enter server IP address"
    $SERVER_USER = Read-Host "Enter SSH username"
    
    Write-Host "Connecting to server..."
    
    $deployScript = @"
# Pull latest image
docker pull ${FULL_IMAGE_NAME}

# Stop existing container
docker stop ${CONTAINER_NAME} 2>/dev/null || true
docker rm ${CONTAINER_NAME} 2>/dev/null || true

# Run new container
docker run -d \
    --name ${CONTAINER_NAME} \
    --restart unless-stopped \
    -p 80:8080 \
    -e ASPNETCORE_ENVIRONMENT=Production \
    -e ConnectionStrings__DefaultConnection='$env:DB_CONNECTION' \
    -e Redis__ConnectionString='$env:REDIS_CONNECTION' \
    ${FULL_IMAGE_NAME}

echo "Deploy completed!"
docker ps | grep ${CONTAINER_NAME}
"@
    
    $deployScript | ssh "${SERVER_USER}@${SERVER_IP}" "bash -s"
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput Green "? Deploy successfully!"
        Write-Host "API: http://${SERVER_IP}"
    } else {
        Write-ColorOutput Red "? Deploy failed!"
    }
}

function Clean-Up {
    Write-ColorOutput Yellow "Cleaning up Docker resources..."
    
    # Stop and remove container
    docker stop ${CONTAINER_NAME} 2>$null
    docker rm ${CONTAINER_NAME} 2>$null
    
    # Remove images
    docker rmi ${FULL_IMAGE_NAME} 2>$null
    
    # Prune unused resources
    docker system prune -f
    
    Write-ColorOutput Green "? Cleanup completed!"
}

# ============================================
# MAIN LOOP
# ============================================

do {
    Show-Menu
    $choice = Read-Host "Select an option"
    
    switch ($choice) {
        '1' {
            Build-DockerImage
            Read-Host "Press Enter to continue"
        }
        '2' {
            Push-DockerImage
            Read-Host "Press Enter to continue"
        }
        '3' {
            Build-DockerImage
            if ($LASTEXITCODE -eq 0) {
                Push-DockerImage
            }
            Read-Host "Press Enter to continue"
        }
        '4' {
            Run-Container
            Read-Host "Press Enter to continue"
        }
        '5' {
            Stop-Container
            Read-Host "Press Enter to continue"
        }
        '6' {
            Show-Logs
        }
        '7' {
            Deploy-ToServer
            Read-Host "Press Enter to continue"
        }
        '8' {
            Clean-Up
            Read-Host "Press Enter to continue"
        }
        '9' {
            Write-ColorOutput Green "Goodbye!"
            return
        }
        default {
            Write-ColorOutput Red "Invalid option!"
            Start-Sleep -Seconds 2
        }
    }
} while ($true)
