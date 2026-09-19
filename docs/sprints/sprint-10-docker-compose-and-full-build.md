# Спринт 10. Docker Compose и сборка всего проекта

## Цель

Собрать все части системы в единое Docker Compose окружение, чтобы проект можно было поднять одной командой.

После спринта backend, база, SeaweedFS, публичный сайт и админка должны работать вместе в контейнерах.

## Контекст

На предыдущих спринтах части проекта могли запускаться локально по отдельности. Теперь нужно связать их в полноценное окружение, близкое к production topology.

## Задачи

### Backend Dockerfile

- Создать Dockerfile для backend.
- Использовать multi-stage build.
- Выполнять restore/build/publish.
- Запускать опубликованное приложение.
- Прокинуть конфигурацию через environment variables.

### Public Web Dockerfile

- Создать Dockerfile для Next.js приложения.
- Настроить build.
- Настроить runtime.
- Передать backend API URL.
- Проверить работу server-side запросов из контейнера.

### Admin Web Dockerfile

- Создать Dockerfile для Vite-приложения.
- Собрать static assets.
- Поднять через nginx или другой легкий static server.
- Настроить route fallback для `/admin/*`.
- Передать backend API URL.

### docker-compose.yml

- Добавить сервисы:
  - `postgres`;
  - `seaweedfs`;
  - `api`;
  - `public-web`;
  - `admin-web`.
- Настроить networks.
- Настроить volumes.
- Настроить зависимости сервисов.
- Настроить порты:
  - `5000` для backend;
  - `3000` для public-web;
  - `5173` или другой порт для admin-web;
  - `5432` для PostgreSQL;
  - `8333` и `9333` для SeaweedFS.

### Environment variables

- Backend:
  - connection string;
  - JWT settings;
  - S3 settings;
  - CORS origins.
- Public Web:
  - backend API base URL.
- Admin Web:
  - backend API base URL.
- Добавить `.env.example`.

### Startup checks

- Проверить, что backend видит PostgreSQL.
- Проверить, что backend видит SeaweedFS по внутреннему адресу.
- Проверить, что browser может открыть публичный URL файла.
- Проверить, что public-web может ходить в backend.
- Проверить, что admin-web может ходить в backend.

### Миграции

- Все миграции должны быть заранее созданы через CLI EF Core; не создавать и не править migration-файлы вручную.
- Выбрать стратегию применения миграций:
  - автоматически на старте API в development;
  - отдельной командой;
  - отдельным migration container.
- Задокументировать выбранный вариант.

## Результат спринта

- Весь проект поднимается через Docker Compose.
- Все сервисы связаны между собой.
- Публичный сайт и админка работают с backend API.
- Файлы загружаются и доступны по URL.

## Definition of Done

- `docker compose up --build` поднимает все сервисы.
- Swagger доступен на `http://localhost:5000/swagger`.
- Public Web доступен на `http://localhost:3000`.
- Admin Web доступен на `http://localhost:5173/admin`.
- PostgreSQL сохраняет данные между перезапусками.
- SeaweedFS сохраняет файлы между перезапусками.
- README содержит команды запуска и остановки.

## Риски и решения

- Для frontend важно различать внутренний container URL и URL, доступный браузеру.
- SeaweedFS public URL в local окружении может отличаться от внутреннего S3 endpoint.
- Vite-приложению нужен fallback для client-side routing, иначе `/admin/about` может отдавать 404 при обновлении страницы.

## Что не входит в спринт

- Production deployment.
- HTTPS.
- Reverse proxy для единого домена.
- CI/CD.
