# API-контракты

## 1. Общие сведения

Backend API реализуется на ASP.NET Core Web API.

Базовый URL для локальной разработки:

```text
http://localhost:5000
```

API делится на две группы:

- публичные методы для Next.js сайта;
- защищенные методы для административной панели.

Формат данных:

```text
application/json
```

Для загрузки файлов используется:

```text
multipart/form-data
```

## 2. Авторизация

Защищенные административные endpoints требуют авторизации.

Рекомендуемый вариант:

```text
Authorization: Bearer <accessToken>
```

## 3. Общие модели

### 3.1. ErrorResponse

```json
{
  "message": "Описание ошибки",
  "errors": {
    "fieldName": ["Ошибка валидации"]
  }
}
```

Поле `errors` может отсутствовать, если ошибка не связана с валидацией.

### 3.2. ProjectCategory

```text
AI
OTHER
```

## 4. Auth API

### 4.1. Вход администратора

```http
POST /api/auth/login
```

Request:

```json
{
  "login": "admin",
  "password": "password"
}
```

Response `200 OK`:

```json
{
  "accessToken": "jwt-token",
  "expiresAt": "2026-09-19T12:00:00Z",
  "user": {
    "id": "b9f0d6d4-2d52-4a13-8fb1-6e8bbf189f2d",
    "login": "admin"
  }
}
```

Ошибки:

- `400 Bad Request` - некорректные данные;
- `401 Unauthorized` - неверный логин или пароль.

### 4.2. Получение текущего пользователя

```http
GET /api/auth/me
```

Headers:

```text
Authorization: Bearer <accessToken>
```

Response `200 OK`:

```json
{
  "id": "b9f0d6d4-2d52-4a13-8fb1-6e8bbf189f2d",
  "login": "admin"
}
```

Ошибки:

- `401 Unauthorized`.

## 5. Page Content API

### 5.1. Получение публичного контента страницы

```http
GET /api/pages/{key}
```

Пример:

```http
GET /api/pages/about
```

Response `200 OK`:

```json
{
  "key": "about",
  "htmlContent": "<h1>О себе</h1><p>...</p>",
  "updatedAt": "2026-09-19T12:00:00Z"
}
```

Ошибки:

- `404 Not Found` - страница не найдена.

### 5.2. Получение контента страницы для админки

```http
GET /api/admin/pages/{key}
```

Headers:

```text
Authorization: Bearer <accessToken>
```

Response `200 OK`:

```json
{
  "id": "fa024e71-7a81-4358-b8d8-020f554706d2",
  "key": "about",
  "htmlContent": "<h1>О себе</h1><p>...</p>",
  "createdAt": "2026-09-19T12:00:00Z",
  "updatedAt": "2026-09-19T12:00:00Z"
}
```

Ошибки:

- `401 Unauthorized`;
- `404 Not Found`.

### 5.3. Обновление контента страницы

```http
PUT /api/admin/pages/{key}
```

Headers:

```text
Authorization: Bearer <accessToken>
```

Request:

```json
{
  "htmlContent": "<h1>О себе</h1><p>Новый контент</p>"
}
```

Response `200 OK`:

```json
{
  "id": "fa024e71-7a81-4358-b8d8-020f554706d2",
  "key": "about",
  "htmlContent": "<h1>О себе</h1><p>Новый контент</p>",
  "createdAt": "2026-09-19T12:00:00Z",
  "updatedAt": "2026-09-19T12:30:00Z"
}
```

Ошибки:

- `400 Bad Request`;
- `401 Unauthorized`;
- `404 Not Found` - страница не найдена.

## 6. Public Projects API

### 6.1. Получение списка проектов

```http
GET /api/projects?category=AI
```

Query params:

- `category` - `AI` или `OTHER`, необязательный параметр.

Публичный список возвращает только проекты с `isPublished: true`.

Порядок выдачи:

- новые проекты выше старых;
- сортировка по `createdAt desc`.

Response `200 OK`:

