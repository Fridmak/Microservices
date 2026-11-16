# Microservices

Backend системы управления задачами на ASP.NET Core 8: пользователи, задачи,
назначение исполнителей, статусы, история изменений и уведомления.
Четыре сервиса объединены API Gateway на YARP, а внешним edge-роутером
выступает WasmGate на Go.

## Архитектура

```
Клиент :8080
  │
  ▼
WasmGate (TCP/L4 + path_router WASM)
  ├── /api/*  → ApiGateway :7000    JWT-валидация, cookie → Authorization, CORS
  │                ├── AuthService :7001           регистрация, логин, JWT, справочник
  │                ├── TaskService :7002           задачи, права, история, outbox
  │                └── NotificationService :7003   уведомления, SignalR
  └── /ws*    → NotificationService :7003   SignalR-хаб (алиас /notificationHub)
```

WasmGate не заменяет YARP: он делает L4-релей и path-роутинг по первому
HTTP-пакету. YARP остаётся HTTP-gateway с аутентификацией и CORS.

Каждый сервис использует свою PostgreSQL-базу. Общие контракты, модели,
исключения и настройки лежат в `Shared`.

## Стек

- .NET 8, ASP.NET Core
- YARP 2.3
- WasmGate (Go, L4 edge + `path_router` WASM plugin)
- PostgreSQL 16 + EF Core 9 / Npgsql
- JWT + HttpOnly cookie, BCrypt
- SignalR
- xUnit + Testcontainers
- Docker Compose, GitHub Actions

## Что реализовано

### Аутентификация и пользователи

- Регистрация с нормализацией email, проверкой формата и уникальным индексом.
- BCrypt с ограничением пароля в 72 байта (ограничение алгоритма).
- JWT на 24 часа в HttpOnly + Secure + SameSite cookie.
- `POST /api/auth/logout` очищает cookie.
- Защита от подбора пароля: лимит попыток по email в AuthService и лимит
  login/register по IP на gateway.
- Справочник пользователей для назначения задач: `GET /api/auth/users`.
- Внутренний endpoint `GET /internal/users/{id}/exists` по `Service-Api-Key`.

### Задачи

- Создание, чтение, обновление, soft/hard delete и restore.
- Владелец задачи (`OwnerId`) и исполнитель (`AssignedToUserId`).
- Статусы: `Todo`, `InProgress`, `Done`, `Cancelled`; `CompletedAt` при `Done`.
- Приоритеты: `Low`, `Normal`, `High`, `Necessary` (сортировка исправлена:
  бизнес-порядок не зависит от числовых значений enum).
- Фильтры по приоритету, статусу, дедлайну и поиску по названию.
- Пагинация: `page`, `pageSize` (до 100), стабильная сортировка.
- История изменений с типом события, описанием и JSON-снимком состояния.
- Проверка существования исполнителя через AuthService.
- Optimistic concurrency (`xmin`) не включена — вынесена за рамки текущего объёма.

### Уведомления

- Transactional Outbox в `TaskDb`: задача, история и уведомление сохраняются
  атомарно; фоновый воркер доставляет уведомления с retry/backoff.
- Идемпотентный приём: `Notification.Id` — ключ идемпотентности, повторная
  доставка не создаёт дубль.
- Публичный API работает только с уведомлениями текущего пользователя.
- Пагинация, unread-count, mark-as-read, mark-all-as-read, delete.
- SignalR-хаб `/notificationHub` и алиас `/ws` для маршрута через WasmGate.
- Push `ReceiveNotification`; для `/ws` работает cookie-auth.
- Dead-letter сообщения видны в health check TaskService.

### Gateway и WasmGate

- WasmGate принимает клиентов на `:8080` и маршрутизирует:
  - `/api/*` → ApiGateway;
  - `/ws*` → NotificationService (SignalR);
  - `/health*` → ApiGateway.
- `path_router.wasm` из WasmGate выбирает пул по Host/path в первом пакете.
- Внешний edge даёт TLS-терминацию/лимиты/health checks WasmGate; локально
  используется HTTP.
- ApiGateway проксирует `/api/auth`, `/api/tasks`, `/api/notifications`.
- JWT-валидация на edge, политика `authenticated` на tasks/notifications.
- Cookie `auth_token` превращается в `Authorization` и не пересылается внутрь.
- CORS с `AllowCredentials` (origins задаются конфигом).
- Rate limit login/register по IP.
- Удалён нерабочий in-memory кэш ответов.
- Для `/ws` cookie проверяет NotificationService сам; gateway в этой ветке нет.

