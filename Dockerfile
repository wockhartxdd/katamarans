# ---------- Этап 1: сборка приложения ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем только csproj и восстанавливаем зависимости (кэшируется слой)
COPY CatamaranRental.csproj ./
RUN dotnet restore

# Копируем остальной код и публикуем
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---------- Этап 2: минимальный образ для запуска ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Приложение слушает порт 8080 на всех интерфейсах
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CatamaranRental.dll"]
