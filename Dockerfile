# Стадия сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CatamaranRental.csproj", "./"]
RUN dotnet restore "CatamaranRental.csproj"
COPY . .
RUN dotnet publish "CatamaranRental.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Стадия запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CatamaranRental.dll"]