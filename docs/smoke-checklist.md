# Smoke checklist

Чеклист используется в финальном спринте перед сдачей проекта. Он покрывает основные сценарии из ТЗ и `docs/sprints/sprint-11-polish-security-and-delivery.md`.

## Backend API

- `GET /health` возвращает `200 OK`.
- Swagger доступен по `http://localhost:5000/swagger` в `Development`.
- `POST /api/auth/login` возвращает JWT для development-администратора.
- `GET /api/auth/me` без токена возвращает `401`.
- `GET /api/auth/me` с валидным токеном возвращает текущего пользователя.
- `GET /api/admin/pages/about` без токена возвращает `401`.
- `GET /api/admin/projects` без токена возвращает `401`.
- `GET /api/projects?category=AI` возвращает только опубликованные AI-проекты.
- Неопубликованный `draft-automation-scenario` не возвращается публичным API.
- Создание проекта с занятым `slug` возвращает `409 Conflict`.
- Upload принимает `jpeg`, `png`, `webp`, безопасный `svg` до 5 MB.
- Upload отклоняет файл неверного типа, слишком большой файл и SVG с активным содержимым.

## Public Web

- `/` открывает страницу "О себе" с demo content.
- `/ai-projects` показывает опубликованные AI-проекты.
- `/other-projects` показывает опубликованные OTHER-проекты.
- Карточка проекта открывает `/projects/[slug]`.
- `/projects/unknown-project` показывает 404.
- Пустое состояние не ломает layout, если в категории нет опубликованных проектов.
- Длинные названия и описания не выходят за пределы карточек.
- Страницы проверены на desktop, tablet и mobile ширинах.
- Для `/`, `/ai-projects`, `/other-projects`, `/projects/[slug]` есть title и description.

## Admin Web

- `/admin` перенаправляет неавторизованного пользователя на `/admin/login`.
- Login с development-учетными данными открывает `/admin/about`.
- Logout очищает сессию и возвращает на login.
- Удаленный или истекший token приводит к выходу из админки при следующем API-запросе.
- Страница "О себе" загружает HTML, сохраняет изменения и показывает состояние успеха/ошибки.
- Разделы "Проекты с ИИ" и "Другие проекты" показывают таблицы своих категорий.
- Создание проекта работает, опубликованный проект появляется на публичной странице.
- Редактирование проекта обновляет публичную страницу после сохранения.
- Удаление проекта требует подтверждения.
- Upload изображения подставляет URL в форму проекта.
- Ошибки backend validation отображаются в форме.

## Build and Delivery

- `dotnet build backend/Portfolio.slnx` проходит.
- `npm run build` проходит в `public-web`.
- `npm run build` проходит в `admin-web`.
- `docker compose up --build -d` поднимает `postgres`, `seaweedfs`, `api`, `public-web`, `admin-web`.
- В репозитории нет реальных секретов; `.env.example` содержит только development placeholders.
- README описывает назначение, стек, запуск, env variables, маршруты, Swagger и известные ограничения.
