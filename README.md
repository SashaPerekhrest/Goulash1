# Portfolio CMS

Портфолио-лендинг с управляемым контентом для демонстрации опыта в ИИ, backend/frontend-разработке, интеграциях и автоматизации.

Проект состоит из публичного сайта, административной панели, backend API, PostgreSQL и SeaweedFS. Все сервисы можно поднять одной командой через Docker Compose.

## Стек

- Backend: .NET 8, ASP.NET Core Web API, Entity Framework Core, JWT Bearer, Swagger / OpenAPI.
- Public Web: Next.js, React, TypeScript, server-rendered pages, metadata API.
- Admin Web: React, Vite, TypeScript, MUI, React Router, TanStack Query, React Hook Form, TinyMCE.
- Data: PostgreSQL.
- Files: SeaweedFS в S3-compatible режиме.
- Infra: Docker, Docker Compose.

## Структура

```text
backend/
  src/
    Portfolio.Api/
    Portfolio.Application/
    Portfolio.Domain/
    Portfolio.Infrastructure/
public-web/
admin-web/
docs/
docker-compose.yml
```

## Локальные URL

- Backend API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Public Web: http://localhost:3000
- Admin Web: http://localhost:5173/admin
- PostgreSQL: localhost:5432
- SeaweedFS S3: http://localhost:8333
- SeaweedFS UI: http://localhost:9333

## Docker Compose запуск

```bash
docker compose up --build -d
docker compose ps
```

Остановка окружения:

```bash
docker compose down
```

Данные PostgreSQL и файлы SeaweedFS сохраняются в Docker volumes:

- `postgres-data`
- `seaweedfs-data`

Для полной очистки локальных данных используйте `docker compose down -v`.

Development-доступ в админку для Docker-окружения:

```text
login: docker-admin
password: portfolio_dev_password
```

Пароль предназначен только для локальной демонстрации. Его можно переопределить через `.env`.

## Backend

```bash
cd backend
dotnet build Portfolio.slnx
dotnet run --project src/Portfolio.Api/Portfolio.Api.csproj
```

Проверка API:

```text
GET http://localhost:5000/health
Swagger: http://localhost:5000/swagger
```

В `Development` API может автоматически применить EF Core migrations и создать seed-данные: администратора и страницу `about`. Для этого включите `Database:ApplyMigrationsOnStartup=true` в локальном `appsettings.Development.json` или через переменную окружения `Database__ApplyMigrationsOnStartup=true`. Docker Compose включает это поведение для локальной демонстрации. Учетные данные администратора берутся из `Seed:Admin:Login` и `Seed:Admin:Password`; локальные значения предназначены только для разработки. Пароль сохраняется в базе как PBKDF2-хеш.

Seed также создает demo content для страницы "О себе", несколько опубликованных проектов категорий `AI` и `OTHER`, а также один неопубликованный проект для проверки того, что публичный API не раскрывает черновики.

PageContent API для страницы "О себе":

- `GET /api/pages/about` - публичное получение HTML-контента.
- `GET /api/admin/pages/about` - получение HTML-контента для админки, требует Bearer token.
- `PUT /api/admin/pages/about` - обновление HTML-контента, требует Bearer token.

HTML-контент редактируется через защищенную админку и проходит backend sanitizer с whitelist тегов и атрибутов. Перед финальной сдачей нужно повторно проверить whitelist в рамках security pass.

Загрузка файлов ограничена изображениями `jpeg`, `png`, `webp`, `svg` до 5 MB. SVG дополнительно разбирается как XML: DTD запрещены, активное содержимое и небезопасные ссылки отклоняются.

## Public Web

```bash
cd public-web
npm install
npm run dev
npm run build
```

## Admin Web

```bash
cd admin-web
npm install
npm run dev
npm run build
```

