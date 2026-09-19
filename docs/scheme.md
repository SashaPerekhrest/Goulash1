# Схема взаимодействия модулей

## 1. Общая схема

```text
┌────────────────────┐
│    Пользователь    │
└─────────┬──────────┘
          │
          │ Открывает публичный сайт
          ▼
┌────────────────────┐
│    Public Web      │
│    Next.js         │
└─────────┬──────────┘
          │
          │ GET /api/pages/about
          │ GET /api/projects
          │ GET /api/projects/{slug}
          ▼
┌────────────────────┐
│    Backend API     │
│    .NET 8          │
└──────┬────────┬────┘
       │        │
       │        │ Получение файлов
       │        ▼
       │   ┌────────────────────┐
       │   │     SeaweedFS      │
       │   │   S3-compatible    │
       │   └────────────────────┘
       │
       │ Чтение данных
       ▼
┌────────────────────┐
│    PostgreSQL      │
└────────────────────┘
```

```text
┌────────────────────┐
│   Администратор    │
└─────────┬──────────┘
          │
          │ Открывает /admin
          ▼
┌────────────────────┐
│     Admin Web      │
│ React / Vite / MUI │
└─────────┬──────────┘
          │
          │ POST /api/auth/login
          │ PUT /api/admin/pages/about
          │ CRUD /api/admin/projects
          │ POST /api/admin/files
          ▼
┌────────────────────┐
│    Backend API     │
│    .NET 8          │
└──────┬────────┬────┘
       │        │
       │        │ Загрузка файлов
       │        ▼
       │   ┌────────────────────┐
       │   │     SeaweedFS      │
       │   │   S3-compatible    │
       │   └────────────────────┘
       │
       │ Запись и чтение данных
       ▼
┌────────────────────┐
│    PostgreSQL      │
└────────────────────┘
```

## 2. Схема модулей

```text
portfolio-project/
  backend/
    src/
      Portfolio.Api
      Portfolio.Application
      Portfolio.Domain
      Portfolio.Infrastructure

  public-web/
    Next.js application

  admin-web/
    React + Vite application

  docker-compose.yml
```

## 3. Взаимодействие публичного сайта

### 3.1. Открытие страницы "О себе"

```text
Visitor
  │
  ▼
Public Web: /
  │
  │ GET /api/pages/about
  ▼
Backend API
  │
  │ SELECT * FROM page_contents WHERE key = 'about'
  ▼
PostgreSQL
  │
  ▼
Backend API returns htmlContent
  │
  ▼
Public Web renders HTML content
```

### 3.2. Открытие списка проектов с ИИ

```text
Visitor
  │
  ▼
Public Web: /ai-projects
  │
  │ GET /api/projects?category=AI
  ▼
Backend API
  │
  │ SELECT projects WHERE category = 'AI' AND is_published = true
  ▼
PostgreSQL
  │
  ▼
Backend API returns project cards data
  │
  ▼
Public Web renders project cards
```

### 3.3. Открытие списка других проектов

```text
Visitor
  │
  ▼
Public Web: /other-projects
  │
  │ GET /api/projects?category=OTHER
  ▼
Backend API
  │
  │ SELECT projects WHERE category = 'OTHER' AND is_published = true
  ▼
PostgreSQL
  │
  ▼
Backend API returns project cards data
  │
  ▼
Public Web renders project cards
```

### 3.4. Открытие страницы проекта

```text
Visitor
  │
  ▼
Public Web: /projects/{slug}
  │
  │ GET /api/projects/{slug}
  ▼
Backend API
  │
  │ SELECT project WHERE slug = {slug} AND is_published = true
  ▼
PostgreSQL
  │
  ▼
Backend API returns full project data
  │
  ▼
Public Web renders project page
```

## 4. Взаимодействие админки

### 4.1. Авторизация администратора

```text
Administrator
  │
  ▼
Admin Web: /admin/login
  │
  │ POST /api/auth/login
  │ { login, password }
  ▼
Backend API
  │
  │ SELECT admin user by login
  ▼
PostgreSQL
  │
  ▼
Backend API checks password hash
  │
  ▼
Backend API returns accessToken
  │
  ▼
Admin Web stores token
```

### 4.2. Редактирование страницы "О себе"

