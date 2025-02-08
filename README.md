# ASP.NET Web API with MySQL, Docker, and Certbot SSL

This project sets up an **ASP.NET Web API** with **MySQL**, **Docker**, **Nginx reverse proxy**, and **Certbot SSL**. It includes database migrations, CORS configuration, and containerized deployment with HTTPS support.

---

## 1. Creating the ASP.NET Web API Project

Create a new **ASP.NET Web API** project using the .NET CLI:

```powershell
# Using the .NET CLI
dotnet new webapi -n MyWebAPI
cd MyWebAPI
```

---

## 2. Adding MySQL and Entity Framework Core Migration

Install the required **Entity Framework Core** packages:

```powershell
dotnet add package Pomelo.EntityFrameworkCore.MySql
dotnet add package Microsoft.EntityFrameworkCore.Design
```

Configure the **DbContext** inside `appsettings.json`:

```json
"ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=mydb;User=root;Password=mypassword;"
}
```

Register the `DbContext` in `Program.cs`:

```csharp
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));
```

Apply the migrations:

```powershell
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## 3. Configuring CORS

Enable **CORS** in `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});
```

Enable it in the application:

```csharp
app.UseCors("AllowAllOrigins");
```

---

## 4. Adding Docker Support

Create a `Dockerfile`:

```dockerfile
# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["MyWebAPI/MyWebAPI.csproj", "MyWebAPI/"]
RUN dotnet restore "./MyWebAPI/MyWebAPI.csproj"

COPY . .
WORKDIR "/src/MyWebAPI"
RUN dotnet build "./MyWebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "./MyWebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish ./
ENTRYPOINT ["dotnet", "MyWebAPI.dll"]
```

Build the Docker image:

```powershell
docker build -t webapi:0.1 .
```

---

## 5. Creating `docker-compose.yml`

```yaml
version: "3.8"

services:
  db:
    image: mysql:latest
    container_name: mysql_container
    restart: always
    environment:
      MYSQL_ROOT_PASSWORD: P@SS
      MYSQL_DATABASE: localdb
      MYSQL_USER: sa
      MYSQL_PASSWORD: P@SS
    ports:
      - "3307:3306"
    networks:
      - app_network
    volumes:
      - mysql_data:/var/lib/mysql

  webapi:
    build: .
    image: webapi:0.1
    container_name: webapi
    restart: always
    depends_on:
      db:
        condition: service_healthy
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=db;Port=3306;Database=localdb;User=sa;Password=P@SS;
    networks:
      - app_network

  nginx:
    image: nginx:latest
    container_name: nginx_container
    restart: unless-stopped
    depends_on:
      - webapi
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf
      - certbot_etc:/etc/letsencrypt # Mount Certbot SSL Certificates
      - certbot_www:/var/www/certbot # ACME Challenge folder
    networks:
      - app_network
    ports:
      - "80:80"
      - "443:443"

  certbot:
    image: certbot/certbot:latest
    container_name: certbot
    restart: unless-stopped
    volumes:
      - certbot_etc:/etc/letsencrypt
      - certbot_www:/var/www/certbot # ACME Challenge folder
    depends_on:
      - nginx
    entrypoint: >
      sh -c "certbot certonly --webroot -w /var/www/certbot -d yourdomain.com --email your@email.com --agree-tos --no-eff-email --force-renewal && certbot renew --dry-run"

networks:
  app_network:

volumes:
  mysql_data:
  certbot_etc:
  certbot_www:
```

---

## 12. Last Step Adjust Nginx To Get the 443 Request

```nginx
events {
    worker_connections 1024;
}

http {
    include       mime.types;
    default_type  application/octet-stream;
    sendfile        on;
    keepalive_timeout  65;
    gzip  on;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml application/xml+rss text/javascript;

    # Redirect HTTP to HTTPS
    server {
        listen 80;
        server_name yourdomain.com;

        location /.well-known/acme-challenge/ {
            root /var/www/certbot;
        }

        location / {
            proxy_pass http://webapi:8080/;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        return 301 https://$host$request_uri;
    }

    server {
        listen 443 ssl;
        server_name yourdomain.com;

        ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
        ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;
        include /etc/letsencrypt/options-ssl-nginx.conf;
        ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem;

        location / {
            proxy_pass http://webapi:8080/;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }
    }
}
```

Start the containers:

```powershell
docker-compose up -d
```

Access the API at:

```text
https://yourdomain.com/swagger
```
https://www.notion.so/Deploying-an-ASP-NET-Web-API-with-MySQL-Nginx-and-Docker-194661c42c9580eeb372f9599c7c0ea0?pvs=4

