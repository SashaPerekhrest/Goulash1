# Portfolio CMS

Портфолио-лендинг с управляемым контентом для демонстрации опыта в ИИ, backend/frontend-разработке, интеграциях и автоматизации.

Проект состоит из публичного сайта, административной панели, backend API, PostgreSQL и SeaweedFS. Sprint 00 подготавливает только запускаемый технический фундамент без бизнес-логики.

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
```

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

## Инфраструктура

```bash
docker compose up -d
docker compose ps
docker compose down
```

На sprint 00 через Docker Compose поднимаются только PostgreSQL и SeaweedFS. Backend, public-web и admin-web запускаются локально отдельными командами.

## Переменные окружения

Скопируйте `.env.example` в локальный `.env` при необходимости и замените placeholder-значения. Реальные секреты не должны попадать в репозиторий.
