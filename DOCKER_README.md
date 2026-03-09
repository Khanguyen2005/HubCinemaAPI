# HubCinema API - Docker Setup

## H??ng d?n ch?y v?i Docker

### Yêu c?u
- Docker Desktop ?ã cài ??t
- Docker Compose

### Các l?nh c? b?n

#### 1. Build và ch?y toàn b? ?ng d?ng (Development)
```bash
docker-compose up --build
```

#### 2. Ch?y ? ch? ?? n?n (background)
```bash
docker-compose up -d
```

#### 3. D?ng các container
```bash
docker-compose down
```

#### 4. Xem logs
```bash
docker-compose logs -f hubcinema-api
```

#### 5. Build l?i image
```bash
docker-compose build
```

#### 6. Ch?y môi tr??ng Production
```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Các Service

| Service | Port | Mô t? |
|---------|------|-------|
| hubcinema-api | 5000 | ASP.NET Core API |
| sqlserver | 1433 | SQL Server Database |
| redis | 6379 | Redis Cache |

### Truy c?p ?ng d?ng

- API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger
- SQL Server: localhost,1433 (sa/StrongPass@123)
- Redis: localhost:6379

### Build ch? Docker image (không dùng docker-compose)

#### Build image
```bash
docker build -t hubcinema-api:latest .
```

#### Ch?y container
```bash
docker run -d -p 5000:8080 --name hubcinema-api hubcinema-api:latest
```

### C?u hình môi tr??ng

#### Development
- S? d?ng SQL Server và Redis local trong container
- Environment: Development
- Swagger enabled

#### Production
- K?t n?i ??n SQL Server và Redis bên ngoài (theo appsettings.json)
- Environment: Production
- HTTPS recommended

### Volumes

- `sqlserver-data`: L?u tr? d? li?u SQL Server
- `redis-data`: L?u tr? d? li?u Redis

### Troubleshooting

#### Xóa volumes và rebuild
```bash
docker-compose down -v
docker-compose up --build
```

#### Ki?m tra logs l?i
```bash
docker-compose logs hubcinema-api
```

#### Vào bên trong container
```bash
docker exec -it hubcinema-api bash
```

### Notes

- M?t kh?u và connection strings trong file docker-compose.yml ch? dùng cho development
- Trong production, nên s? d?ng Docker secrets ho?c environment variables t? CI/CD
- File `docker-compose.prod.yml` s? d?ng database và Redis bên ngoài (theo appsettings.json g?c)
