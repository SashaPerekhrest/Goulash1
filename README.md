# Portfolio CMS

Портфолио-лендинг с управляемым контентом для демонстрации опыта в ИИ, backend/frontend-разработке, интеграциях и автоматизации.

Проект состоит из публичного сайта, административной панели, backend API, PostgreSQL и SeaweedFS. Sprint 01 добавляет backend foundation: доменную модель, EF Core, PostgreSQL, миграции, Swagger и стартовые данные.

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
- Public Web: http://localhost:3000
- Admin Web: http://localhost:5173/admin
- PostgreSQL: localhost:5432
- SeaweedFS S3: http://localhost:8333
- SeaweedFS UI: http://localhost:9333

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

В `Development` API может автоматически применить EF Core migrations и создать seed-данные: администратора и страницу `about`. Для этого включите `Database:ApplyMigrationsOnStartup=true` в локальном `appsettings.Development.json` или через переменную окружения `Database__ApplyMigrationsOnStartup=true`. Учетные данные администратора берутся из `Seed:Admin:Login` и `Seed:Admin:Password`; локальные значения предназначены только для разработки. Пароль сохраняется в базе как PBKDF2-хеш.

PageContent API для страницы "О себе":

- `GET /api/pages/about` - публичное получение HTML-контента.
- `GET /api/admin/pages/about` - получение HTML-контента для админки, требует Bearer token.
- `PUT /api/admin/pages/about` - обновление HTML-контента, требует Bearer token.

HTML-контент редактируется через защищенную админку и проходит backend sanitizer с whitelist тегов и атрибутов. Перед финальной сдачей нужно повторно проверить whitelist в рамках security pass.

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

```bash
docker compose up -d
docker compose ps
docker compose down
```

На sprint 00 через Docker Compose поднимаются только PostgreSQL и SeaweedFS. Backend, public-web и admin-web запускаются локально отдельными командами.

## Переменные окружения

Скопируйте `.env.example` в локальный `.env` при необходимости и замените placeholder-значения. Реальные секреты не должны попадать в репозиторий.
