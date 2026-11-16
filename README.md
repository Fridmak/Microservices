# Microservices

Backend системы управления задачами на ASP.NET Core 8: пользователи, задачи,
назначение исполнителей, статусы, история изменений и уведомления.
Четыре сервиса объединены API Gateway на YARP.

## Архитектура

```
Клиент
  │
  ▼
ApiGateway :7000          JWT-валидация, cookie → Authorization, CORS, rate limit
  ├── AuthService :7001           регистрация, логин, JWT, справочник пользователей
  ├── TaskService :7002           задачи, права, статусы, история, outbox
  └── NotificationService :7003   уведомления, SignalR-хаб
```

Каждый сервис использует свою PostgreSQL-базу. Общие контракты, модели,
исключения и настройки лежат в `Shared`.

## Стек

- .NET 8, ASP.NET Core
- YARP 2.3
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
- SignalR-хаб `/notificationHub` и push `ReceiveNotification`.
- Dead-letter сообщения видны в health check TaskService.

### Gateway

- Проксирование `/api/auth`, `/api/tasks`, `/api/notifications`.
- JWT-валидация на edge, политика `authenticated` на tasks/notifications.
- Cookie `auth_token` превращается в `Authorization` и не пересылается внутрь.
- CORS с `AllowCredentials` (origins задаются конфигом).
- Rate limit login/register по IP.
- Удалён нерабочий in-memory кэш ответов.

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

```bash
cp .env.example .env
# откройте .env и замените пароли/ключи
docker compose up -d --build
```

Сервисы:

| Сервис | URL |
|---|---|
| Gateway | http://localhost:7000 |
| AuthService / Swagger | http://localhost:7001/swagger |
| TaskService / Swagger | http://localhost:7002/swagger |
| NotificationService / Swagger | http://localhost:7003/swagger |
| Health | `http://localhost:7000/health` и `/health` каждого сервиса |
| PostgreSQL | `localhost:55432` (пользователь `app`) |

Миграции применяются автоматически при старте контейнеров
(`Database__MigrateOnStartup=true`). Полный сброс:

```bash
docker compose down -v
```

Все публичные запросы идут через gateway. Direct-порты нужны для Swagger и
для SignalR-хаба.

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

- **SignalR через gateway не проксируется** — это оставлено под отдельные планы.
  Хаб доступен напрямую на `:7003`; токен передаётся в `access_token` query.
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
