# Аренда катамаранов

Сервис бронирования катамаранов с выбором даты, времени и типа судна.

В этой части проекта реализован CRUD API для сущности **Catamaran** (каталог судов). Данные хранятся в PostgreSQL, всё запускается через Docker Compose.

## 1. Сущность и связь с темой проекта

Основная сущность — `Catamaran` (катамаран). Это объект аренды: без каталога судов нельзя реализовать бронирование.

| Поле | Тип | Описание |
|---|---|---|
| `Id` | int | Первичный ключ, генерируется БД |
| `Name` | string | Название катамарана |
| `Capacity` | int | Вместимость (человек) |
| `PricePerHour` | decimal | Цена аренды за час |
| `Status` | string | Статус, по умолчанию `available` |

Через CRUD можно добавить судно в каталог, изменить цену или статус и убрать судно из парка. Позже на этой сущности строится бронирование.

## 2. Описание API

REST API на .NET 8 (Minimal API) + Entity Framework Core + Npgsql.

| Метод | Endpoint | Описание | Ответ |
|---|---|---|---|
| GET | `/` | Проверка работоспособности | 200 `{"status":"ok"}` |
| GET | `/catamarans` | Список катамаранов | 200 |
| POST | `/catamarans` | Создать катамаран | 201 + созданный объект |
| PUT | `/catamarans/{id}` | Обновить катамаран | 204 / 404 |
| DELETE | `/catamarans/{id}` | Удалить катамаран | 200 / 404 |

Пример тела запроса для `POST` и `PUT`:

```json
{
  "name": "Sea Breeze",
  "capacity": 4,
  "pricePerHour": 1500,
  "status": "available"
}
```

## 3. Структура проекта

```
katamarans/
├── Program.cs              # API, модель Catamaran, AppDbContext
├── CatamaranRental.csproj  # файл проекта .NET 8
├── Dockerfile              # multi-stage сборка образа API
├── compose.yaml            # сервисы server и db, том, секрет
├── db-password.txt         # пароль БД (НЕ хранится в git)
├── .dockerignore
├── .gitignore
├── api.md
└── README.md
```

## 4. Docker Compose

Файл `compose.yaml` описывает два сервиса.

### Сервис `server`
- Образ собирается из `Dockerfile` (`target: runtime`).
- Порт `8080` хоста проброшен на `8080` контейнера.
- Запускается после того, как `db` стала healthy (`depends_on` с `condition: service_healthy`).
- Получает пароль БД через секрет `db-password`.

### Сервис `db`
- Образ `postgres:16`, `restart: always`.
- Данные хранятся в именованном томе `db_data`.
- Healthcheck: `pg_isready -U postgres -d app` (интервал 5 с, 5 попыток, `start_period` 10 с).
- Порт БД наружу не публикуется: к ней можно обратиться только из контейнера `server`.

### Переменные окружения

| Переменная | Сервисы | Назначение |
|---|---|---|
| `POSTGRES_SERVER=db` | server | Хост БД (имя сервиса в Compose) |
| `POSTGRES_USER=postgres` | server, db | Пользователь БД |
| `POSTGRES_DB=app` | server, db | Имя базы данных |
| `POSTGRES_PASSWORD_FILE=/run/secrets/db-password` | server, db | Путь к файлу с паролем |

### Секрет
Пароль не записан в `compose.yaml`. Он берётся из файла `db-password.txt` и монтируется в контейнеры как `/run/secrets/db-password`. Файл добавлен в `.gitignore` и `.dockerignore`.

Перед первым запуском создайте файл с паролем:

```powershell
Set-Content -NoNewline -Path db-password.txt -Value "ваш_пароль"
```

### Сеть
Отдельная сеть в файле не описана, поэтому Compose создаёт сеть по умолчанию (`<проект>_default`). Оба сервиса подключены к ней и обращаются друг к другу по имени сервиса. Поэтому API подключается к БД по адресу `db`.

### Volume `db_data`
Именованный том смонтирован в `/var/lib/postgresql/data`, где PostgreSQL хранит файлы базы. Том существует отдельно от контейнера, поэтому данные сохраняются после `docker compose down` и пересоздания контейнеров. Удаляются они только командой `docker compose down -v`.

## 5. Почему PostgreSQL и как API подключается к `db`