### Качество

- Единый `ProblemDetails` и глобальный обработчик исключений.
- Swagger в трёх бизнес-сервисах (только в Development, прямой порт сервиса).
- Health checks с проверкой БД; в TaskService — дополнительно outbox.
- Централизованные версии пакетов (`Directory.Packages.props`).
- Интеграционные тесты на Testcontainers PostgreSQL.
- Docker Compose для запуска одной командой.
- CI: build, tests, docker build.

## Матрица прав

| Действие | Владелец | Исполнитель | Другой пользователь |
|---|---|---|---|
| Читать задачу и историю | да | да | нет |
| Менять title/deadline/priority | да | нет | нет |
| Комментировать | да | да | нет |
| Менять статус | да | да | нет |
| Назначать/переназначать | да | нет | нет |
| Soft/hard delete, restore | да | нет | нет |

Недоступная задача возвращает `404`, доступная но запрещённая операция — `403`.

## Быстрый старт через Docker

Одна команда сама создаст `.env` со случайными секретами и поднимет весь стек:

```bash
# Windows
./scripts/up.ps1

# Linux / macOS
./scripts/up.sh
```

Если хотите управлять секретами вручную:

```bash
cp .env.example .env
# откройте .env и замените пароли/ключи
docker compose up -d --build
```

Сервисы:

| Сервис | URL |
|---|---|
| **WasmGate (публичная точка входа)** | **http://localhost:8080** |
| WasmGate admin (`/healthz`, `/metrics`) | http://localhost:19099 (Bearer token) |
| ApiGateway (внутренний/direct) | http://localhost:7000 |
| AuthService / Swagger | http://localhost:7001/swagger |
| TaskService / Swagger | http://localhost:7002/swagger |
| NotificationService / Swagger | http://localhost:7003/swagger |
| Health через WasmGate | `http://localhost:8080/health` |
| PostgreSQL (direct) | `localhost:55432` (пользователь `app`) |
| PostgreSQL через WasmGate L4 (demo) | `localhost:5433` (пользователь `app`) |

Миграции применяются автоматически при старте контейнеров
(`Database__MigrateOnStartup=true`). Полный сброс:

```bash
docker compose down -v
```

WasmGate собирается из закреплённого коммита
`WASMGATE_REF` (по умолчанию `4d9448f...`). Чтобы показать более свежую версию
WasmGate:

```bash
WASMGATE_REF=main docker compose up -d --build wasmgate
```

Токен для `:19099` лежит в `.env` как `WASMGATE_ADMIN_TOKEN` (скрипт выше
генерирует его автоматически).

Публичные API-запросы идут через `http://localhost:8080`, а не напрямую в
gateway. Direct-порты нужны для Swagger, отладки и SignalR-хаба.

### SignalR через WasmGate

Клиент подключается к `http://localhost:8080/ws` с `withCredentials: true`.
WasmGate по пути `/ws*` отправляет соединение в NotificationService, минуя
YARP. NotificationService принимает `auth_token` из cookie (или
`access_token` из query для прямых клиентов).

Ограничения `path_router`: он смотрит первый пакет, рассчитан на HTTP/1.1 и
не является полноценным HTTP-роутером. Для production предпочтительнее
TLS-терминация на WasmGate и/или раздельные hostname `api.*` и `ws.*` с
SNI-роутингом.

### PostgreSQL через WasmGate

Порт `localhost:5433` — отдельный L4-listener WasmGate, который релеит сырой
PostgreSQL-трафик в `postgres:5432`:

```bash
psql "host=localhost port=5433 user=app dbname=postgres"
```

Это **демонстрационный** маршрут: сервисы по-прежнему подключаются к
`postgres:5432` напрямую. Ставить БД на критический путь edge-прокси в
production не стоит — это лишняя точка отказа и циклическая зависимость
(приложение → WasmGate → PostgreSQL).

## Локальный запуск

Нужен PostgreSQL и три базы: `AuthDb`, `TaskDb`, `NotificationsDb`.

Секреты не хранятся в репозитории. Для локальной разработки используйте
user-secrets или переменные окружения. Пример:

```bash
dotnet user-secrets set "Jwt:Key" "your-32-byte-secret" --project AuthService
dotnet user-secrets set "Jwt:Key" "your-32-byte-secret" --project TaskService
dotnet user-secrets set "Jwt:Key" "your-32-byte-secret" --project NotificationService
dotnet user-secrets set "Jwt:Key" "your-32-byte-secret" --project ApiGateway
```

