# Anidex

## Prerequisites:
- .NET 10 SDK: Install the latest .NET 10 SDK from dotnet.microsoft.com.
- SQL Server LocalDB: This is typically installed with Visual Studio. If you don't have it, install "SQL Server Express LocalDB".

## Steps to Run:
1. Open the project in Visual Studio 2022 or VS Code.
2. Restore Dependencies:
`dotnet restore`
3. Setup the Database:
The project uses EF Core migrations. To create the local database on your machine, run `dotnet ef database update`
3. (Note: If `dotnet ef` is not installed, run `dotnet tool install --global dotnet-ef` first).
4. Run the App:
`dotnet run` or `dotnet watch` (for automatic Hot Reload)
5. Access the App:
Open your browser to the URL provided in the terminal (usually `https://anidex.dev.localhost` or `https://localhost:XXXX`).

## Troubleshooting DB Connections:
If you encounter a connection error, check `Anidex/appsettings.json`. The DefaultConnection is currently configured for `(localdb)\mssqllocaldb`. If your local instance has a different name, update it there.