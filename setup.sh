#!/usr/bin/env bash
# One-time setup. Run from the repository root:  bash setup.sh
# Adds the NuGet packages (latest stable versions) and the EF Core CLI tool.
set -euo pipefail

dotnet --version | grep -q '^11\.' || { echo "Install the .NET 10 SDK first: https://dotnet.microsoft.com/download"; exit 1; }

dotnet add src/NeoWatch.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/NeoWatch.Infrastructure package Microsoft.Extensions.Http.Resilience
dotnet add src/NeoWatch.Infrastructure package Microsoft.Extensions.Options.ConfigurationExtensions

dotnet add src/NeoWatch.Worker package Microsoft.EntityFrameworkCore.Design
dotnet add src/NeoWatch.Worker package Serilog.Extensions.Hosting
dotnet add src/NeoWatch.Worker package Serilog.Sinks.Console

dotnet new tool-manifest --force
dotnet tool install dotnet-ef

dotnet build
echo
echo "Setup done. Next: dotnet test   (expect failures until you fill in the TODOs)"
