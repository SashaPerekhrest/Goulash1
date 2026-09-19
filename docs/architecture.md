# Архитектура проекта

## 1. Общий обзор

Проект представляет собой портфолио с управляемым контентом.

Система состоит из следующих основных частей:

- публичный сайт на Next.js;
- административная панель на React + Vite;
- backend API на .NET 8;
- база данных PostgreSQL;
- файловое хранилище SeaweedFS;
- инфраструктура Docker Compose.

Основной принцип архитектуры: публичная часть и админка являются отдельными frontend-приложениями, которые работают с единым backend API.

## 2. Модули системы

### 2.1. Public Web

Публичный сайт.

Технологии:

- Next.js;
- React;
- TypeScript.

Ответственность:

- отображение страницы "О себе";
- отображение списка проектов с ИИ;
- отображение списка других проектов;
- отображение детальной страницы проекта;
- SEO;
- адаптивная верстка;
- получение публичных данных из backend API.

Ключевые маршруты:

```text
/
/ai-projects
/other-projects
/projects/[slug]
```

### 2.2. Admin Web

Административная панель.

Технологии:

- React;
- Vite;
- TypeScript;
- MUI;
- React Router;
- TanStack Query;
- React Hook Form;
- TinyMCE.

Ответственность:

- авторизация администратора;
- управление HTML-контентом страницы "О себе";
- управление проектами категории `AI`;
- управление проектами категории `OTHER`;
- загрузка изображений;
- работа с TinyMCE;
- отправка административных запросов в backend API.

Ключевые маршруты:

```text
/admin/login
/admin/about
/admin/ai-projects
/admin/other-projects
```

### 2.3. Backend API

Серверная часть.

Технологии:

- .NET 8;
- ASP.NET Core Web API;
- Entity Framework Core;
- PostgreSQL provider;
- Swagger / OpenAPI;
- S3-compatible клиент для SeaweedFS;
- JWT Bearer авторизация.

Ответственность:

- предоставление публичных API для сайта;
- предоставление защищенных API для админки;
- авторизация администратора;
- хранение и получение HTML-контента;
- CRUD проектов;
- загрузка файлов в SeaweedFS;
- сохранение ссылок на файлы;
- работа с PostgreSQL через EF Core.

### 2.4. PostgreSQL

Реляционная база данных.

Ответственность:

- хранение пользователей админки;
- хранение HTML-контента страницы "О себе";
- хранение проектов;
- хранение метаданных проектов;
- хранение ссылок на изображения.

Основные таблицы:

- `admin_users`;
- `page_contents`;
- `projects`.

### 2.5. SeaweedFS

S3-compatible файловое хранилище.

Ответственность:

- хранение изображений проектов;
- хранение файлов, загруженных через админку;
- предоставление URL для отображения файлов на публичном сайте.

Backend работает с SeaweedFS через S3-compatible API. Для локального Docker Compose окружения используется endpoint:

```text
http://seaweedfs:8333
```

Рекомендуемый bucket:

```text
portfolio
```

Пример структуры:

```text
portfolio/
  projects/
    ai-support-bot.png
    crm-system.webp
  content/
    about-image.png
```

## 3. Архитектурная схема

```text
                ┌──────────────────────┐
                │      Visitor         │
                └──────────┬───────────┘
                           │
                           ▼
                ┌──────────────────────┐
                │   Public Web         │
                │   Next.js            │
                └──────────┬───────────┘
                           │
                           │ Public API
                           ▼
┌────────────────────────────────────────────────┐
│                 Backend API                    │
│          .NET 8 / ASP.NET Core                 │
└──────────┬───────────────────────────┬─────────┘
           │                           │
           ▼                           ▼
┌──────────────────────┐    ┌──────────────────────┐
│      PostgreSQL      │    │        SeaweedFS         │
│      Database        │    │    S3 Storage        │
└──────────────────────┘    └──────────────────────┘


                ┌──────────────────────┐
                │    Administrator     │
                └──────────┬───────────┘
                           │
                           ▼
                ┌──────────────────────┐
                │      Admin Web       │
                │ React / Vite / MUI   │
                └──────────┬───────────┘
                           │
                           │ Protected API
                           ▼
┌────────────────────────────────────────────────┐
│                 Backend API                    │
│          .NET 8 / ASP.NET Core                 │
└────────────────────────────────────────────────┘
```

## 4. Слои backend

Рекомендуемая структура backend:

```text
backend/
  src/
    Portfolio.Api/
      Controllers/
      Middlewares/
      Program.cs
      appsettings.json
    Portfolio.Application/
      DTOs/
      Interfaces/
      Services/
      Validators/
    Portfolio.Domain/
      Entities/
      Enums/
    Portfolio.Infrastructure/
      Data/
      Migrations/
      Repositories/
      Storage/
```

### 4.1. Portfolio.Api

Слой HTTP API.

Содержит:

- controllers;
- настройки приложения;
- middleware;
- конфигурацию Swagger;
- конфигурацию CORS;
- подключение авторизации.

### 4.2. Portfolio.Application

