# Спринт 00. Подготовка проекта

## Цель

Создать технический фундамент репозитория, чтобы backend, публичный сайт, админка и инфраструктура могли развиваться независимо, но в рамках одной согласованной структуры.

На этом спринте не требуется реализовывать бизнес-логику. Главная задача - получить пустые, но запускаемые приложения и минимальное локальное окружение.

## Контекст

Проект состоит из нескольких частей:

- backend API на .NET 8;
- публичный сайт на Next.js;
- административная панель на React + Vite;
- PostgreSQL;
- SeaweedFS;
- Docker Compose.

Для дальнейшей разработки важно сразу зафиксировать структуру каталогов, базовые команды запуска и переменные окружения.

## Задачи

### Репозиторий

- Создать или проверить структуру проекта:
  - `backend/`
  - `public-web/`
  - `admin-web/`
  - `docs/`
  - `docker-compose.yml`
- Добавить общий `.gitignore` для .NET, Node.js, Docker и локальных `.env` файлов.
- Добавить корневой `README.md` с кратким описанием проекта и базовыми командами.
- Добавить `.env.example` или отдельные примеры env-файлов для backend/frontend.

### Backend skeleton

- Создать .NET solution в `backend/`.
- Добавить проекты:
  - `Portfolio.Api`
  - `Portfolio.Application`
  - `Portfolio.Domain`
  - `Portfolio.Infrastructure`
- Настроить ссылки между проектами:
  - `Portfolio.Api` зависит от `Application` и `Infrastructure`;
  - `Portfolio.Application` зависит от `Domain`;
  - `Portfolio.Infrastructure` зависит от `Application` и `Domain`.
- Добавить минимальный `Program.cs`.
- Проверить запуск пустого API.

### Public Web skeleton

- Создать Next.js приложение в `public-web/`.
- Использовать TypeScript.
- Подготовить базовую структуру:
  - `app/`
  - `components/`
  - `lib/`
  - `styles/`
- Проверить локальный запуск.

### Admin Web skeleton

- Создать Vite-приложение в `admin-web/`.
- Использовать React + TypeScript.
- Подготовить базовую структуру:
  - `src/app/`
  - `src/components/`
  - `src/features/`
  - `src/shared/`
- Проверить локальный запуск.

### Docker Compose foundation

- Добавить `postgres` сервис.
- Добавить `seaweedfs` сервис.
- Настроить базовые volume для хранения данных.
- Проверить, что сервисы стартуют локально.

## Рекомендуемые локальные порты

- Backend API: `http://localhost:5000`
- Public Web: `http://localhost:3000`
- Admin Web dev server: `http://localhost:5173`
- Admin Web route: `http://localhost:5173/admin`
- PostgreSQL: `localhost:5432`
- SeaweedFS S3: `http://localhost:8333`
- SeaweedFS UI: `http://localhost:9333`

## Результат спринта

- Репозиторий имеет понятную структуру.
- Backend API запускается.
- Public Web запускается.
- Admin Web запускается.
- PostgreSQL и SeaweedFS поднимаются через Docker Compose.
- Есть минимальные инструкции по запуску.

## Definition of Done

- `dotnet build` проходит для backend solution.
- `npm run build` или аналогичная команда проходит для frontend-приложений, если они уже настроены.
- `docker compose up` поднимает PostgreSQL и SeaweedFS.
- В репозитории нет секретов.
- Новый разработчик может понять, как запустить проект, из README.

## Что не входит в спринт

- Авторизация.
- База данных и миграции.
- Реальные API endpoints.
- UI публичного сайта.
- UI админки.
- Загрузка файлов.
