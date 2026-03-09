#!/bin/bash

# Quick Deploy Script for HubCinema API (Linux/Mac)
# Ch?y: chmod +x deploy.sh && ./deploy.sh

# ============================================
# C?U HÌNH - Thay ??i theo môi tr??ng c?a b?n
# ============================================

DOCKER_USERNAME="nguyenxuanbac88"
IMAGE_NAME="hubcinema-api"
IMAGE_TAG="latest"
FULL_IMAGE_NAME="${DOCKER_USERNAME}/${IMAGE_NAME}:${IMAGE_TAG}"

CONTAINER_NAME="hubcinema-api"
HOST_PORT="5000"
CONTAINER_PORT="8080"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# ============================================
# FUNCTIONS
# ============================================

show_menu() {
    clear
    echo -e "${GREEN}=========================================="
    echo "   HubCinema API - Deploy Script"
    echo -e "==========================================${NC}"
    echo ""
    echo "1. Build Docker Image"
    echo "2. Push to Docker Hub"
    echo "3. Build & Push (1 + 2)"
    echo "4. Run Container Locally"
    echo "5. Stop & Remove Container"
    echo "6. View Container Logs"
    echo "7. Deploy to Server (SSH)"
    echo "8. Clean up (Remove images & containers)"
    echo "9. Exit"
    echo ""
}

build_image() {
    echo -e "${YELLOW}Building Docker image...${NC}"
    docker build -t ${FULL_IMAGE_NAME} .
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}? Build successfully!${NC}"
    else
        echo -e "${RED}? Build failed!${NC}"
    fi
}

push_image() {
    echo -e "${YELLOW}Pushing image to Docker Hub...${NC}"
    
    # Login to Docker Hub
    echo "Logging in to Docker Hub..."
    docker login
    
    if [ $? -eq 0 ]; then
        docker push ${FULL_IMAGE_NAME}
        if [ $? -eq 0 ]; then
            echo -e "${GREEN}? Push successfully!${NC}"
            echo "Image available at: https://hub.docker.com/r/${DOCKER_USERNAME}/${IMAGE_NAME}"
        else
            echo -e "${RED}? Push failed!${NC}"
        fi
    fi
}

run_container() {
    echo -e "${YELLOW}Running container...${NC}"
    
    # Stop existing container if exists
    docker stop ${CONTAINER_NAME} 2>/dev/null
    docker rm ${CONTAINER_NAME} 2>/dev/null
    
    # Run new container
    docker run -d \
        --name ${CONTAINER_NAME} \
        --restart unless-stopped \
        -p ${HOST_PORT}:${CONTAINER_PORT} \
        -e ASPNETCORE_ENVIRONMENT=Production \
        -e ConnectionStrings__DefaultConnection="Data Source=160.30.44.204,14334;Initial Catalog=cinema;User ID=sa;Password=StrongPass@123;Encrypt=True;Trust Server Certificate=True" \
        -e Redis__ConnectionString="redis-18049.crce264.ap-east-1-1.ec2.cloud.redislabs.com:18049,password=PjEAlhDFKaccsFIryc8YfQqOz2tGLaDX,abortConnect=False" \
        ${FULL_IMAGE_NAME}
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}? Container started successfully!${NC}"
        echo "API: http://localhost:${HOST_PORT}"
        echo "Swagger: http://localhost:${HOST_PORT}/swagger"
    else
        echo -e "${RED}? Container failed to start!${NC}"
    fi
}

stop_container() {
    echo -e "${YELLOW}Stopping and removing container...${NC}"
    docker stop ${CONTAINER_NAME}
    docker rm ${CONTAINER_NAME}
    echo -e "${GREEN}? Container removed!${NC}"
}

show_logs() {
    echo -e "${YELLOW}Showing container logs (Ctrl+C to exit)...${NC}"
    docker logs -f ${CONTAINER_NAME}
}

deploy_to_server() {
    echo -e "${YELLOW}Deploy to Server via SSH...${NC}"
    
    read -p "Enter server IP address: " SERVER_IP
    read -p "Enter SSH username: " SERVER_USER
    
    echo "Connecting to server..."
    
    ssh ${SERVER_USER}@${SERVER_IP} << EOF
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
    -e ConnectionStrings__DefaultConnection='Data Source=160.30.44.204,14334;Initial Catalog=cinema;User ID=sa;Password=StrongPass@123;Encrypt=True;Trust Server Certificate=True' \
    -e Redis__ConnectionString='redis-18049.crce264.ap-east-1-1.ec2.cloud.redislabs.com:18049,password=PjEAlhDFKaccsFIryc8YfQqOz2tGLaDX,abortConnect=False' \
    ${FULL_IMAGE_NAME}

echo "Deploy completed!"
docker ps | grep ${CONTAINER_NAME}
EOF
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}? Deploy successfully!${NC}"
        echo "API: http://${SERVER_IP}"
    else
        echo -e "${RED}? Deploy failed!${NC}"
    fi
}

cleanup() {
    echo -e "${YELLOW}Cleaning up Docker resources...${NC}"
    
    # Stop and remove container
    docker stop ${CONTAINER_NAME} 2>/dev/null
    docker rm ${CONTAINER_NAME} 2>/dev/null
    
    # Remove images
    docker rmi ${FULL_IMAGE_NAME} 2>/dev/null
    
    # Prune unused resources
    docker system prune -f
    
    echo -e "${GREEN}? Cleanup completed!${NC}"
}

# ============================================
# MAIN LOOP
# ============================================

while true; do
    show_menu
    read -p "Select an option: " choice
    
    case $choice in
        1)
            build_image
            read -p "Press Enter to continue..."
            ;;
        2)
            push_image
            read -p "Press Enter to continue..."
            ;;
        3)
            build_image
            if [ $? -eq 0 ]; then
                push_image
            fi
            read -p "Press Enter to continue..."
            ;;
        4)
            run_container
            read -p "Press Enter to continue..."
            ;;
        5)
            stop_container
            read -p "Press Enter to continue..."
            ;;
        6)
            show_logs
            ;;
        7)
            deploy_to_server
            read -p "Press Enter to continue..."
            ;;
        8)
            cleanup
            read -p "Press Enter to continue..."
            ;;
        9)
            echo -e "${GREEN}Goodbye!${NC}"
            exit 0
            ;;
        *)
            echo -e "${RED}Invalid option!${NC}"
            sleep 2
            ;;
    esac
done