```json
[
  {
    "id": "1f1f6d9d-3e22-4cc4-a5fc-180ed1dd278e",
    "title": "AI Support Bot",
    "slug": "ai-support-bot",
    "shortDescription": "Бот поддержки с использованием ИИ.",
    "imageUrl": "http://localhost:8333/portfolio/projects/ai-support-bot.png",
    "category": "AI",
    "isPublished": true,
    "createdAt": "2026-09-19T12:00:00Z",
    "updatedAt": "2026-09-19T12:00:00Z"
  }
]
```

### 6.2. Получение проекта по slug

```http
GET /api/projects/{slug}
```

Пример:

```http
GET /api/projects/ai-support-bot
```

Response `200 OK`:

```json
{
  "id": "1f1f6d9d-3e22-4cc4-a5fc-180ed1dd278e",
  "title": "AI Support Bot",
  "slug": "ai-support-bot",
  "shortDescription": "Бот поддержки с использованием ИИ.",
  "imageUrl": "http://localhost:8333/portfolio/projects/ai-support-bot.png",
  "htmlContent": "<h1>AI Support Bot</h1><p>...</p>",
  "category": "AI",
  "isPublished": true,
  "createdAt": "2026-09-19T12:00:00Z",
  "updatedAt": "2026-09-19T12:00:00Z"
}
```

Ошибки:

- `404 Not Found` - проект не найден или не опубликован.

## 7. Admin Projects API

### 7.1. Получение списка проектов для админки

```http
GET /api/admin/projects?category=AI
```

Headers:

```text
Authorization: Bearer <accessToken>
```

Query params:

- `category` - `AI` или `OTHER`, необязательный параметр.

Административный список возвращает опубликованные и неопубликованные проекты.

Порядок выдачи:

- новые проекты выше старых;
- сортировка по `createdAt desc`.

Response `200 OK`:

```json
[
  {
    "id": "1f1f6d9d-3e22-4cc4-a5fc-180ed1dd278e",
    "title": "AI Support Bot",
    "slug": "ai-support-bot",
    "shortDescription": "Бот поддержки с использованием ИИ.",
    "imageUrl": "http://localhost:8333/portfolio/projects/ai-support-bot.png",
    "htmlContent": "<h1>AI Support Bot</h1><p>...</p>",
    "category": "AI",
    "isPublished": true,
    "createdAt": "2026-09-19T12:00:00Z",
    "updatedAt": "2026-09-19T12:00:00Z"
  }
]
```

Ошибки:

- `401 Unauthorized`.

### 7.2. Получение проекта по id

```http
GET /api/admin/projects/{id}
```

Headers:

```text
Authorization: Bearer <accessToken>
```

Response `200 OK`:

```json
{
  "id": "1f1f6d9d-3e22-4cc4-a5fc-180ed1dd278e",
  "title": "AI Support Bot",
  "slug": "ai-support-bot",
  "shortDescription": "Бот поддержки с использованием ИИ.",
  "imageUrl": "http://localhost:8333/portfolio/projects/ai-support-bot.png",
  "htmlContent": "<h1>AI Support Bot</h1><p>...</p>",
  "category": "AI",
  "isPublished": true,
  "createdAt": "2026-09-19T12:00:00Z",
  "updatedAt": "2026-09-19T12:00:00Z"
}
```

Ошибки:

- `401 Unauthorized`;
- `404 Not Found`.

### 7.3. Создание проекта

```http
POST /api/admin/projects
```

Headers:

```text
Authorization: Bearer <accessToken>
```

Request:

```json
{
  "title": "AI Support Bot",
  "slug": "ai-support-bot",
  "shortDescription": "Бот поддержки с использованием ИИ.",
  "imageUrl": "http://localhost:8333/portfolio/projects/ai-support-bot.png",
  "htmlContent": "<h1>AI Support Bot</h1><p>...</p>",
  "category": "AI",
  "isPublished": true
}
```

Response `201 Created`:

