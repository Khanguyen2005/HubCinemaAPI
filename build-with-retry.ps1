# Build Docker Image with Retry Logic
# Script t? ??ng retry khi build th?t b?i

param(
    [string]$ImageName = "nguyenxuanbac88/hubcinema-api",
    [string]$Tag = "latest",
    [int]$MaxRetries = 3
)

$FullImageName = "${ImageName}:${Tag}"

function Write-ColorOutput($ForegroundColor, $Message) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    Write-Output $Message
    $host.UI.RawUI.ForegroundColor = $fc
}

Write-ColorOutput Green "========================================"
Write-ColorOutput Green "  Docker Build with Retry"
Write-ColorOutput Green "========================================"
Write-Host ""

# Step 1: Pull base images first
Write-ColorOutput Yellow "Step 1: Pulling base images..."
Write-Host "Pulling SDK image..."
docker pull mcr.microsoft.com/dotnet/sdk:8.0

if ($LASTEXITCODE -ne 0) {
    Write-ColorOutput Red "Failed to pull SDK image. Checking network..."
    Test-NetConnection mcr.microsoft.com -Port 443
    Write-ColorOutput Red "Please check your internet connection and try again."
    exit 1
}

Write-Host "Pulling ASP.NET Runtime image..."
docker pull mcr.microsoft.com/dotnet/aspnet:8.0

if ($LASTEXITCODE -ne 0) {
    Write-ColorOutput Red "Failed to pull ASP.NET image."
    exit 1
}

Write-ColorOutput Green "? Base images pulled successfully!"
Write-Host ""

# Step 2: Clean old build cache
Write-ColorOutput Yellow "Step 2: Cleaning build cache..."
docker builder prune -f
Write-ColorOutput Green "? Cache cleaned!"
Write-Host ""

# Step 3: Build with retry
$attempt = 1
$buildSuccess = $false

while ($attempt -le $MaxRetries -and -not $buildSuccess) {
    Write-ColorOutput Yellow "Step 3: Building image (Attempt $attempt/$MaxRetries)..."
    Write-Host "Building: $FullImageName"
    Write-Host ""
    
    # Build with progress
    docker build -t $FullImageName .
    
    if ($LASTEXITCODE -eq 0) {
        $buildSuccess = $true
        Write-ColorOutput Green "? Build successful!"
    } else {
        Write-ColorOutput Red "? Build failed on attempt $attempt"
        
        if ($attempt -lt $MaxRetries) {
            Write-ColorOutput Yellow "Retrying in 5 seconds..."
            Start-Sleep -Seconds 5
            $attempt++
        } else {
            Write-ColorOutput Red "Build failed after $MaxRetries attempts."
            Write-Host ""
            Write-Host "Troubleshooting tips:"
            Write-Host "1. Check Docker Desktop is running"
            Write-Host "2. Check internet connection"
            Write-Host "3. Restart Docker Desktop"
            Write-Host "4. Try changing DNS to 8.8.8.8 or 1.1.1.1"
            exit 1
        }
    }
}

Write-Host ""
Write-ColorOutput Green "========================================"
Write-ColorOutput Green "  Build Completed Successfully!"
Write-ColorOutput Green "========================================"
Write-Host ""
Write-Host "Image: $FullImageName"
Write-Host ""
Write-Host "Next steps:"
Write-Host "1. Test locally:  docker run -d -p 5000:8080 --name test-api $FullImageName"
Write-Host "2. Push to hub:   docker push $FullImageName"
Write-Host ""

# Optional: Show image details
Write-Host "Image details:"
docker images $ImageName --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}"
