# Sistema de Reserva de Quiosques

Projeto acadêmico da disciplina de Design e Arquitetura de Software (UNIVILLE).

## Descrição do sistema

Sistema web para reservar quiosques de churrasqueira de um condomínio **por diária** (das 08:00 às 22:00). O morador escolhe o quiosque e um dia livre no calendário, informa CPF e dados de contato (**sem precisar criar conta**) e paga via **Pix**. Assim que o pagamento é aprovado, a reserva é confirmada e o comprovante é enviado por e-mail.

Quem quiser pode criar uma conta depois, vinculada ao CPF, e ver todo o histórico de reservas. A administração do condomínio tem uma área própria para gerenciar reservas e quiosques.

## Objetivo do MVP

Substituir o processo manual de reserva (planilha, telefone e pagamento conferido à mão) por um fluxo on-line em que:

- o cliente reserva em no máximo 4 telas (lista → detalhe → dados → pagamento), sem cadastro;
- **nunca existam duas reservas para o mesmo quiosque e data**, nem com cliques simultâneos;
- a data só fique bloqueada por 15 minutos enquanto o Pix não é pago;
- a reserva seja confirmada automaticamente quando o pagamento é aprovado;
- o cliente consulte suas reservas só com o CPF e possa cancelá-las, respeitando o prazo de 48h;
- o administrador gerencie reservas e quiosques sem depender dessas regras do cliente.

