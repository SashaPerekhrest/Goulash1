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

PUBLIC_SITE_ORIGIN=https://portfolio.example.com

API_URL=http://api:5000
# Empty means that the public Next.js app is served from the subdomain root.
NEXT_PUBLIC_BASE_PATH=
NEXT_PUBLIC_API_URL=/api
ADMIN_BASE_PATH=/admin

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

STORAGE_PUBLIC_BASE_URL=https://portfolio.example.com/files/portfolio
STORAGE_BUCKET_NAME=portfolio
STORAGE_REGION=us-east-1
STORAGE_ACCESS_KEY=
STORAGE_SECRET_KEY=
```

`JWT_SECRET` должен быть не короче 32 символов.
`VITE_API_BASE_URL` задается как пустой build argument в GitHub Actions, чтобы админка отправляла API-запросы на тот же поддомен. Это значение не нужно задавать в серверном `.env`.

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

Создайте отдельный конфиг для поддомена:

```bash
sudo nano /etc/nginx/sites-available/portfolio.example.com
```

Замените `portfolio.example.com` на точное имя вашего поддомена:

```nginx
server {
    listen 80;
    server_name portfolio.example.com;

    client_max_body_size 10m;

    location = /admin {
        return 308 /admin/;
    }

    location ^~ /admin/ {
        proxy_pass http://127.0.0.1:5173;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Keep /api in the request path sent to ASP.NET Core.
    location ^~ /api/ {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Strip /files/ so /files/portfolio/image.png becomes /portfolio/image.png
    # on SeaweedFS.
    location ^~ /files/ {
        proxy_pass http://127.0.0.1:8333/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location / {
        proxy_pass http://127.0.0.1:3000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Здесь `proxy_pass` для API указан без завершающего URI slash, поэтому nginx сохраняет `/api/...`. Для файлов завершающий slash срезает `/files/`: SeaweedFS получает исходный путь bucket, например `/portfolio/image.png`. Админский nginx получает `/admin/...` без изменения пути. Остальные маршруты идут в Next.js.

Включите сайт:

```bash
sudo ln -s /etc/nginx/sites-available/portfolio.example.com /etc/nginx/sites-enabled/portfolio.example.com
sudo nginx -t
sudo systemctl reload nginx
```

## 5. SSL через certbot

DNS A-запись поддомена должна уже указывать на сервер, а входящие порты 80 и 443 должны быть доступны. Установите Certbot для nginx согласно текущей официальной инструкции; для Ubuntu через Snap:

```bash
sudo snap install --classic certbot
sudo ln -s /snap/bin/certbot /usr/local/bin/certbot
```

Выпустите сертификат для точного имени поддомена и включите HTTPS:

```bash
sudo certbot --nginx -d portfolio.example.com
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

Проверки после переключения:

```text
https://portfolio.example.com/
https://portfolio.example.com/admin/
https://portfolio.example.com/api/projects?category=AI
```

У backend health endpoint находится по `/health` (внутри сервера: `http://127.0.0.1:5000/health`), а не `/api/health`.

Новые загруженные изображения будут иметь URL поддомена. Уже сохраненные в PostgreSQL полные URL с прежним доменом или `/portfolio/files/` сами не меняются: до выключения старого маршрута нужно перенести их адреса в колонках проектов и HTML-контенте страницы `about`, либо сохранить совместимость со старым URL в nginx.

Не используйте `docker compose down -v` и `docker volume prune` для обычного деплоя: это удалит данные PostgreSQL и SeaweedFS.
