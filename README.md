# Аренда катамаранов
Описание: сервис бронирования катамаранов с выбором даты, времени и типа судна

## Возможности
- просмотр каталога катамаранов
- бронирование на выбранную дату

## 1. Описание API

REST API на .NET 10 (Minimal API) для сервиса аренды катамаранов, упакованное в Docker-контейнер.
Реализует план из [api.md](api.md): каталог катамаранов и управление бронированием.
Данные хранятся в памяти (без БД).

| Endpoint | Описание |
|---|---|
| `GET /health` | Проверка работоспособности |
| `GET /catamarans` | Список катамаранов (название, тип, вместимость, цена за час) |
| `GET /catamarans/{id}` | Информация о катамаране (404, если нет) |
| `POST /bookings` | Создать бронь (дата, время, часы); 409 при пересечении по времени |
| `GET /bookings/{id}` | Статус брони |
| `DELETE /bookings/{id}` | Отменить бронь (статус → «Отменена») |

Связь с темой проекта: endpoint'ы реализуют ключевые возможности сервиса — просмотр каталога и бронирование на дату и время.

## 2. Структура проекта

```
katamarans/
├── Program.cs              # код API
├── CatamaranRental.csproj  # файл проекта .NET
├── Dockerfile              # сборка образа
├── .dockerignore
├── api.md                  # план API
└── README.md
```

## 3. Запуск локально (без Docker)

Нужен .NET SDK 10.0.

```powershell
dotnet run --urls http://localhost:8080
curl.exe http://localhost:8080/catamarans
```

## 4. Сборка и запуск в Docker

```powershell
# Сборка образа
docker build -t catamaran-rental-api .

# Запуск с пробросом порта: хост 8080 -> контейнер 8080
docker run -d --name catamaran-api -p 8080:8080 catamaran-rental-api
```

## 5. Проверка (PowerShell)

```powershell
docker ps

curl.exe http://localhost:8080/health
curl.exe http://localhost:8080/catamarans
curl.exe http://localhost:8080/catamarans/1

# создать бронь
Invoke-RestMethod -Method Post -Uri http://localhost:8080/bookings -ContentType "application/json" -Body '{"catamaranId":1,"date":"2026-10-20","time":"12:00:00","hours":2}'

Invoke-RestMethod http://localhost:8080/bookings/1
Invoke-RestMethod -Method Delete http://localhost:8080/bookings/1

docker logs catamaran-api
```

Остановка: `docker stop catamaran-api; docker rm catamaran-api`

## 6. Результаты проверки

> Результаты команд сохранены в папке `evidence/` (docker-build.txt, docker-ps.txt, api-response.txt, docker-logs.txt); скриншоты приведены в отчёте.

### docker build
```
(вывод)
```
### docker ps
```
(вывод)
```
### Ответ API
```
(вывод curl http://localhost:8080/catamarans)
```
### docker logs
```
(вывод)
```

## 7. Описание Dockerfile

| Инструкция | Зачем нужна |
|---|---|
| `FROM ...sdk:10.0 AS build` | Образ с .NET SDK для сборки (первый этап multi-stage) |
| `WORKDIR /src` | Рабочая папка в образе |
| `COPY CatamaranRental.csproj ./` + `RUN dotnet restore` | Восстановление зависимостей отдельным слоем (кэшируется) |
| `COPY . .` | Копирование исходников |
| `RUN dotnet publish -c Release -o /app/publish` | Компиляция в Release |
| `FROM ...aspnet:10.0 AS final` | Лёгкий образ только с рантаймом — итоговый образ меньше |
| `COPY --from=build` | Перенос результата публикации из этапа сборки |
| `ENV ASPNETCORE_URLS=http://+:8080` | Слушать порт 8080 на всех интерфейсах, иначе API недоступно снаружи контейнера |
| `EXPOSE 8080` | Документирует порт (реально публикуется через `-p`) |
| `ENTRYPOINT ["dotnet","CatamaranRental.dll"]` | Команда запуска приложения |

## 8. Вывод

Создано API по теме «Аренда катамаранов», написан Dockerfile, собран образ, контейнер запущен с пробросом порта 8080; работа подтверждена запросами к API, `docker ps` и `docker logs`.