Os requisitos completos estão em [docs/](#documentação).

## Funcionalidades

| História de usuário | O que o sistema faz | Requisitos |
|---|---|---|
| HU01 — Ver quiosques e disponibilidade | Lista de quiosques ativos, detalhe com fotos, itens e regras, calendário mensal com dias livres/ocupados | RF01, RF02, RF03 |
| HU02 — Reservar sem conta | Reserva com CPF, nome, e-mail e telefone; data bloqueada por 15 min | RF04 |
| HU03 — Pagar e receber o comprovante | Pix com QR Code e copia-e-cola, confirmação automática, comprovante por e-mail | RF05, RF06, RF07 |
| HU04 — Consultar e cancelar | Consulta de todas as reservas pelo CPF, cancelamento com reembolso quando pago | RF08 |
| HU05 — Conta e histórico | Conta vinculada ao CPF, login, "Minhas reservas", reserva com dados pré-preenchidos | RF09, RF10 |
| HU06 — Administrar reservas | Login do admin, lista com filtros, criar, alterar quiosque/data e cancelar reservas | RF11–RF15 |
| HU07 — Administrar quiosques | Cadastrar, editar, ativar/desativar, excluir quiosques e enviar fotos | RF16 |

## Tecnologias utilizadas

| Parte | Tecnologia |
|---|---|
| Backend | .NET 9, ASP.NET Core (Minimal APIs), Entity Framework Core 9, Npgsql |
| Autenticação | JWT (expira em 60 min) e senhas com bcrypt (custo 12) |
| Documentação da API | Swagger UI + OpenAPI (em desenvolvimento) e [docs/06-api-endpoints.md](docs/06-api-endpoints.md) |
| Banco de dados | PostgreSQL 17 |
| Frontend | React 19, TypeScript, Vite, Tailwind CSS 4, React Router 7 |
| Testes | xUnit + Testcontainers (backend) e Playwright (ponta a ponta) |
| Execução | Docker e Docker Compose; nginx servindo o frontend |

As decisões estão registradas nos [ADRs](docs/04-adrs/).

## Arquitetura da aplicação

```
 Navegador (React SPA)
        │  HTTP/JSON  (/api/...)
        ▼
 nginx  (container "web", porta 8080)  ── serve o build do React e repassa /api e /uploads
        │
        ▼
 API .NET 9  (container "api")
   ├── Quiosques.Api             Endpoints, autenticação JWT, tratamento de erros, rate limit, job de expiração
   ├── Quiosques.Application     Casos de uso (CreateReservation, ConfirmPayment, CancelReservation, ...)
   ├── Quiosques.Domain          Entidades e regras de negócio puras (CPF, janela de 2 a 90 dias, prazo de 48h, ...)
   └── Quiosques.Infrastructure  EF Core, repositórios, migrations, gateway Pix simulado, e-mail, fotos
        │
        ▼
 PostgreSQL  (container "postgres", porta 5433 no host)
```

- **Camadas (Clean Architecture):** o domínio não depende de nada; a aplicação depende do domínio e de interfaces (`IReservationRepository`, `IPaymentGateway`, `IEmailSender`, `IClock`); a infraestrutura implementa essas interfaces; a API só traduz HTTP para casos de uso.
- **Reserva duplicada (RN01):** garantida pelo banco com um índice único parcial em `(quiosque_id, data_reserva)` só para reservas ativas ([ADR-001](docs/04-adrs/ADR-001-banco-de-dados.md)). Duas requisições simultâneas → uma é aceita e a outra recebe `409 Data indisponível`.
- **Expiração (RN05):** um job em segundo plano expira, a cada minuto, as reservas não pagas em 15 minutos.
- **Pagamento:** o gateway Pix é simulado (`FakePaymentGateway`). O QR Code é um Pix real para a chave configurada, mas a confirmação é feita pelo botão "Simular pagamento" (ou pela rota `/api/dev/...`). O e-mail do comprovante é escrito no log da API (`LoggingEmailSender`).
- **Erros:** toda exceção de negócio vira uma resposta `{ "message": "..." }` com o código HTTP adequado (400, 401, 404, 409, 422, 429), e o frontend mostra essa mensagem.

```
backend/
  src/Quiosques.{Api,Application,Domain,Infrastructure}
  tests/Quiosques.Domain.Tests     testes unitários das regras
  tests/Quiosques.Api.Tests        testes de integração da API com PostgreSQL real, organizados por HU
frontend/
  src/pages        telas do cliente          src/admin       telas da administração
  src/components   componentes reutilizáveis src/api         cliente HTTP e tipos
  e2e/             testes ponta a ponta (Playwright)
docs/              requisitos, protótipos, ADRs, DER e documentação da API
docker-compose.yml banco + API + frontend
```

## Pré-requisitos

| Para | Precisa de |
|---|---|
| Só rodar o sistema (apresentação) | [Docker Desktop](https://www.docker.com/products/docker-desktop/) |
| Desenvolver | Docker, [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0) e [Node.js 24](https://nodejs.org/) (ou mais novo) |
| Rodar os testes | Docker (os testes do backend sobem um PostgreSQL próprio), .NET SDK 9 e Node.js |

## Instalação

```bash
git clone https://github.com/WellingtonGr3in/reservaQuiosques.git
cd reservaQuiosques
```

Para rodar com Docker não é preciso instalar mais nada. Para desenvolver:

```bash
dotnet tool restore               # instala o dotnet-ef (migrations)
cd frontend && npm install        # dependências do frontend
```

## Execução

### Opção 1 — Tudo no Docker (apresentação)

```bash
docker compose up -d --build
```

Abra **http://localhost:8080**. Na primeira vez o Docker baixa as imagens e compila tudo (alguns minutos). A API cria as tabelas e os quiosques iniciais sozinha.

| Comando | Para quê |
|---|---|
| `docker compose ps` | Ver se os 3 containers (postgres, api, web) estão rodando |
| `docker compose logs -f api` | Acompanhar o log da API (o comprovante "enviado por e-mail" aparece aqui) |
| `docker compose up -d --build` | Subir de novo depois de mudar o código |
| `docker compose down` | Parar tudo (os dados do banco continuam guardados) |
| `docker compose down -v` | Parar e **apagar** o banco (volta ao estado inicial) |

**Administração:** http://localhost:8080/admin (link "Administração" no rodapé), usuário `admin` e senha `admin`.

**Documentação da API (Swagger):** http://localhost:8080/swagger — lista todas as rotas agrupadas por história de usuário e permite testá-las ("Try it out"). Para as rotas com cadeado, faça login em `POST /api/admin/login` ou `POST /api/login`, copie o `token` e clique em **Authorize**.

### Opção 2 — Desenvolvimento

**1. Banco e API** (a API sobe em http://localhost:5091):

```bash
docker compose up -d postgres
dotnet ef database update -p backend/src/Quiosques.Infrastructure -s backend/src/Quiosques.Api
dotnet run --project backend/src/Quiosques.Api --urls http://localhost:5091
```

**2. Frontend**, em outro terminal (abre em http://localhost:3000 e repassa `/api` para a API):

```bash
cd frontend
npm run dev
```

**Documentação interativa da API (Swagger):** http://localhost:5091/swagger (especificação em http://localhost:5091/openapi/v1.json). A descrição completa de cada endpoint está em [docs/06-api-endpoints.md](docs/06-api-endpoints.md).

### Pagamento na apresentação

O gateway Pix é simulado. Na tela de pagamento, o botão ⚡ **"Simular pagamento do Pix"** (canto superior direito do cartão) aprova o Pix como se o cliente tivesse pago, e a tela avança para o comprovante. Ele aparece no `npm run dev` e na versão do Docker (`VITE_SIMULAR_PAGAMENTO`). O comprovante "enviado por e-mail" aparece em `docker compose logs -f api`.

## Variáveis de ambiente

A API lê a configuração do `appsettings.json`, do `appsettings.Development.json` e de variáveis de ambiente. Nas variáveis, o `:` da configuração vira `__` (ex.: `Admin:Email` → `Admin__Email`). Os valores abaixo são **apenas para desenvolvimento**.

### API (`backend/src/Quiosques.Api`)

| Variável | Para quê | Valor em desenvolvimento |
|---|---|---|
| `ConnectionStrings__Quiosques` | Conexão com o PostgreSQL | `Host=localhost;Port=5433;Database=quiosques;Username=quiosques;Password=quiosques_dev` (no Docker: `Host=postgres;Port=5432;...`) |
| `ASPNETCORE_ENVIRONMENT` | `Development` liga o pagamento simulado (`/api/dev/...`), o Swagger/OpenAPI e a chave JWT de desenvolvimento | `Development` |
| `Jwt__Key` | Chave para assinar os tokens (mínimo 32 caracteres). **Obrigatória em produção** | definida no `appsettings.Development.json` |
| `Database__MigrateOnStartup` | `true` cria/atualiza as tabelas ao subir a API | `true` no Docker; fora dele use `dotnet ef database update` |
| `Admin__Name`, `Admin__Email`, `Admin__Password` | Primeiro administrador, criado ao subir se ainda não existir (RF11) | `Administrador` / `admin` / `admin` |
| `Pix__Key` | Chave Pix do recebedor usada no QR Code (sem ela o código é só ilustrativo) | telefone de exemplo |
| `Pix__MerchantName`, `Pix__MerchantCity` | Nome e cidade do recebedor no Pix | — |
| `Jobs__ReservationExpiration__Enabled` | Liga/desliga o job que expira reservas não pagas (os testes desligam) | `true` |

### Frontend (`frontend/`)

| Variável | Para quê | Padrão |
|---|---|---|
| `VITE_SIMULAR_PAGAMENTO` | `true` mostra o botão de simular pagamento no build de produção (no `npm run dev` ele sempre aparece) | `true` no Docker |
| `API_URL` | Para onde o `npm run dev` repassa `/api` e `/uploads` | `http://localhost:5091` |
| `E2E_BASE_URL` | Endereço usado pelos testes Playwright | `http://localhost:8080` |
| `E2E_ADMIN_LOGIN`, `E2E_ADMIN_PASSWORD` | Login do admin nos testes Playwright | `admin` / `admin` |

Para configurações locais sem mexer nos arquivos versionados, crie `backend/src/Quiosques.Api/appsettings.Local.json` (ignorado pelo git) ou use variáveis de ambiente.

## Banco de dados

- **SGBD:** PostgreSQL 17, no container `quiosques-postgres`.
- **Acesso pelo host** (DBeaver, pgAdmin, etc.): `localhost:5433`, banco `quiosques`, usuário `quiosques`, senha `quiosques_dev`.
- **Criação:** feita pelas migrations do EF Core em `backend/src/Quiosques.Infrastructure/Persistence/Migrations` (no Docker, automaticamente ao subir a API).
- **Modelo:** segue o [DER](docs/05-modelagem-dados.md).

| Tabela | Conteúdo |
|---|---|
| `cliente` | Pessoa identificada pelo CPF (único) |
| `conta` | Login opcional do cliente (1 por CPF) |
| `administrador` | Login da administração, separado das contas de cliente |
| `quiosque`, `quiosque_foto`, `quiosque_item` | Quiosques, galeria de fotos e itens inclusos |
| `reserva` | Uma diária de um quiosque para um cliente, com código, valor e status |
| `pagamento` | Cobranças Pix e pagamentos registrados na administração |

**Restrições principais:** chaves primárias UUID; chaves estrangeiras entre todas as tabelas relacionadas; `CHECK` nos status e no método de pagamento; únicos em `cliente.cpf`, `conta.cliente_id`, `conta.email_login`, `administrador.email`, `reserva.codigo` e `pagamento.transacao_gateway_id`; e o índice único parcial `ux_reserva_quiosque_data_ativa`, que impede duas reservas ativas no mesmo quiosque e data.

**Dados iniciais:** 6 quiosques (seed por migration) e o administrador da configuração. Para voltar ao estado inicial: `docker compose down -v` e `docker compose up -d --build`.

## Testes

### Backend — regras de negócio e API com banco real

```bash
dotnet test backend
```

O Docker precisa estar aberto: o Testcontainers sobe um PostgreSQL só para os testes.

| Projeto | O que cobre |
|---|---|
| `Quiosques.Domain.Tests` | Regras puras: validação de CPF, janela de 2 a 90 dias, prazo de cancelamento de 48h, estados da reserva |
| `Quiosques.Api.Tests` | Um arquivo por história (HU01 a HU07) com os cenários dos critérios de aceite, chamando a API por HTTP com PostgreSQL real: validações, mensagens de erro, regras de negócio, concorrência (100 reservas simultâneas → 1 criada), notificação de pagamento repetida, rate limit e o esquema do banco |

### Ponta a ponta — frontend + API + banco

Os testes Playwright abrem o navegador e usam o sistema como um usuário, conferindo o resultado na tela e no banco (pela API).

```bash
docker compose up -d --build      # sistema completo em http://localhost:8080
cd frontend
npx playwright install chromium   # só na primeira vez
npm run test:e2e
```

| Arquivo | Cenários |
|---|---|
| `e2e/hu01-hu03-reserva.spec.ts` | Lista e detalhe dos quiosques; dia ocupado no calendário; **fluxo completo** lista → detalhe → dados → Pix → comprovante; CPF inválido; campos obrigatórios e e-mail inválido; data indisponível; limite de 2 reservas por CPF |
| `e2e/hu04-hu05-cliente.spec.ts` | Consulta pelo CPF listando todas as reservas e cancelamento de uma delas; CPF sem reservas; criar conta herdando reservas; CPF sem reservas; senha errada |
| `e2e/hu06-hu07-admin.spec.ts` | Login do admin, acesso negado sem login, cancelar reserva pela administração, lista de quiosques |
| `e2e/responsivo.spec.ts` | Telas sem rolagem horizontal em 360px (RNF09) |

O relatório HTML fica em `frontend/playwright-report` (`npx playwright show-report`).

## Documentação

| Documento | Conteúdo |
|---|---|
| [Requisitos funcionais](docs/01-requisitos-funcionais.md) | RFs, regras de negócio e histórias de usuário com critérios de aceite |
| [Requisitos não funcionais](docs/02-requisitos-nao-funcionais.md) | Desempenho, segurança, LGPD, usabilidade e disponibilidade |
| [Protótipos](docs/03-prototipos/prototipos.md) | Telas do MVP |
| [ADRs](docs/04-adrs/) | Banco de dados, stack, execução com Docker (sem publicação) e reserva sem login |
| [Modelagem de dados](docs/05-modelagem-dados.md) | DER, status e restrições |
| [Documentação da API](docs/06-api-endpoints.md) | Todos os endpoints, com corpo, respostas e erros |

**Protótipos no Figma:** https://www.figma.com/design/XcScmvykeXCxQF483gxKgR/Sem-t%25C3%25ADtulo?node-id=0-1&p=f&t=3tnbNGrZ8H2WO1nX-0
