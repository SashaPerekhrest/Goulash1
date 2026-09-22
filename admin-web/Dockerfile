FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

ARG VITE_API_BASE_URL=http://localhost:5000
ARG VITE_ADMIN_BASE_PATH=/admin/
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_ADMIN_BASE_PATH=$VITE_ADMIN_BASE_PATH

RUN npm run build

FROM nginx:1.27-alpine AS runtime

# Serving path inside the container: <ADMIN_BASE_PATH>/admin, e.g. /portfolio/admin.
ARG ADMIN_BASE_PATH=/admin
ENV ADMIN_BASE_PATH=$ADMIN_BASE_PATH

COPY nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html/app

EXPOSE 5173
