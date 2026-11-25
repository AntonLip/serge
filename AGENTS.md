# CleanDistrict — контекст для продолжения работы

## Назначение проекта

Дипломный прототип системы ООО «Чистый округ» для управления клиентами, объектами, договорами, расчётами, заявками, нарядами, бригадами и автопарком. Интерфейс русскоязычный, денежная единица — RUB (российский рубль), время хранится в UTC и отображается в Europe/Minsk.

Предметные правила зафиксированы в `docs/domain-rules.md`. Демонстрационные данные, учётные записи и запуск описаны в `docs/demo-data.md`. Сценарии и use case: `docs/precedents.md` (нарратив для ВКР), `docs/client-scenarios.md`, `docs/client-use-cases.md`, `docs/staff-scenarios.md`, `docs/staff-use-cases.md`; диаграммы — `docs/diagrams.md`. Перед изменением бизнес-логики сначала сверяться с этими документами; при изменении задокументированных правил обновлять документы и этот файл.

## Технологии и структура

- .NET 10, ASP.NET Core MVC, Razor, Bootstrap.
- PostgreSQL, Entity Framework Core, миграции EF Core.
- ASP.NET Identity для пользователей и ролей.
- SignalR для обновления диспетчерской доски.
- `src/CleanDistrict.Domain` — сущности и перечисления.
- `src/CleanDistrict.Application` — расчёт стоимости, правила переходов, роли, интерфейс файлового хранилища.
- `src/CleanDistrict.Infrastructure` — `CleanDistrictDbContext`, Identity, PostgreSQL, локальное файловое хранилище, миграции и `DemoDataSeeder`.
- `src/CleanDistrict.Web` — контроллеры, Razor-представления, модели ввода, SignalR hub.

Сохранять текущую структуру и стиль: file-scoped namespaces, первичные конструкторы контроллеров, серверные проверки ролей, POST и antiforgery для изменяющих операций. Не переносить бизнес-правила только в представления или JavaScript.

## Состояние окружения

В каталоге проекта нет `.git`: `git status` и `git log` недоступны. Не полагаться на историю коммитов и не утверждать, что изменения можно посмотреть через Git. Перед продолжением проверять актуальное содержимое файлов, поскольку эта точка остановки могла устареть.

Тестового проекта в решении нет. Не создавать тестовые проекты или отдельные отчёты без запроса пользователя.

## Запуск и проверка

Полный запуск в Docker:

```bash
docker compose up --build -d
```

Приложение доступно на `http://localhost:5080`; база, загрузки и ключи Data Protection сохраняются в именованных томах Compose.

Локальный запуск:

```bash
docker compose up -d postgres
dotnet restore
dotnet tool restore
dotnet ef database update --project src/CleanDistrict.Infrastructure/CleanDistrict.Infrastructure.csproj --startup-project src/CleanDistrict.Web/CleanDistrict.Web.csproj
dotnet run --project src/CleanDistrict.Web/CleanDistrict.Web.csproj
```

Для отдельного порта:

```bash
dotnet run --project src/CleanDistrict.Web/CleanDistrict.Web.csproj --urls http://localhost:5080
```

Стандартная быстрая проверка сборки:

```bash
dotnet build CleanDistrict.sln --no-restore
```

При Development-запуске автоматически применяются миграции и заполняется пустая база демонстрационными данными. Демонстрационные учётные записи указаны в `docs/demo-data.md`. Не удалять и не пересоздавать базу без явного разрешения пользователя.

## Точка остановки на 2026-11-19 (документация сценариев)

1. Добавлены `docs/staff-scenarios.md` (S1–S14), `docs/staff-use-cases.md` (UC-A/CL/CT/E/R/W/F/ADM/REP), `docs/diagrams.md` (Mermaid: актёры, UC-обзор, статусы заявки/наряда/техники/ТО, последовательности, ER).
2. Клиентские `client-scenarios.md` / `client-use-cases.md` сохранены; в `domain-rules.md` и этом файле добавлены ссылки на полный комплект.
3. Код и Docker в этом шаге не менялись.

## Первоочередная нерешённая задача

Нерешённых задач нет. Тестового проекта по-прежнему нет.

## Следующие проверки

1. При изменении бизнес-логики синхронизировать сценарии, use case и `diagrams.md`.
2. После код-изменений — `dotnet build` и сквозные сценарии; при смене модели — `has-pending-model-changes` до Docker.
3. SignalR-доска: визуальная проверка в браузере по-прежнему желательна.

После завершения очередного этапа обновлять разделы «Точка остановки», «Первоочередная нерешённая задача» и «Следующие проверки», чтобы следующий запуск продолжался с актуального состояния.

## Схемы
Все схемы делай в формате drawio, не надо делать никаких экспортов, я сам сделаю