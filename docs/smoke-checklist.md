# Smoke checklist

Чеклист используется в финальном спринте перед сдачей проекта. Он покрывает основные сценарии из ТЗ и `docs/sprints/sprint-11-polish-security-and-delivery.md`.

В production весь проект может обслуживаться под URL-префиксом (`NEXT_PUBLIC_BASE_PATH=/portfolio`); в примерах ниже используется префикс `/portfolio`. Для локальной разработки без префикса просто уберите `/portfolio` из путей.

## Backend API

- `GET /portfolio/api/health` через host nginx возвращает `200 OK`.
- Swagger доступен по `http://localhost:5000/swagger` в `Development`.
- `POST /portfolio/api/auth/login` возвращает JWT для development-администратора.
- `GET /portfolio/api/auth/me` без токена возвращает `401`.
- `GET /portfolio/api/auth/me` с валидным токеном возвращает текущего пользователя.
- `GET /portfolio/api/admin/pages/about` без токена возвращает `401`.
- `GET /portfolio/api/admin/projects` без токена возвращает `401`.
- `GET /portfolio/api/projects?category=AI` возвращает только опубликованные AI-проекты.
- Неопубликованный `draft-automation-scenario` не возвращается публичным API.
- Создание проекта с занятым `slug` возвращает `409 Conflict`.
- Upload принимает `jpeg`, `png`, `webp`, безопасный `svg` до 5 MB.
- Upload отклоняет файл неверного типа, слишком большой файл и SVG с активным содержимым.
- URL загруженных файлов начинаются с `/portfolio/files/`.

## Public Web

- `/portfolio/` открывает страницу "О себе" с demo content.
- `/portfolio/ai-projects` показывает опубликованные AI-проекты.
- `/portfolio/other-projects` показывает опубликованные OTHER-проекты.
- Карточка проекта открывает `/portfolio/projects/[slug]`.
- `/portfolio/projects/unknown-project` показывает 404.
- Пустое состояние не ломает layout, если в категории нет опубликованных проектов.
- Длинные названия и описания не выходят за пределы карточек.
- Страницы проверены на desktop, tablet и mobile ширинах.
- Для `/portfolio/`, `/portfolio/ai-projects`, `/portfolio/other-projects`, `/portfolio/projects/[slug]` есть title и description.
- Статика Next.js (`_next/*`) загружается с префиксом, в консоли браузера нет 404.

## Admin Web

- `/portfolio/admin` перенаправляет неавторизованного пользователя на `/portfolio/admin/login`.
- Login с development-учетными данными открывает `/portfolio/admin/about`.
- Logout очищает сессию и возвращает на login.
- Удаленный или истекший token приводит к выходу из админки при следующем API-запросе.
- Страница "О себе" загружает HTML, сохраняет изменения и показывает состояние успеха/ошибки.
- Разделы "Проекты с ИИ" и "Другие проекты" показывают таблицы своих категорий.
- Создание проекта работает, опубликованный проект появляется на публичной странице.
- Редактирование проекта обновляет публичную страницу после сохранения.
- Удаление проекта требует подтверждения.
- Upload изображения подставляет URL в форму проекта.
- Ошибки backend validation отображаются в форме.
- TinyMCE загружается под префиксом: скины, иконки и загрузка изображений в редакторе работают.

## Build and Delivery

- `dotnet build backend/Portfolio.slnx` проходит.
- `npm run build` проходит в `public-web`.
- `npm run build` проходит в `admin-web`.
- `docker compose up --build -d` поднимает `postgres`, `seaweedfs`, `api`, `public-web`, `admin-web`.
- В репозитории нет реальных секретов; `.env.example` содержит только development placeholders.
- README описывает назначение, стек, запуск, env variables, маршруты, Swagger и известные ограничения.
