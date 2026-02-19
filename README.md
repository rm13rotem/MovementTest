# Movement.WebApp

This ASP.NET Core (.NET 8) application demonstrates a layered data access approach with:

- `MovementEntities` EF Core DbContext (SQL Server)
- `SqlServerDbDataSource` (database)
- `SelfDesignedCache` (in-memory LRU-like cache)
- `RedisDataSource` (StackExchange.Redis-backed cache)
- `DataServiceCoordinator` (coordinator that reads/writes DB, SDCS and Redis)
- MVC `MovementController` with full CRUD + index views that can query different sources

Quick start
-----------

Prerequisites
- .NET 8 SDK
- SQL Server (named instance if you use `SQLEXPRESS01`) or update `appsettings.json` connection string
- Redis running (recommended in WSL2 or Docker)

Install Redis in WSL2 (Ubuntu)
1. Open your WSL distro terminal (Ubuntu)
2. sudo apt update && sudo apt install redis-server -y
3. sudo service redis-server start
4. Test: redis-cli ping -> PONG

Configuration
- `Movement.WebApp/appsettings.json` contains:
  - `ConnectionStrings:DefaultConnection` — update to point at your SQL Server instance
  - `Redis:Configuration` — e.g. `localhost:6379` or `<wsl-ip>:6379` if needed
  - `Redis:InstanceName` — optional key namespace prefix

Required packages
Run from repository root:

```bash
dotnet add Movement.WebApp package Microsoft.EntityFrameworkCore.SqlServer
dotnet add Movement.WebApp package StackExchange.Redis
dotnet tool install --global dotnet-ef
```

Migrations & database
```bash
dotnet ef migrations add InitialCreate --project Movement.WebApp --startup-project Movement.WebApp
dotnet ef database update --project Movement.WebApp --startup-project Movement.WebApp
```

Run the app
```bash
dotnet run --project Movement.WebApp
# open https://localhost:PORT/Movement
```

Notes
- The Redis flush action in the app deletes only keys matching the configured prefix to avoid calling FLUSH on the whole instance.
- If your app can't reach Redis at `localhost:6379`, try the WSL IP or run Redis in Docker and expose the port.
- For production, secure Redis with auth/TLS and avoid using server key scans; use an index set for efficient deletion.

Troubleshooting
- DI lifetime errors: `IDataSource` must be scoped because it depends on a scoped DbContext.
- If EF provider is missing, install the `Microsoft.EntityFrameworkCore.SqlServer` package.

If you want I can add a small admin page for Redis key management or create the EF migration files here for you.