# Deploy через GitHub Actions и GHCR

Деплой состоит из двух job:

1. `build_and_push_images` собирает `api`, `public-web`, `admin-web` и публикует образы в `ghcr.io`.
2. `deploy_to_server` подключается к серверу по SSH, подтягивает новые образы, полностью перезапускает контейнеры и чистит старые Docker images/build cache.

Workflow запускается только при push в ветку `main`.

## 1. GitHub repository settings

Откройте репозиторий в GitHub:

```text
Settings -> Secrets and variables -> Actions
```

Добавьте secrets:

```text
SERVER_HOST=<server-ip-or-domain>
SERVER_USER=<ssh-user>
SERVER_PORT=22
SERVER_SSH_KEY=<private-ssh-key>
DEPLOY_PATH=/opt/goulash1
GHCR_USERNAME=<github-username-or-org>
GHCR_PAT=<github-token-with-write-packages>
```

`GHCR_PAT` используется для публикации образов в GHCR. Минимально ему нужны permissions:

```text
write:packages
read:packages
```

Если хотите разделить права, создайте отдельный read-only token для сервера и добавьте:

```text
GHCR_READ_TOKEN=<github-token-with-read-packages>
```

Если `GHCR_READ_TOKEN` не задан, серверный pull использует `GHCR_PAT`.

Опционально, если GHCR namespace отличается от owner репозитория, добавьте repository variable:

```text
GHCR_OWNER=<github-owner-or-org>
```

Это variable, не secret:

```text
Settings -> Secrets and variables -> Actions -> Variables
```

Если `GHCR_PAT` не задан, workflow fallback'ом попробует использовать встроенный `github.token`, но основной ожидаемый вариант для этого проекта - PAT.

## 2. Сервер

На сервере должны быть установлены Docker и Docker Compose plugin:

```bash
sudo apt update
sudo apt install -y ca-certificates curl git
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo tee /etc/apt/keyrings/docker.asc > /dev/null
sudo chmod a+r /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

Добавьте deploy-пользователя в группу Docker:

```bash
sudo usermod -aG docker <ssh-user>
```

После этого нужно перелогиниться под этим пользователем.

Создайте директорию деплоя:

```bash
sudo mkdir -p /opt/goulash1
sudo chown -R <ssh-user>:<ssh-user> /opt/goulash1
```

Создайте production env:

```bash
nano /opt/goulash1/.env
```

Минимальный пример:

```env
GHCR_OWNER=your-github-owner-lowercase
IMAGE_TAG=latest

PUBLIC_SITE_ORIGIN=https://example.com

API_URL=http://api:5000
NEXT_PUBLIC_BASE_PATH=/portfolio
NEXT_PUBLIC_API_URL=/portfolio/api
# Admin API paths already include /api, so keep only the shared prefix here.
VITE_API_BASE_URL=/portfolio

POSTGRES_DB=portfolio
POSTGRES_USER=portfolio
POSTGRES_PASSWORD=replace-with-strong-postgres-password

ASPNETCORE_ENVIRONMENT=Production
Database__ApplyMigrationsOnStartup=true

SEED_ADMIN_LOGIN=admin
SEED_ADMIN_PASSWORD=replace-with-strong-admin-password

JWT_ISSUER=portfolio
JWT_AUDIENCE=portfolio-admin
JWT_SECRET=replace-with-a-long-random-secret-at-least-32-chars
JWT_LIFETIME_MINUTES=60

STORAGE_PUBLIC_BASE_URL=https://example.com/portfolio/files/portfolio
STORAGE_BUCKET_NAME=portfolio
STORAGE_REGION=us-east-1
STORAGE_ACCESS_KEY=
STORAGE_SECRET_KEY=
```

`JWT_SECRET` должен быть не короче 32 символов.

## 3. SSH key для GitHub Actions

На локальной машине создайте отдельный ключ для деплоя:

```bash
ssh-keygen -t ed25519 -C "github-actions-goulash1-deploy" -f ./goulash1_deploy_key
```

На сервере добавьте public key:

```bash
mkdir -p ~/.ssh
chmod 700 ~/.ssh
nano ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
```

Вставьте содержимое:

```bash
cat ./goulash1_deploy_key.pub
```

В GitHub secret `SERVER_SSH_KEY` вставьте private key:

```bash
cat ./goulash1_deploy_key
```

Проверьте доступ:

```bash
ssh -i ./goulash1_deploy_key <ssh-user>@<server-ip>
```

## 4. Nginx на хосте

Контейнеры публикуются только на localhost:

```text
127.0.0.1:3000 -> public-web
127.0.0.1:5173 -> admin-web
127.0.0.1:5000 -> api
127.0.0.1:8333 -> seaweedfs
```

Установите nginx:

```bash
sudo apt update
sudo apt install -y nginx
```

Создайте конфиг:

```bash
sudo nano /etc/nginx/sites-available/goulash1
```

Пример для одного домена, когда весь проект обслуживается под префиксом `/portfolio` (корень домена остается свободным для других проектов). Префикс задается переменной `NEXT_PUBLIC_BASE_PATH=/portfolio` и зашивается в образы при сборке:

```nginx
server {
    listen 80;
    server_name example.com www.example.com;

    client_max_body_size 10m;

    # Next.js with basePath redirects /portfolio/ -> /portfolio itself (308),
    # so /portfolio without a slash must reach the Next.js container as is.
    location = /portfolio {
        proxy_pass http://127.0.0.1:3000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /portfolio/api/ {
        proxy_pass http://127.0.0.1:5000/api/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /portfolio/files/ {
        proxy_pass http://127.0.0.1:8333/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /portfolio/admin/ {
        proxy_pass http://127.0.0.1:5173;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /portfolio/ {
        proxy_pass http://127.0.0.1:3000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Важно: `/portfolio/api/` и `/portfolio/files/` проксируются со слешем в конце `proxy_pass` (префикс срезается), а `/portfolio/` и `/portfolio/admin/` - без слеша (префикс сохраняется, Next.js и admin nginx обслуживают пути с basePath сами). Редирект `/portfolio/` на `/portfolio` выполняет сам Next.js - nginx не должен перехватывать этот маршрут.

Включите сайт:

```bash
sudo ln -s /etc/nginx/sites-available/goulash1 /etc/nginx/sites-enabled/goulash1
sudo nginx -t
sudo systemctl reload nginx
```

Если включен дефолтный сайт nginx и он мешает:

```bash
sudo rm /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx
```

## 5. SSL через certbot

Установите certbot:

```bash
sudo apt install -y certbot python3-certbot-nginx
```

Выпустите сертификат:

```bash
sudo certbot --nginx -d example.com -d www.example.com
```

Проверьте автообновление:

```bash
sudo certbot renew --dry-run
```

## 6. Первый деплой

Сделайте push в `main`.

После завершения workflow проверьте на сервере:

```bash
cd /opt/goulash1
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs api --tail=100
```

Публичные проверки:

```text
https://example.com/portfolio/
https://example.com/portfolio/admin/
https://example.com/portfolio/api/health
```

Не используйте `docker compose down -v` и `docker volume prune` для обычного деплоя: это удалит данные PostgreSQL и SeaweedFS.