PostgreSQL выбрана потому, что это надёжная реляционная СУБД с хранением данных на диске: данные не теряются при перезапуске API. Для неё есть официальный Docker-образ и провайдер для EF Core (Npgsql). Она хорошо подходит для будущих связей «катамаран — бронирование».

Подключение устроено так:
1. Compose передаёт в контейнер `server` переменные `POSTGRES_SERVER`, `POSTGRES_DB`, `POSTGRES_USER` и `POSTGRES_PASSWORD_FILE`.
2. `Program.cs` читает пароль из файла секрета и собирает строку подключения `Host=db;Database=app;Username=postgres;Password=...`.
3. Имя `db` совпадает с именем сервиса, Docker DNS подставляет IP контейнера БД.
4. При старте API вызывает `EnsureCreated()` (до 10 попыток с паузой 3 с) и создаёт таблицу `Catamarans`, если её ещё нет.

## 6. Запуск

Нужны Docker и Docker Compose. Файл `db-password.txt` должен существовать (см. выше).

```powershell
docker compose up -d --build
docker compose ps
docker compose logs server
```

Остановка:

```powershell
docker compose down        # остановить, данные сохраняются
docker compose down -v     # остановить и удалить данные (том db_data)
```

## 7. Проверка CRUD (PowerShell)

```powershell
# Проверка работоспособности
curl.exe http://localhost:8080/

# Create
Invoke-RestMethod -Method Post -Uri http://localhost:8080/catamarans -ContentType "application/json" -Body '{"name":"Sea Breeze","capacity":4,"pricePerHour":1500,"status":"available"}'

# Read
curl.exe http://localhost:8080/catamarans

# Update
Invoke-RestMethod -Method Put -Uri http://localhost:8080/catamarans/1 -ContentType "application/json" -Body '{"name":"Sea Breeze","capacity":4,"pricePerHour":1800,"status":"rented"}'
curl.exe http://localhost:8080/catamarans

# Delete
Invoke-RestMethod -Method Delete -Uri http://localhost:8080/catamarans/1
curl.exe http://localhost:8080/catamarans
```

### Результаты

> Вставьте сюда вывод своих команд.

**POST /catamarans**
```
(вывод)
```
**GET /catamarans**
```
(вывод)
```
**PUT /catamarans/1, затем GET**
```
(вывод)
```
**DELETE /catamarans/1, затем GET**
```
(вывод)
```

## 8. Проверка сохранения данных после перезапуска

1. Создать запись через `POST /catamarans`.
2. Остановить контейнеры без удаления тома:
   ```powershell
   docker compose down
   ```
3. Запустить снова:
   ```powershell
   docker compose up -d
   ```
4. Запросить список:
   ```powershell
   curl.exe http://localhost:8080/catamarans
   ```
   Созданная запись на месте, потому что данные лежат в томе `db_data`.
5. Для контраста: `docker compose down -v`, затем `docker compose up -d`. Список пуст, потому что том удалён.

### Результат

```
(вывод GET /catamarans после перезапуска)
```

## 9. Описание Dockerfile

| Инструкция | Зачем нужна |
|---|---|
| `FROM ...sdk:8.0 AS build` | Образ с .NET SDK для сборки (первый этап multi-stage) |
| `WORKDIR /src` | Рабочая папка на этапе сборки |
| `COPY CatamaranRental.csproj` + `RUN dotnet restore` | Восстановление зависимостей отдельным слоем (кэшируется) |
| `COPY . .` | Копирование исходников |
| `RUN dotnet publish -c Release -o /app/publish` | Компиляция в Release |
| `FROM ...aspnet:8.0 AS runtime` | Лёгкий образ только с рантаймом |
| `COPY --from=build` | Перенос результата публикации из этапа сборки |
| `ENV ASPNETCORE_URLS=http://+:8080` | Слушать порт 8080 на всех интерфейсах |
| `EXPOSE 8080` | Документирует порт (публикуется через `ports` в Compose) |
| `ENTRYPOINT ["dotnet","CatamaranRental.dll"]` | Команда запуска приложения |

## 10. Вывод

Реализован CRUD API для сущности `Catamaran` по теме «Аренда катамаранов». API и PostgreSQL запускаются через Docker Compose, пароль передаётся через Docker secret, данные хранятся в томе `db_data` и сохраняются после перезапуска контейнеров.
