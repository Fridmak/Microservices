# Microservices

Учебный проект: четыре ASP.NET Core-сервиса с API Gateway, JWT-аутентификацией,
историей задач и real-time уведомлениями через SignalR.

## Стек

- .NET 8, ASP.NET Core
- YARP 2.3 (reverse proxy / API Gateway)
- PostgreSQL + EF Core 8/9 (Npgsql)
- JWT + HttpOnly cookie, BCrypt для паролей
- SignalR
- Swagger / Postman

## Архитектура

```
Клиент
  │
  ▼
ApiGateway :7000          JWT-валидация, проксирование, кэш ответов
  ├── AuthService :7001   регистрация, логин, выдача JWT
  ├── TaskService :7002   CRUD задач, сортировка, история, soft delete
  └── NotificationService :7003   SignalR-хаб и уведомления
```

Общие DTO и модели лежат в `Shared`.

## Сервисы

| Сервис | Порт | Ответственность |
|---|---|---|
| `ApiGateway` | 7000 | YARP, проверка JWT, проксирование, in-memory кэш ответов |
| `AuthService` | 7001 | Регистрация, логин, BCrypt, выпуск JWT |
| `TaskService` | 7002 | Задачи: создание, назначение, обновление, удаление, история, сортировка |
| `NotificationService` | 7003 | SignalR-хаб, уведомления о событиях задач |

## Что реализовано

- Регистрация и логин с хешированием паролей через BCrypt.
- JWT-аутентификация: токен кладётся в HttpOnly + Secure + SameSite=Strict cookie,
  gateway перекладывает его в заголовок `Authorization`.
- Единая точка входа через YARP: клиент не знает про внутренние порты сервисов.
- Кэш ответов gateway в памяти (`IMemoryCache` + middleware).
- Задачи: приоритеты (Low / Normal / High / Necessary), дедлайны, назначение
  исполнителя, сортировка по приоритету / дедлайну / дате создания.
- История изменений задачи и soft delete.
- Уведомления через SignalR: создание, назначение, обновление, удаление,
  переназначение задачи.
- Swagger в каждом сервисе, общая Postman-коллекция: `Spec.postman_collection.json`.
- Миграции EF Core для Auth, Task и Notification баз.

## Как запустить

Нужен PostgreSQL и строка подключения `DefaultConnection` в `appsettings.json`
каждого сервиса. Затем в отдельных терминалах:

```bash
dotnet run --project ApiGateway     --urls "http://localhost:7000"
dotnet run --project AuthService    --urls "http://localhost:7001"
dotnet run --project TaskService    --urls "http://localhost:7002"
dotnet run --project NotificationService --urls "http://localhost:7003"
```

Применить миграции:

```bash
dotnet ef database update --project AuthService
dotnet ef database update --project TaskService
dotnet ef database update --project NotificationService
```

Все запросы идут через gateway: `http://localhost:7000`.

## API

Импортируй `Spec.postman_collection.json` в Postman. Основные группы:

- `POST /api/auth/register`, `POST /api/auth/login`
- задачи: создание, список, обновление, назначение, удаление
- SignalR-хаб в `NotificationService`

## Что бы улучшил

- Docker Compose для PostgreSQL и всех сервисов в одну команду.
- Интеграционные тесты через `WebApplicationFactory` и Testcontainers.
- Вынести общие настройки JWT и логирование в `Shared`.
- Health checks и централизованная конфигурация.

## Лицензия

См. `LICENSE.txt`.