```text
Administrator
  │
  ▼
Admin Web: /admin/about
  │
  │ GET /api/admin/pages/about
  ▼
Backend API
  │
  │ SELECT page_content WHERE key = 'about'
  ▼
PostgreSQL
  │
  ▼
Admin Web displays TinyMCE editor
  │
  │ User edits HTML
  │
  │ PUT /api/admin/pages/about
  │ { htmlContent }
  ▼
Backend API
  │
  │ UPDATE page_contents
  ▼
PostgreSQL
```

### 4.3. Создание проекта

```text
Administrator
  │
  ▼
Admin Web: /admin/ai-projects or /admin/other-projects
  │
  │ Click "Create"
  ▼
Project modal opens
  │
  │ User fills title, slug, shortDescription, htmlContent, isPublished
  │
  │ Optional: uploads image
  ▼
Admin Web
  │
  │ POST /api/admin/files
  ▼
Backend API
  │
  │ Upload file to SeaweedFS
  ▼
SeaweedFS
  │
  ▼
Backend API returns imageUrl
  │
  ▼
Admin Web inserts imageUrl into form
  │
  │ POST /api/admin/projects
  ▼
Backend API
  │
  │ INSERT INTO projects
  ▼
PostgreSQL
```

### 4.4. Редактирование проекта

```text
Administrator
  │
  ▼
Admin Web project table
  │
  │ Click "Edit"
  ▼
Admin Web
  │
  │ GET /api/admin/projects/{id}
  ▼
Backend API
  │
  │ SELECT project by id
  ▼
PostgreSQL
  │
  ▼
Project modal opens with existing data
  │
  │ User edits fields
  │
  │ PUT /api/admin/projects/{id}
  ▼
Backend API
  │
  │ UPDATE projects
  ▼
PostgreSQL
```

### 4.5. Удаление проекта

```text
Administrator
  │
  ▼
Admin Web project table
  │
  │ Click "Delete"
  ▼
Confirmation
  │
  │ DELETE /api/admin/projects/{id}
  ▼
Backend API
  │
  │ DELETE FROM projects WHERE id = {id}
  ▼
PostgreSQL
```

## 5. Схема данных

```text
┌────────────────────────────┐
│        admin_users         │
├────────────────────────────┤
│ id uuid PK                 │
│ login varchar UNIQUE       │
│ password_hash varchar      │
│ created_at timestamp       │
│ updated_at timestamp       │
└────────────────────────────┘

┌────────────────────────────┐
│       page_contents        │
├────────────────────────────┤
│ id uuid PK                 │
│ key varchar UNIQUE         │
│ html_content text          │
│ created_at timestamp       │
│ updated_at timestamp       │
└────────────────────────────┘

┌────────────────────────────┐
│          projects          │
├────────────────────────────┤
│ id uuid PK                 │
│ title varchar(200)         │
│ slug varchar(200) UNIQUE   │
│ short_description varchar  │
│ image_url varchar          │
│ html_content text          │
│ category varchar(20)       │
│ is_published boolean       │
│ created_at timestamp       │
│ updated_at timestamp       │
└────────────────────────────┘
```

## 6. Docker Compose схема

```text
┌─────────────────────────────────────────────────────────┐
│                    docker-compose                       │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  public-web  ───────┐                                  │
│                     │                                  │
│  admin-web   ───────┼──────► backend-api               │
│                     │             │                    │
│                     │             ├────► postgres       │
│                     │             │                    │
│                     │             └────► seaweedfs      │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

Рекомендуемые сервисы:

```text
postgres
seaweedfs
api
public-web
admin-web
```

## 7. Основные пользовательские сценарии

### 7.1. Посетитель смотрит портфолио

```text
Открывает сайт
  -> читает "О себе"
  -> переходит в "Проекты с ИИ"
  -> открывает карточку проекта
  -> читает полное описание проекта
  -> возвращается к списку или переходит в "Другие проекты"
```

### 7.2. Администратор обновляет контент

```text
Открывает /admin
  -> входит по логину и паролю
  -> открывает нужный раздел
  -> меняет контент через TinyMCE
  -> сохраняет изменения
  -> изменения появляются на публичном сайте
```

### 7.3. Администратор добавляет проект

```text
Открывает раздел проектов
  -> нажимает "Создать"
  -> заполняет название, slug и краткое описание
  -> при необходимости загружает изображение
  -> пишет полное описание через TinyMCE
  -> выбирает статус публикации
  -> сохраняет проект
  -> опубликованный проект появляется на публичной странице
```

