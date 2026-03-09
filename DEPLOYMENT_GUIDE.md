# H??ng d?n Deploy HubCinema API lên Host

## ?? M?c l?c
1. [Deploy lên Docker Hub](#1-deploy-lên-docker-hub)
2. [Deploy lên Azure Container Registry (ACR)](#2-deploy-lên-azure-container-registry)
3. [Deploy lên Google Container Registry (GCR)](#3-deploy-lên-google-container-registry)
4. [Deploy lên Server/VPS riêng](#4-deploy-lên-servervps-riêng)
5. [Deploy lên Cloud Platforms](#5-deploy-lên-cloud-platforms)

---

## 1. Deploy lên Docker Hub

### B??c 1: ??ng ký tài kho?n Docker Hub
- Truy c?p: https://hub.docker.com/
- T?o tài kho?n mi?n phí

### B??c 2: ??ng nh?p Docker t? terminal
```bash
docker login
# Nh?p username và password Docker Hub
```

### B??c 3: Build image v?i tag phù h?p
```bash
# Format: docker build -t <dockerhub-username>/<image-name>:<tag> .
docker build -t nguyenxuanbac88/hubcinema-api:latest .
docker build -t nguyenxuanbac88/hubcinema-api:v1.0.0 .
```

### B??c 4: Push image lên Docker Hub
```bash
docker push nguyenxuanbac88/hubcinema-api:latest
docker push nguyenxuanbac88/hubcinema-api:v1.0.0
```

### B??c 5: Pull và ch?y trên server
```bash
# Trên server/VPS
docker pull nguyenxuanbac88/hubcinema-api:latest
docker run -d -p 5000:8080 \
  -e ConnectionStrings__DefaultConnection="Data Source=160.30.44.204,14334;Initial Catalog=cinema;User ID=sa;Password=StrongPass@123;Encrypt=True;Trust Server Certificate=True" \
  -e Redis__ConnectionString="redis-18049.crce264.ap-east-1-1.ec2.cloud.redislabs.com:18049,password=PjEAlhDFKaccsFIryc8YfQqOz2tGLaDX,abortConnect=False" \
  --name hubcinema-api \
  nguyenxuanbac88/hubcinema-api:latest
```

---

## 2. Deploy lên Azure Container Registry (ACR)

### B??c 1: T?o Azure Container Registry
```bash
# ??ng nh?p Azure
az login

# T?o resource group
az group create --name hubcinema-rg --location southeastasia

# T?o container registry
az acr create --resource-group hubcinema-rg \
  --name hubcinemaregistry --sku Basic
```

### B??c 2: ??ng nh?p ACR
```bash
az acr login --name hubcinemaregistry
```

### B??c 3: Tag và push image
```bash
# Tag image
docker tag hubcinema-api:latest hubcinemaregistry.azurecr.io/hubcinema-api:latest

# Push image
docker push hubcinemaregistry.azurecr.io/hubcinema-api:latest
```

### B??c 4: Deploy lên Azure Container Instances
```bash
az container create \
  --resource-group hubcinema-rg \
  --name hubcinema-api \
  --image hubcinemaregistry.azurecr.io/hubcinema-api:latest \
  --dns-name-label hubcinema-api \
  --ports 8080 \
  --environment-variables \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="<your-connection-string>" \
    Redis__ConnectionString="<your-redis-connection>"
```

---

## 3. Deploy lên Google Container Registry (GCR)

### B??c 1: Cài ??t Google Cloud SDK
```bash
# ??ng nh?p
gcloud auth login
gcloud config set project YOUR_PROJECT_ID
```

### B??c 2: Build và push
```bash
# Build v?i Cloud Build
gcloud builds submit --tag gcr.io/YOUR_PROJECT_ID/hubcinema-api

# Ho?c push local image
docker tag hubcinema-api:latest gcr.io/YOUR_PROJECT_ID/hubcinema-api:latest
docker push gcr.io/YOUR_PROJECT_ID/hubcinema-api:latest
```

### B??c 3: Deploy lên Cloud Run
```bash
gcloud run deploy hubcinema-api \
  --image gcr.io/YOUR_PROJECT_ID/hubcinema-api:latest \
  --platform managed \
  --region asia-southeast1 \
  --allow-unauthenticated \
  --set-env-vars "ASPNETCORE_ENVIRONMENT=Production"
```

---

## 4. Deploy lên Server/VPS riêng

### Option A: S? d?ng Docker tr?c ti?p

#### B??c 1: Cài ??t Docker trên server
```bash
# Ubuntu/Debian
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo systemctl enable docker
sudo systemctl start docker
```

#### B??c 2: Copy image lên server (n?u không dùng registry)
```bash
# Trên máy local: Save image ra file
docker save hubcinema-api:latest | gzip > hubcinema-api.tar.gz

# Upload lên server (dùng scp)
scp hubcinema-api.tar.gz user@your-server-ip:/home/user/

# Trên server: Load image
docker load < hubcinema-api.tar.gz
```

#### B??c 3: Ch?y container
```bash
docker run -d \
  --name hubcinema-api \
  --restart unless-stopped \
  -p 80:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Data Source=160.30.44.204,14334;Initial Catalog=cinema;User ID=sa;Password=StrongPass@123;Encrypt=True;Trust Server Certificate=True" \
  -e Redis__ConnectionString="redis-18049.crce264.ap-east-1-1.ec2.cloud.redislabs.com:18049,password=PjEAlhDFKaccsFIryc8YfQqOz2tGLaDX,abortConnect=False" \
  hubcinema-api:latest
```

### Option B: S? d?ng Docker Compose trên server

#### B??c 1: Upload files lên server
```bash
# Copy files docker-compose
scp docker-compose.yml user@your-server-ip:/home/user/hubcinema/
scp docker-compose.prod.yml user@your-server-ip:/home/user/hubcinema/
```

#### B??c 2: Build ho?c pull image
```bash
# SSH vào server
ssh user@your-server-ip

cd /home/user/hubcinema/

# Option 1: Build trên server (c?n copy source code)
docker-compose build

# Option 2: Pull t? Docker Hub
docker-compose pull
```

#### B??c 3: Ch?y services
```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Option C: S? d?ng Portainer (GUI)

#### B??c 1: Cài ??t Portainer
```bash
docker volume create portainer_data
docker run -d -p 9000:9000 -p 8000:8000 \
  --name portainer --restart always \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -v portainer_data:/data \
  portainer/portainer-ce:latest
```

#### B??c 2: Truy c?p Portainer
- M? browser: http://your-server-ip:9000
- T?o tài kho?n admin
- Ch?n "Local" environment

#### B??c 3: Deploy container qua GUI
- Vào Containers > Add Container
- Nh?p image name: `nguyenxuanbac88/hubcinema-api:latest`
- Map port: 80 -> 8080
- Add environment variables
- Deploy

---

## 5. Deploy lên Cloud Platforms

### A. AWS (Elastic Container Service - ECS)

#### B??c 1: Push image lên AWS ECR
```bash
# ??ng nh?p ECR
aws ecr get-login-password --region ap-southeast-1 | \
  docker login --username AWS --password-stdin YOUR_AWS_ACCOUNT_ID.dkr.ecr.ap-southeast-1.amazonaws.com

# Tag và push
docker tag hubcinema-api:latest YOUR_AWS_ACCOUNT_ID.dkr.ecr.ap-southeast-1.amazonaws.com/hubcinema-api:latest
docker push YOUR_AWS_ACCOUNT_ID.dkr.ecr.ap-southeast-1.amazonaws.com/hubcinema-api:latest
```

#### B??c 2: T?o ECS Task Definition và Service
- Vào AWS Console > ECS
- Create Cluster (Fargate)
- Create Task Definition
- Create Service

### B. DigitalOcean App Platform

```bash
# S? d?ng doctl CLI
doctl apps create --spec .do/app.yaml
```

### C. Heroku Container Registry

```bash
# ??ng nh?p Heroku
heroku login
heroku container:login

# Create app
heroku create hubcinema-api

# Push image
heroku container:push web --app hubcinema-api
heroku container:release web --app hubcinema-api

# Open app
heroku open --app hubcinema-api
```

---

## ?? B?o m?t khi Deploy

### 1. S? d?ng Docker Secrets (Swarm mode)
```bash
echo "your-connection-string" | docker secret create db_connection -
```

### 2. S? d?ng Environment Variables t? file
```bash
# T?o file .env
cat > .env << EOF
ConnectionStrings__DefaultConnection=Data Source=...
Redis__ConnectionString=redis-18049...
EOF

# Run v?i env file
docker run -d --env-file .env hubcinema-api:latest
```

### 3. Không commit sensitive data
- Thêm `.env` vào `.gitignore`
- S? d?ng Azure Key Vault, AWS Secrets Manager, etc.

---

## ?? Monitoring & Logs

### Xem logs container
```bash
# Real-time logs
docker logs -f hubcinema-api

# Last 100 lines
docker logs --tail 100 hubcinema-api
```

### Health check
```bash
# Ki?m tra container ?ang ch?y
docker ps

# Ki?m tra health endpoint
curl http://localhost:5000/health
curl http://localhost:5000/swagger
```

---

## ?? CI/CD Pipeline

### GitHub Actions Example
T?o file `.github/workflows/docker-deploy.yml`

```yaml
name: Build and Push Docker Image

on:
  push:
    branches: [ master ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Login to Docker Hub
      uses: docker/login-action@v2
      with:
        username: ${{ secrets.DOCKER_USERNAME }}
        password: ${{ secrets.DOCKER_PASSWORD }}
    
    - name: Build and push
      uses: docker/build-push-action@v4
      with:
        context: .
        push: true
        tags: nguyenxuanbac88/hubcinema-api:latest
```

---

## ? Checklist Deploy

- [ ] Build image thành công local
- [ ] Test container ch?y ???c local
- [ ] ??y image lên registry (Docker Hub/ACR/GCR)
- [ ] C?u hình environment variables
- [ ] C?u hình port mapping
- [ ] Setup SSL/HTTPS (n?u c?n)
- [ ] C?u hình domain name
- [ ] Setup monitoring và logging
- [ ] Backup database connection strings
- [ ] Test API endpoints sau khi deploy

---

## ?? Troubleshooting

### Container không start
```bash
docker logs hubcinema-api
docker inspect hubcinema-api
```

### Port ?ã ???c s? d?ng
```bash
# Linux/Mac
sudo lsof -i :8080
kill -9 <PID>

# Windows
netstat -ano | findstr :8080
taskkill /PID <PID> /F
```

### Connection timeout
- Ki?m tra firewall rules
- Ki?m tra security groups (AWS/Azure)
- Ki?m tra connection strings

---

## ?? Liên h? Support

- Email: cinemahub405@gmail.com
- GitHub: https://github.com/nguyenxuanbac88/HubCinema-API
