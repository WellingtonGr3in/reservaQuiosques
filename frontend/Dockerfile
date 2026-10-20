# Imagem do frontend (React + Vite). Também em duas etapas:
#   1. "build": usa o Node para instalar as dependências e gerar os arquivos estáticos (pasta dist/);
#   2. "web": serve esses arquivos com o nginx, que também repassa /api para o container da API.

# ---------- 1. Compilação ----------
FROM node:24-alpine AS build
WORKDIR /app

# Instala as dependências primeiro (fica em cache enquanto package*.json não mudarem).
COPY package.json package-lock.json ./
RUN npm ci

# Variável lida pelo Vite no build: mostra o botão "Simular pagamento" (o gateway Pix é simulado).
ARG VITE_SIMULAR_PAGAMENTO=false
ENV VITE_SIMULAR_PAGAMENTO=$VITE_SIMULAR_PAGAMENTO

COPY . .
RUN npm run build

# ---------- 2. Servidor web ----------
FROM nginx:1.27-alpine AS web
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
