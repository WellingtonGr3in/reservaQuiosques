# Sistema de Reserva de Quiosques

Projeto acadêmico da disciplina de Design e Arquitetura de Software (UNIVILLE). Permite reservar quiosques de churrasqueira por diária, sem criar conta, com pagamento via Pix.

## Documentação

| Documento | Conteúdo |
|---|---|
| [Requisitos funcionais](docs/01-requisitos-funcionais.md) | RFs, regras de negócio e histórias de usuário com critérios de aceite |
| [Requisitos não funcionais](docs/02-requisitos-nao-funcionais.md) | Desempenho, segurança, LGPD, usabilidade e disponibilidade |
| [Protótipos](docs/03-prototipos/prototipos.md) | Telas do MVP (Figma) |
| [ADRs](docs/04-adrs/) | Banco de dados, stack, hospedagem e reserva sem login |
| [Modelagem de dados](docs/05-modelagem-dados.md) | DER, status e restrições |

## Stack

- **Backend:** .NET 9 (ASP.NET Core), Entity Framework Core, PostgreSQL
- **Frontend:** React + TypeScript (Vite)
- **Testes:** xUnit e Testcontainers (PostgreSQL real)

## Como rodar com Docker (apresentação)

Pré-requisito: só o Docker Desktop aberto.

```bash
docker compose up -d --build
```

Abra **http://localhost:8080**. Na primeira vez o Docker baixa as imagens base e compila tudo (alguns minutos); depois é rápido. A API cria as tabelas e os quiosques sozinha ao subir.

| Comando | Para quê |
|---|---|
| `docker compose ps` | Ver se os 3 containers (postgres, api, web) estão rodando |
| `docker compose logs -f api` | Acompanhar o log da API (o comprovante "enviado por e-mail" aparece aqui) |
| `docker compose up -d --build` | Subir de novo depois de mudar o código (recompila as imagens) |
| `docker compose down` | Parar tudo (os dados do banco continuam guardados) |
| `docker compose down -v` | Parar e **apagar** o banco (volta ao estado inicial) |

**Administração:** http://localhost:8080/admin (link "Administração" no rodapé), com o usuário `admin` e a senha `admin`. Esses dados ficam no `docker-compose.yml` e no `appsettings.Development.json` e servem só para desenvolvimento e apresentação.

## Como rodar para desenvolver

Pré-requisitos: .NET SDK 9, Node.js 24 e Docker.

**1. Banco e API** (a API sobe em http://localhost:5091):

```bash
docker compose up -d postgres
dotnet tool restore
dotnet ef database update -p backend/src/Quiosques.Infrastructure -s backend/src/Quiosques.Api
dotnet run --project backend/src/Quiosques.Api --urls http://localhost:5091
```

**2. Frontend**, em outro terminal (abre em http://localhost:3000 e repassa `/api` para a API):

```bash
cd frontend
npm install
npm run dev
```

**Testes da API:** `dotnet test backend`.

O PostgreSQL do Docker fica exposto na porta **5433** (usuário `quiosques`, senha `quiosques_dev`, apenas para desenvolvimento).

### Pagamento na apresentação

O gateway Pix é simulado. Na tela de pagamento, o botão **"Simular pagamento do Pix (modo de desenvolvimento)"** aprova o Pix como se o cliente tivesse pago. Ele aparece no `npm run dev` e na versão do Docker (variável `VITE_SIMULAR_PAGAMENTO` no `docker-compose.yml`). O comprovante "enviado por e-mail" aparece no log da API.
