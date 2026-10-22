# ADR-003: Execução com Docker Compose, sem publicação na internet — Aprovado

## Contexto

O projeto é acadêmico, sem orçamento, e o MVP precisa ser executado e apresentado em sala. A proposta inicial era publicar cada parte em um serviço PaaS gratuito (frontend, API e banco em provedores diferentes), mas isso traria problemas para o objetivo do trabalho:

- a API "dorme" no plano gratuito, e a primeira requisição da apresentação ficaria lenta;
- seriam três contas, três configurações e três pontos de falha fora do controle da equipe;
- o gateway Pix é simulado no MVP (ADR-004), então não há notificação de pagamento real que exija uma URL pública;
- quem for avaliar ou continuar o projeto precisa conseguir rodar tudo na própria máquina, do mesmo jeito.

## Decisão

O sistema **não será publicado na internet**. Ele roda com **Docker Compose**, em qualquer máquina com Docker, com um único comando:

```bash
docker compose up -d --build
```

| Container | Imagem | Papel | Porta no host |
|---|---|---|---|
| `postgres` | `postgres:17-alpine` | Banco de dados, com os dados em um volume (`postgres-data`) | `5433` |
| `api` | `backend/Dockerfile` (.NET 9) | API REST; cria as tabelas e os dados iniciais ao subir | — (só na rede interna) |
| `web` | `frontend/Dockerfile` (build do React + nginx) | Serve o frontend e repassa `/api` e `/uploads` para a API | `8080` |

- O navegador acessa só `http://localhost:8080`. Como o nginx repassa `/api` para a API, frontend e API têm a mesma origem e não é preciso configurar CORS.
- A API só sobe depois que o banco está saudável (`healthcheck` + `depends_on`).
- As fotos enviadas pelo administrador ficam em outro volume (`uploads-data`), então continuam lá depois de reiniciar.
- Para desenvolver, é possível subir só o banco (`docker compose up -d postgres`) e rodar a API e o frontend fora do Docker (README).

## Consequências

**Vantagens**
- Um comando sobe o sistema inteiro, igual em qualquer máquina: sem contas, sem custo e sem depender de internet na apresentação.
- Mesmo ambiente para desenvolvimento, testes ponta a ponta (Playwright contra `localhost:8080`) e apresentação.
- `docker compose down -v` volta o banco ao estado inicial em segundos.
- Como API e frontend já são imagens Docker, publicar no futuro em qualquer provedor exige pouca mudança.

**Desvantagens**
- O sistema não fica acessível pela internet; cada pessoa precisa rodá-lo na própria máquina, com o Docker Desktop instalado.
- A primeira execução demora alguns minutos para baixar as imagens e compilar.
- Requisitos que dependem de um ambiente publicado ficam fora do escopo do MVP:
  - **RNF05 (HTTPS):** a comunicação local é HTTP em `localhost`. Em uma publicação futura, o HTTPS seria feito no proxy (nginx) ou pelo provedor.
  - **RNF10 (disponibilidade de 99%):** não se aplica a um sistema executado localmente.
  - **Gateway Pix real:** exigiria uma URL pública para receber as notificações. A rota `/api/pagamentos/notificacao` já existe e está pronta para isso.

**Alternativas descartadas:** PaaS gratuito (Vercel para o frontend, Render para a API e Neon para o banco), pelos motivos do contexto; AWS/Azure (configuração complexa e risco de cobrança); VPS própria (exige manter servidor, certificados e backups).