```json
{
  "id": "1f1f6d9d-3e22-4cc4-a5fc-180ed1dd278e",
  "title": "AI Support Bot",
  "slug": "ai-support-bot",
  "shortDescription": "Бот поддержки с использованием ИИ.",
  "imageUrl": "http://localhost:8333/portfolio/projects/ai-support-bot.png",
  "htmlContent": "<h1>AI Support Bot</h1><p>...</p>",
  "category": "AI",
  "isPublished": true,
  "createdAt": "2026-09-19T12:00:00Z",
  "updatedAt": "2026-09-19T12:00:00Z"
}
```

Ошибки:

- `400 Bad Request`;
- `401 Unauthorized`;
- `409 Conflict` - проект с таким slug уже существует.

### 7.4. Обновление проекта

```http
PUT /api/admin/projects/{id}
```

Headers:

```text
Authorization: Bearer <accessToken>
```

Request:

```json
{
  "title": "AI Support Bot",
  "slug": "ai-support-bot",
  "shortDescription": "Обновленное краткое описание.",
  "imageUrl": "http://localhost:8333/portfolio/projects/ai-support-bot-v2.png",
  "htmlContent": "<h1>AI Support Bot</h1><p>Обновленный контент</p>",
  "category": "AI",
  "isPublished": true
}
```

Response `200 OK`:

```json
{
  "id": "1f1f6d9d-3e22-4cc4-a5fc-180ed1dd278e",
  "title": "AI Support Bot",
  "slug": "ai-support-bot",
  "shortDescription": "Обновленное краткое описание.",
  "imageUrl": "http://localhost:8333/portfolio/projects/ai-support-bot-v2.png",
  "htmlContent": "<h1>AI Support Bot</h1><p>Обновленный контент</p>",
  "category": "AI",
  "isPublished": true,
  "createdAt": "2026-09-19T12:00:00Z",
  "updatedAt": "2026-09-19T12:30:00Z"
}
```

Ошибки:

- `400 Bad Request`;
- `401 Unauthorized`;
- `404 Not Found`;
- `409 Conflict` - проект с таким slug уже существует.

### 7.5. Удаление проекта

```http
DELETE /api/admin/projects/{id}
```

Headers:

```text
Authorization: Bearer <accessToken>
```

Response:

```http
204 No Content
```

Ошибки:

- `401 Unauthorized`;
- `404 Not Found`.

## 8. Files API

В локальной разработке примеры URL используют публичный S3 endpoint SeaweedFS:

```text
http://localhost:8333/portfolio/<object-key>
```

В production значение `url` может быть либо публичным URL S3-compatible хранилища, либо backend proxy URL, если bucket остается закрытым.

### 8.1. Загрузка файла

```http
POST /api/admin/files
```

Headers:

```text
Authorization: Bearer <accessToken>
Content-Type: multipart/form-data
```

Form fields:

- `file` - загружаемый файл;
- `folder` - папка назначения, например `projects`, необязательное поле.

Response `201 Created`:

```json
{
  "fileName": "ai-support-bot.png",
  "contentType": "image/png",
  "size": 245120,
  "url": "http://localhost:8333/portfolio/projects/ai-support-bot.png"
}
```

Ошибки:

- `400 Bad Request` - файл отсутствует, неверный тип или превышен размер;
- `401 Unauthorized`.

## 9. Рекомендуемые ограничения валидации

### 9.1. Project

- `title` - обязательное поле, максимум 200 символов;
- `slug` - обязательное поле, максимум 200 символов, только латиница, цифры и дефисы;
- `shortDescription` - обязательное поле, максимум 500 символов;
- `imageUrl` - необязательное поле, максимум 1000 символов;
- `htmlContent` - обязательное поле;
- `category` - обязательное поле, только `AI` или `OTHER`.
- `isPublished` - обязательное поле, boolean.

### 9.2. PageContent

- `htmlContent` - обязательное поле.

### 9.3. File upload

- допустимые типы: `image/jpeg`, `image/png`, `image/webp`, `image/svg+xml`;
- максимальный размер файла: 5 MB.

