# StockTrader Application Setup Instructions

This document provides instructions on how to set up and run the StockTrader C# application suite.

## 1. Prerequisites

*   **.NET 8 SDK**: Ensure you have the .NET 8 SDK installed. You can download it from [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0).
*   **(Optional) Git**: For cloning the repository.
*   **(Optional) SQL Server or PostgreSQL**: If you plan to use a persistent database instead of the default In-Memory database.
    *   SQL Server Express: [https://www.microsoft.com/en-us/sql-server/sql-server-downloads](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
    *   PostgreSQL: [https://www.postgresql.org/download/](https://www.postgresql.org/download/)

## 2. Getting the Code

Clone the repository (if applicable) or download the source code.

```bash
# git clone <repository_url>
# cd <repository_directory>
```

## 3. Configuration

The application consists of two main executable projects: `StockTrader.Api` (the Web API) and `StockTrader.Worker` (the background data processing service). Both use an In-Memory database by default for ease of setup.

### 3.1. Database Configuration (Optional - For Persistent Storage)

If you want to use PostgreSQL or SQL Server:

1.  **Ensure the Database is Running**: Make sure your PostgreSQL or SQL Server instance is running and accessible.
2.  **Create a Database**: Create an empty database (e.g., `StockTraderDb`) in your chosen database server.
3.  **Connection Strings**:
    *   Locate the `appsettings.json` file in the `StockTrader.Api` project (`StockTrader.Api/appsettings.json`).
    *   Locate the `appsettings.json` file in the `StockTrader.Worker` project (`StockTrader.Worker/appsettings.json`).
    *   In **both** files, update the `ConnectionStrings.StockTraderDb` placeholder with your actual connection string.

    **Example for PostgreSQL in `appsettings.json`:**
    ```json
    "ConnectionStrings": {
      "StockTraderDb": "Host=localhost;Port=5432;Database=StockTraderDb;Username=your_username;Password=your_password;"
    }
    ```

    **Example for SQL Server in `appsettings.json`:**
    ```json
    "ConnectionStrings": {
      "StockTraderDb": "Server=your_server_name;Database=StockTraderDb;User ID=your_user_id;Password=your_password;Trusted_Connection=False;Encrypt=True;TrustServerCertificate=True;"
    }
    ```
4.  **Enable Database Provider in Code**:
    *   In `StockTrader.Api/Program.cs` and `StockTrader.Worker/Program.cs`:
        *   Comment out the line: `Console.WriteLine("Using In-Memory database..."); services.AddDbContext<StockTraderDbContext>(options => options.UseInMemoryDatabase(...));`
        *   Uncomment the appropriate `services.AddDbContext` section for either PostgreSQL or SQL Server based on your choice and connection string. Make sure the corresponding NuGet package (`Npgsql.EntityFrameworkCore.PostgreSQL` or `Microsoft.EntityFrameworkCore.SqlServer`) is referenced in the `StockTrader.Data` project (they were included during setup).

### 3.2. Worker Service Configuration

The `StockTrader.Worker` project has settings in its `StockTrader.Worker/appsettings.json` file under the `"WorkerSettings"` section:

*   `FetchLiveMarketDataIntervalSeconds`: How often to fetch live market data.
*   `FetchSentimentDataIntervalSeconds`: How often to fetch sentiment data.
*   `GenerateTipsIntervalSeconds`: How often to run the tip generation engine.
*   `StockSymbolsToMonitor`: A list of stock symbols the worker should actively monitor and process.

Adjust these values as needed.

```json
  "WorkerSettings": {
    "FetchLiveMarketDataIntervalSeconds": 60,
    "FetchSentimentDataIntervalSeconds": 300,
    "GenerateTipsIntervalSeconds": 600,
    "StockSymbolsToMonitor": ["RELIANCE", "TCS", "HDFCBANK", "INFY", "ICICIBANK"]
  }
```

## 4. Building the Solution

Open a terminal or command prompt in the root directory of the solution (where `StockTrader.sln` is located) and run:

```bash
dotnet build
```

This command will restore NuGet packages and build all projects in the solution.

## 5. Running the Application

You need to run both the API and the Worker for the full application functionality.

### 5.1. Running the API (`StockTrader.Api`)

Navigate to the API project directory and run:

```bash
cd StockTrader.Api
dotnet run
```
The API will typically be available at `https://localhost:7xxx` or `http://localhost:5xxx`. The exact URLs will be shown in the console output.
You can access Swagger UI for API documentation and testing at `/swagger` (e.g., `https://localhost:7xxx/swagger`).

**Initial Data Seeding**: The API project is configured to seed a few sample stocks into the database if it's empty when the application starts.

### 5.2. Running the Worker (`StockTrader.Worker`)

Open a **new** terminal or command prompt, navigate to the Worker project directory, and run:

```bash
cd StockTrader.Worker
dotnet run
```
The worker will start its background tasks, periodically fetching data and generating tips based on its configuration. Its activity will be logged to the console.

**Initial Data Seeding**: The Worker project is also configured to seed a few sample stocks into the database if it's empty and if the API hasn't already done so (if they share the same persistent database).

## 6. Development Mode

*   **Running from an IDE**: You can open the `StockTrader.sln` file in an IDE like Visual Studio or JetBrains Rider.
    *   Set `StockTrader.Api` and `StockTrader.Worker` as startup projects (if your IDE supports multiple startup projects).
    *   Run them in Debug mode.
*   **Hot Reload**: .NET Hot Reload is enabled by default, allowing you to make code changes that are applied without restarting the application during development.

## 7. Deployment (Conceptual)

Deploying this application would typically involve:

*   **API Project (`StockTrader.Api`)**:
    *   Publishing the project: `dotnet publish -c Release`
    *   Deploying the published output to a hosting environment (e.g., IIS, Azure App Service, Docker container).
*   **Worker Project (`StockTrader.Worker`)**:
    *   Publishing the project: `dotnet publish -c Release`
    *   Deploying the published output to an environment where it can run as a background service (e.g., Windows Service, Linux daemon, Azure WebJob, Kubernetes CronJob/Deployment, Docker container).
*   **Database**: Ensure the chosen database (PostgreSQL/SQL Server) is deployed and accessible by both the API and Worker services with the configured connection strings.
*   **Configuration Management**: Use environment variables or configuration services (like Azure App Configuration) for sensitive settings like connection strings and API keys in a production environment.

This setup guide provides the basics to get the application running locally.
```