Ключ должен быть одинаковым во всех сервисах. Также задайте:

- `ConnectionStrings:DefaultConnection`
- `Services:Security:ApiKey` в AuthService и NotificationService
- `Services:AuthService:ApiKey` и `Services:NotificationService:ApiKey` в TaskService

Запуск в отдельных терминалах:

```bash
dotnet run --project ApiGateway          --urls "http://localhost:7000"
dotnet run --project AuthService         --urls "http://localhost:7001"
dotnet run --project TaskService         --urls "http://localhost:7002"
dotnet run --project NotificationService --urls "http://localhost:7003"
```

Применить миграции вручную:

```bash
dotnet ef database update --project AuthService
dotnet ef database update --project TaskService
dotnet ef database update --project NotificationService
```

## Основные API

Все запросы, кроме регистрации и логина, требуют аутентификации.

### Auth

| Метод | Путь | Назначение |
|---|---|---|
| POST | `/api/auth/register` | Регистрация |
| POST | `/api/auth/login` | Логин, устанавливает cookie |
| POST | `/api/auth/logout` | Очистка cookie |
| GET | `/api/auth/me` | Текущий пользователь |
| GET | `/api/auth/users?search=&page=&pageSize=` | Справочник пользователей |
| GET | `/api/auth/users/{id}` | Краткая информация о пользователе |

### Tasks

| Метод | Путь | Назначение |
|---|---|---|
| GET | `/api/tasks?scope=Assigned\|Owned&page=&pageSize=&sortBy=&priorityFilter=&statusFilter=&searchTerm=&deadlineFrom=&deadlineTo=` | Список задач |
| GET | `/api/tasks/{id}` | Задача |
| POST | `/api/tasks` | Создать задачу |
| PUT | `/api/tasks/{id}` | Обновить title/deadline/priority/комментарии |
| PATCH | `/api/tasks/{id}/status` | Сменить статус |
| PUT | `/api/tasks/{id}/assign` | Назначить исполнителя |
| GET | `/api/tasks/{id}/history?page=&pageSize=` | История |
| DELETE | `/api/tasks/{id}?hard=false` | Soft/hard delete |
| POST | `/api/tasks/{id}/restore` | Восстановить |

### Notifications

| Метод | Путь | Назначение |
|---|---|---|
| GET | `/api/notifications?unreadOnly=&page=&pageSize=` | Уведомления текущего пользователя |
| GET | `/api/notifications/unread-count` | Непрочитанные |
| PUT | `/api/notifications/{id}/read` | Отметить прочитанным |
| PUT | `/api/notifications/read-all` | Отметить все |
| DELETE | `/api/notifications/{id}` | Удалить |

Внутренний endpoint `POST /internal/notifications` доступен только с
`Service-Api-Key` и не маршрутизируется через gateway.

Postman-коллекция: `Spec.postman_collection.json`.

## Тесты

```bash
dotnet test tests/Microservices.IntegrationTests/Microservices.IntegrationTests.csproj
```

Тесты поднимают PostgreSQL через Testcontainers и проверяют:

- регистрацию, логин, logout, уникальность email и валидацию;
- права владельца/исполнителя, историю, статусы, delete/restore;
- исправленную сортировку приоритетов;
- идемпотентность уведомлений и изоляцию по пользователю.

Для интеграционных тестов нужен Docker. CI workflow: `.github/workflows/ci.yml`.

## Ограничения и дальнейшие шаги

- **WasmGate path_router — не полноценный HTTP-роутер.** Он смотрит только
  первый клиентский пакет; при разорванном request line соединение уйдёт в
  default pool. Для production используйте TLS-терминацию и SNI/разные hostname
  `api.*` и `ws.*`, либо YARP-маршрут `/notificationHub`.
- **Хаб также доступен напрямую на `:7003`** (`/notificationHub`) для отладки;
  публичный маршрут — `http://localhost:8080/ws`.
- Нет frontend-приложения.
- Нет ролей/админки и refresh-токенов; JWT живёт 24 часа.
- Rate limiter в AuthService хранится в памяти — для нескольких инстансов нужен
  Redis.
- Hard delete сохраняет историю как аудит, но публичного API для чтения истории
  удалённой задачи нет.
- При нескольких репликах TaskService outbox использует атомарный claim, но
  для полноценного scale-out стоит вынести в отдельный worker.
- Секреты, ранее попадавшие в git-историю, нужно считать скомпрометированными
  и ротировать.

## Лицензия

См. `LICENSE.txt`.