В локальной разработке админка открывается по адресу `http://localhost:5173/admin`.
API endpoint задается через `VITE_API_BASE_URL`, по умолчанию используется `http://localhost:5000`.
Раздел `/admin/about` загружает страницу `about`, редактирует HTML через TinyMCE и сохраняет изменения через `PUT /api/admin/pages/about`.
На текущем MVP-этапе JWT access token хранится в `localStorage`; это удобно для локальной демонстрации, но финальный security pass должен повторно оценить этот компромисс.

## Инфраструктура

Docker Compose поднимает:

- `postgres`
- `seaweedfs`
- `api`
- `public-web`
- `admin-web`

Backend-контейнер использует внутренние адреса `postgres:5432` и `seaweedfs:8333`. Публичный URL файлов для браузера остается `http://localhost:8333/portfolio`.

Public Web внутри контейнера ходит к backend по `API_URL`, по умолчанию `http://api:5000`. Browser-visible URL для публичного сайта задается через `NEXT_PUBLIC_API_URL`, а для статической админки через `VITE_API_BASE_URL`; в локальном Docker Compose оба обычно остаются `http://localhost:5000`. Admin Web является статической Vite-сборкой под nginx; для маршрутов `/admin/*` настроен fallback на `index.html`.

Миграции EF Core заранее лежат в репозитории и применяются автоматически при старте API в `Development` окружении. Migration-файлы не нужно писать вручную; новые миграции создаются через EF Core CLI.

## Переменные окружения

Скопируйте `.env.example` в локальный `.env` при необходимости и замените placeholder-значения. Реальные секреты не должны попадать в репозиторий.

Основные переменные:

- `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`
- `SEED_ADMIN_LOGIN`, `SEED_ADMIN_PASSWORD`
- `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_SECRET`, `JWT_LIFETIME_MINUTES`
- `STORAGE_BUCKET_NAME`, `STORAGE_REGION`, `STORAGE_ACCESS_KEY`, `STORAGE_SECRET_KEY`
- `API_URL`, `NEXT_PUBLIC_API_URL`, `VITE_API_BASE_URL`

## Основные маршруты

Public Web:

- `/` - страница "О себе".
- `/ai-projects` - опубликованные проекты категории `AI`.
- `/other-projects` - опубликованные проекты категории `OTHER`.
- `/projects/[slug]` - детальная страница проекта.

Admin Web:

- `/admin/login` - вход администратора.
- `/admin/about` - редактирование страницы "О себе".
- `/admin/ai-projects` - управление AI-проектами.
- `/admin/other-projects` - управление другими проектами.

Backend API:

- `POST /api/auth/login`
- `GET /api/auth/me`
- `GET /api/pages/about`
- `GET /api/projects?category=AI`
- `GET /api/projects/{slug}`
- `GET|PUT /api/admin/pages/about`
- `GET|POST /api/admin/projects`
- `GET|PUT|DELETE /api/admin/projects/{id}`
- `POST /api/admin/files`

## Финальная проверка

Ручной smoke checklist для сдачи лежит в [`docs/smoke-checklist.md`](docs/smoke-checklist.md).

Минимальный набор команд перед демонстрацией:

```bash
dotnet build backend/Portfolio.slnx
cd public-web && npm run build
cd ../admin-web && npm run build
cd .. && docker compose up --build -d
```

## Известные ограничения

- JWT access token в админке хранится в `localStorage`. Для production лучше перейти на httpOnly cookie или другой вариант с меньшим XSS-риском.
- HTML-контент разрешен осознанно, потому что он редактируется администратором через TinyMCE. Backend sanitizer использует whitelist, но для production стоит заменить самописный sanitizer на хорошо поддерживаемую библиотеку и покрыть его отдельными security-тестами.
- SeaweedFS в локальном Docker Compose открыт для удобства демонстрации. В production bucket лучше закрыть и отдавать файлы через CDN или backend proxy.
- Docker Compose предназначен для локальной демонстрации и ревью. CI/CD, Kubernetes и cloud deployment не входят в финальный спринт.
