FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env

# Update package lists and install procps
RUN apt-get update && apt-get install -y procps

WORKDIR /app

ENV DAY_TRADER_BOT_ROOT_DIR=/app

# Copy the solution file and restore dependencies
COPY *.sln .
COPY DayTradeBot/DayTradeBot.csproj DayTradeBot/
COPY TestDayTradeBot/TestDayTradeBot.csproj TestDayTradeBot/
RUN dotnet restore

# Copy the rest of the files and build
COPY . .
RUN dotnet build -c Release

# Set the entry point for the console application
ENTRYPOINT ["dotnet", "run", "--project", "DayTradeBot/DayTradeBot.csproj"]