Слой бизнес-логики.

Содержит:

- DTO;
- сервисы;
- интерфейсы репозиториев;
- интерфейсы файлового хранилища;
- валидацию.

### 4.3. Portfolio.Domain

Доменный слой.

Содержит:

- сущности;
- enum категорий проектов;
- базовые доменные правила.

### 4.4. Portfolio.Infrastructure

Инфраструктурный слой.

Содержит:

- EF Core DbContext;
- миграции;
- реализации репозиториев;
- интеграцию с PostgreSQL;
- интеграцию с SeaweedFS.

## 5. Структура frontend-приложений

### 5.1. Public Web

Рекомендуемая структура:

```text
public-web/
  app/
    page.tsx
    ai-projects/
      page.tsx
    other-projects/
      page.tsx
    projects/
      [slug]/
        page.tsx
  components/
    Header.tsx
    Footer.tsx
    ProjectCard.tsx
  lib/
    api.ts
    types.ts
  styles/
```

### 5.2. Admin Web

Рекомендуемая структура:

```text
admin-web/
  src/
    app/
      router.tsx
    components/
      Layout/
      ProjectModal/
      TinyEditor/
    features/
      auth/
      pages/
      projects/
      files/
    shared/
      api/
      types/
      ui/
    main.tsx
```

## 6. Данные и хранение

### 6.1. PageContent

Используется для страницы "О себе".

```text
id uuid primary key
key varchar unique not null
html_content text not null
created_at timestamp not null
updated_at timestamp not null
```

### 6.2. Project

Используется для проектов.

```text
id uuid primary key
title varchar(200) not null
slug varchar(200) unique not null
short_description varchar(500) not null
image_url varchar(1000)
html_content text not null
category varchar(20) not null
is_published boolean not null default false
created_at timestamp not null
updated_at timestamp not null
```

### 6.3. AdminUser

Используется для администратора.

```text
id uuid primary key
login varchar(100) unique not null
password_hash varchar not null
created_at timestamp not null
updated_at timestamp not null
```

## 7. Поток авторизации

1. Администратор открывает `/admin`.
2. Если токена нет, пользователь попадает на `/admin/login`.
3. Пользователь вводит логин и пароль.
4. Admin Web отправляет `POST /api/auth/login`.
5. Backend проверяет учетные данные.
6. Backend возвращает access token.
7. Admin Web сохраняет токен.
8. Последующие запросы отправляются с заголовком `Authorization`.
9. Backend проверяет токен на защищенных endpoints.

Публичные endpoints проектов возвращают только записи с `is_published = true`. Административные endpoints возвращают опубликованные и неопубликованные проекты, чтобы администратор мог работать с черновиками.

## 8. Поток загрузки изображения

1. Администратор открывает форму создания или редактирования проекта.
2. При необходимости выбирает изображение.
3. Admin Web отправляет файл на `POST /api/admin/files`.
4. Backend валидирует файл.
5. Backend загружает файл в SeaweedFS.
6. Backend получает публичный URL файла.
7. Backend возвращает URL в ответе.
8. Admin Web подставляет URL в форму проекта.
9. При сохранении проекта URL сохраняется в PostgreSQL.

## 9. SEO-подход

Next.js должен формировать metadata для страниц.

Для статичных страниц:

- title задается вручную;
- description задается вручную.

Для страниц проектов:

- title формируется из названия проекта;
- description формируется из краткого описания проекта;
- slug используется как человекочитаемый URL.

Пример:

```text
title: AI Support Bot | Портфолио
description: Бот поддержки с использованием ИИ.
url: /projects/ai-support-bot
```

## 10. Безопасность

Основные требования:

- закрыть административные endpoints авторизацией;
- хранить пароль только в виде хеша;
- валидировать входные данные;
- ограничить загрузку файлов по типу и размеру;
- учитывать XSS-риски при отображении HTML-контента;
- настроить CORS только для разрешенных frontend-адресов;
- не хранить секреты в репозитории.

## 11. Развертывание

Для локального запуска используется Docker Compose.

Сервисы:

- `api`;
- `postgres`;
- `seaweedfs`;
- `public-web`;
- `admin-web`.

Рекомендуемые порты для локальной разработки:

```text
Backend API:  http://localhost:5000
Swagger:      http://localhost:5000/swagger
Public Web:   http://localhost:3000
Admin Web:    http://localhost:5173
PostgreSQL:   localhost:5432
SeaweedFS S3: http://localhost:8333
SeaweedFS UI:  http://localhost:9333
```

В локальной разработке административный интерфейс открывается как `http://localhost:5173/admin`. В production он должен быть доступен по маршруту `/admin`, например через reverse proxy.

Для SeaweedFS рекомендуется использовать Docker image:

```text
chrislusf/seaweedfs
```

Backend должен иметь две настройки для работы с файлами:

- внутренний S3 endpoint для загрузки файлов из контейнера backend: `http://seaweedfs:8333`;
- публичный base URL для отображения файлов на сайте: `http://localhost:8333/portfolio` в локальной разработке или домен/CDN в production.

