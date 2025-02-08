# ASP.NET Web API with MySQL and Docker

This project sets up an **ASP.NET Web API** with **MySQL**, **Docker**, and **Nginx reverse proxy**. It includes database migrations, CORS configuration, and containerized deployment.

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

Apply automatic migration on project startup:

```csharp
var app = builder.Build();

WaitForDatabase(connectionString, maxRetries: 30, delayMilliseconds: 5000);

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (dbContext.Database.GetPendingMigrations().Any())
    {
        dbContext.Database.Migrate();
    }
}

private static void WaitForDatabase(string connectionString, int maxRetries, int delayMilliseconds)
{
    int retryCount = 0;
    while (retryCount < maxRetries)
    {
        try
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            Console.WriteLine("Successfully connected to MySQL.");
            return;
        }
        catch (Exception ex)
        {
            retryCount++;
            Console.WriteLine($"Waiting for MySQL... Attempt {retryCount}/{maxRetries}: {ex.Message}");
            Thread.Sleep(delayMilliseconds);
        }
    }
    throw new Exception("Unable to connect to MySQL after multiple attempts.");
}
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

Install **Docker support**:

```powershell
dotnet add package Microsoft.NET.Build.Containers
```

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

COPY ["DockerSSLWebAPI/DockerSSLWebAPI.csproj", "DockerSSLWebAPI/"]
RUN dotnet restore "./DockerSSLWebAPI/DockerSSLWebAPI.csproj"

COPY . .
WORKDIR "/src/DockerSSLWebAPI"
RUN dotnet build "./DockerSSLWebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "./DockerSSLWebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish ./
ENTRYPOINT ["dotnet", "DockerSSLWebAPI.dll"]
```

Build the Docker image:

```powershell
docker build -t webapi:0.1 .
```

---

## 5. Creating `docker-compose.yml`

Add a **Docker Compose** file to run **MySQL, Web API, and Nginx**:

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
    healthcheck:
      test: ["CMD-SHELL", "mysqladmin ping -h localhost -usa -pP@SS || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 15
      start_period: 70s
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
    ports:
      - "80:80"
      - "443:443"
    depends_on:
      - webapi
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf
    networks:
      - app_network

networks:
  app_network:

volumes:
  mysql_data:
```

---

## 6. Opening Ports for External Access

Allow ports **80 (HTTP) and 443 (HTTPS)** on the firewall:

```powershell
New-NetFirewallRule -Name "Allow_HTTP_80" -DisplayName "Allow HTTP (80)" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 80
New-NetFirewallRule -Name "Allow_HTTPS_443" -DisplayName "Allow HTTPS (443)" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 443
Get-NetFirewallRule -Name "Allow_HTTP_80", "Allow_HTTPS_443"
```

---

## 7. Running the Setup

Start the containers:

```powershell
docker-compose up -d
```

Access the API at:

```text
http://myip:80/swagger
```

This setup allows a fully containerized **ASP.NET Web API** with **MySQL and Nginx reverse proxy** accessible externally.
https://www.notion.so/Deploying-an-ASP-NET-Web-API-with-MySQL-Nginx-and-Docker-194661c42c9580eeb372f9599c7c0ea0?pvs=4
