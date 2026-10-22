# 6. Documentação da API

API REST em .NET 9 (ASP.NET Core). Todas as rotas começam com `/api` e trocam JSON.

| Ambiente | Endereço base |
|---|---|
| Docker (`docker compose up`) | `http://localhost:8080/api` (o nginx do frontend repassa para a API) |
| Desenvolvimento (`dotnet run`) | `http://localhost:5091/api` |

## Swagger

Com a API em `Development` (o padrão do Docker e do `dotnet run`), a documentação interativa fica no **Swagger UI**:

| Ambiente | Swagger UI | Especificação OpenAPI |
|---|---|---|
| Docker | **http://localhost:8080/swagger** | http://localhost:8080/openapi/v1.json |
| Desenvolvimento | http://localhost:5091/swagger | http://localhost:5091/openapi/v1.json |

As rotas aparecem agrupadas por história de usuário (HU01 a HU07) e podem ser testadas pelo botão **Try it out**. Para as rotas com cadeado: chame `POST /api/admin/login` (`admin` / `admin`) ou `POST /api/login`, copie o `token` da resposta, clique em **Authorize** e cole o token.

## Convenções

| Item | Formato |
|---|---|
| Datas | `AAAA-MM-DD` (ex.: `2026-11-20`) |
| Data e hora | ISO 8601 com fuso (ex.: `2026-11-20T15:04:05+00:00`) |
| Valores | número decimal (ex.: `150.00`) |
| CPF na entrada | com ou sem pontuação (`529.982.247-25` ou `52998224725`) |
| CPF na saída | sempre mascarado: `***.982.247-**` (RNF07) |
| Código da reserva | `QSK-` + 6 caracteres (ex.: `QSK-7F3K9Q`) |
| Status da reserva | `PENDENTE_PAGAMENTO`, `CONFIRMADA`, `EXPIRADA`, `CANCELADA` e, só na exibição, `REALIZADA` (confirmada com data já passada) |
| Status do dia no calendário | `LIVRE`, `OCUPADO`, `INDISPONIVEL` |

### Autenticação

As rotas marcadas com 🔒 exigem o cabeçalho `Authorization: Bearer <token>`. O token é um JWT que expira em 60 minutos (RNF04).

| Tipo | Como obter | Acessa |
|---|---|---|
| Cliente | `POST /api/contas` ou `POST /api/login` | `/api/minha-conta` e `/api/minhas-reservas/*` |
| Administrador | `POST /api/admin/login` | `/api/admin/*` |

Um token de cliente não acessa a administração e vice-versa (resposta `403`).

### Erros

Todo erro devolve um JSON com a mensagem pronta para exibir ao usuário:

```json
{ "message": "Data indisponível" }
```

| Código | Quando | Exemplos de mensagem |
|---|---|---|
| `400` | Entrada inválida | `Nome inválido`, `E-mail inválido`, `Telefone inválido`, `Requisição inválida` (JSON malformado) |
| `401` | Login inválido ou token ausente/expirado | `E-mail ou senha inválidos` |
| `403` | Token de outro perfil (cliente na administração) | — |
| `404` | Recurso não encontrado (ou código + CPF não batem) | `Reserva não encontrada`, `Quiosque não encontrado`, `Não encontramos reservas com este CPF` |
| `409` | Conflito com dado já existente | `Data indisponível` (RN01), `Este CPF já possui conta. Faça login.`, `Este quiosque tem reservas e não pode ser excluído. Desative-o.` (RN15) |
| `422` | Regra de negócio violada | `CPF inválido` (RN04), `A reserva deve ser feita com 2 a 90 dias de antecedência` (RN03), `Limite de 2 reservas ativas por CPF atingido` (RN08), `O cancelamento só é permitido até 48h antes da data reservada` (RN07) |
| `429` | Mais de 5 consultas/cancelamentos por minuto no mesmo IP (RNF06) | `Muitas tentativas. Aguarde um minuto e tente novamente.` |

## Resumo das rotas

| Método | Rota | Auth | Descrição | Requisitos |
|---|---|---|---|---|
| GET | `/api/quiosques` | — | Lista os quiosques ativos | RF01 · HU01 |
| GET | `/api/quiosques/{id}` | — | Detalhes de um quiosque | RF02 · HU01 |
| GET | `/api/quiosques/{id}/calendario?ano=&mes=` | — | Calendário do mês | RF03 · HU01 |
| POST | `/api/reservas` | — | Cria reserva sem conta | RF04 · HU02 |
| POST | `/api/reservas/{code}/pagamento` | — | Gera/reaproveita a cobrança Pix | RF05 · HU03 |
| POST | `/api/pagamentos/notificacao` | — | Webhook do gateway Pix | RF06 · HU03 |
| POST | `/api/dev/pagamentos/{transactionId}/aprovar` | — | Simula o Pix pago (só em desenvolvimento) | RF06 |
| POST | `/api/reservas/{code}/comprovante` | — | Comprovante da reserva | RF07 · HU03 |
| POST | `/api/reservas/consulta` | — | Consulta todas as reservas de um CPF | RF08 · HU04 |
| POST | `/api/reservas/{code}/cancelamento` | — | Cancela uma reserva da consulta (código + CPF) | RF08 · HU04 |
| POST | `/api/contas` | — | Cria conta de cliente | RF09 · HU05 |
| POST | `/api/login` | — | Login do cliente | RF09 · HU05 |
| GET | `/api/minha-conta` | 🔒 cliente | Dados do cliente logado | HU05 |
| GET | `/api/minhas-reservas` | 🔒 cliente | Histórico de reservas | RF10 · HU05 |
| POST | `/api/minhas-reservas` | 🔒 cliente | Reserva com o CPF da conta | RF04 · HU05 |
| GET | `/api/minhas-reservas/{code}` | 🔒 cliente | Detalhe de uma reserva do cliente | RF10 · HU05 |
| POST | `/api/minhas-reservas/{code}/pagamento` | 🔒 cliente | Cobrança Pix da reserva do cliente | RF05 · HU05 |
| POST | `/api/minhas-reservas/{code}/cancelamento` | 🔒 cliente | Cancela reserva do cliente | RF08 · HU05 |
| POST | `/api/admin/login` | — | Login do administrador | RF11 · HU06 |
| POST | `/api/admin/reservas/busca` | 🔒 admin | Lista/filtra reservas | RF12 · HU06 |
| GET | `/api/admin/reservas/{code}` | 🔒 admin | Detalhe da reserva | RF12 · HU06 |
| POST | `/api/admin/reservas` | 🔒 admin | Cria reserva para qualquer cliente | RF13 · HU06 |
| PUT | `/api/admin/reservas/{code}` | 🔒 admin | Altera quiosque e data | RF14 · HU06 |
| POST | `/api/admin/reservas/{code}/pagamento` | 🔒 admin | Cobrança Pix de reserva pendente | RF13 · HU06 |
| POST | `/api/admin/reservas/{code}/cancelamento` | 🔒 admin | Cancela a qualquer momento | RF15 · HU06 |
| GET | `/api/admin/quiosques` | 🔒 admin | Lista todos os quiosques | RF16 · HU07 |
| GET | `/api/admin/quiosques/{id}` | 🔒 admin | Detalhe de um quiosque | RF16 · HU07 |
| POST | `/api/admin/quiosques` | 🔒 admin | Cadastra quiosque | RF16 · HU07 |
| PUT | `/api/admin/quiosques/{id}` | 🔒 admin | Edita quiosque | RF16 · HU07 |
| DELETE | `/api/admin/quiosques/{id}` | 🔒 admin | Exclui quiosque sem reservas | RF16 · RN15 · HU07 |
| POST | `/api/admin/imagens` | 🔒 admin | Envia foto (multipart) | RF16 · HU07 |

---

## Quiosques (HU01)

### `GET /api/quiosques`

Lista os quiosques **ativos** (RF01).

**200**
```json
[
  {
    "id": "0192f0a1-...",
    "name": "Quiosque das Mangueiras",
    "photoUrl": "https://...",
    "capacity": 30,
    "dailyRate": 150.00,
    "items": ["Churrasqueira", "Pia", "Mesas"]
  }
]
```

### `GET /api/quiosques/{id}`

Detalhes de um quiosque ativo (RF02).

**200**
```json
{
  "id": "0192f0a1-...",
  "name": "Quiosque das Mangueiras",
  "description": "...",
  "capacity": 30,
  "dailyRate": 150.00,
  "photoUrl": "https://...",
  "usageRules": "...",
  "items": ["Churrasqueira", "Pia"],
  "photos": ["https://...", "https://..."]
}
```

**404** `Quiosque não encontrado` (inexistente ou desativado).

### `GET /api/quiosques/{id}/calendario?ano=2026&mes=11`

Situação de cada dia do mês (RF03). Considera reserva ativa (RN01), a janela de 2 a 90 dias (RN03) e ignora pendentes com prazo vencido (RN05).

**200**
```json
{
  "kioskId": "0192f0a1-...",
  "year": 2026,
  "month": 11,
  "days": [
    { "date": "2026-11-01", "status": "LIVRE" },
    { "date": "2026-11-21", "status": "OCUPADO" }
  ]
}
```

**400** `Mês ou ano inválido` · **404** `Quiosque não encontrado`.

---

## Reservas sem conta (HU02, HU03, HU04)

### `POST /api/reservas`

Cria uma reserva aguardando pagamento, que bloqueia a data por 15 minutos (RF04, RN05).

**Corpo**
```json
{
  "kioskId": "0192f0a1-...",
  "date": "2026-11-20",
  "cpf": "529.982.247-25",
  "name": "Maria Silva",
  "email": "maria@exemplo.com",
  "phone": "(47) 99999-0000"
}
```

Validações: CPF válido (RN04); nome de 1 a 150 caracteres; e-mail válido; telefone com 10 ou 11 dígitos.

**201**
```json
{
  "code": "QSK-7F3K9Q",
  "kioskId": "0192f0a1-...",
  "date": "2026-11-20",
  "amount": 150.00,
  "status": "PENDENTE_PAGAMENTO",
  "expiresAt": "2026-10-21T15:19:00+00:00"
}
```

| Erro | Mensagem | Regra |
|---|---|---|
| 400 | `Nome inválido`, `E-mail inválido`, `Telefone inválido` | |
| 422 | `CPF inválido` | RN04 |
| 404 | `Quiosque não encontrado` | |
| 409 | `Data indisponível` | RN01 (garantido pelo índice único parcial, mesmo com requisições simultâneas) |
| 422 | `A reserva deve ser feita com 2 a 90 dias de antecedência` | RN03 |
| 422 | `Limite de 2 reservas ativas por CPF atingido` | RN08 |

Se o CPF já existir com outros dados de contato, o cadastro é atualizado (RN12).

### `POST /api/reservas/{code}/pagamento`

Gera a cobrança Pix (ou devolve a já gerada) de uma reserva aguardando pagamento (RF05).

**Corpo** `{ "cpf": "52998224725" }`

**200**
```json
{
  "reservationCode": "QSK-7F3K9Q",
  "amount": 150.00,
  "pixCopyPaste": "00020126...6304ABCD",
  "expiresAt": "2026-10-21T15:19:00+00:00",
  "transactionId": "fake-QSK7F3K9Q-15000-..."
}
```

**404** `Reserva não encontrada` · **422** `Esta reserva não está aguardando pagamento` (expirada, cancelada ou já paga).

### `POST /api/pagamentos/notificacao`

Webhook chamado pelo gateway Pix quando o pagamento muda de situação (RF06). A API confere a situação no próprio gateway antes de confirmar.

**Corpo** `{ "transactionId": "fake-QSK7F3K9Q-15000-..." }` → **200**.

- Pagamento aprovado dentro do prazo: a reserva vira `CONFIRMADA` e o comprovante é enviado por e-mail (RF06, RF07).
- Notificação repetida: nada muda e nenhum novo e-mail é enviado (RNF03).
- Pagamento aprovado depois da expiração: o valor é reembolsado e a reserva continua `EXPIRADA` (RN11).

### `POST /api/dev/pagamentos/{transactionId}/aprovar`

**Só existe em desenvolvimento.** Simula o cliente pagando o Pix e o gateway enviando a notificação. É o que o botão "Simular pagamento do Pix" da tela de pagamento chama.

**200** `{ "outcome": "Confirmed" }`

### `POST /api/reservas/{code}/comprovante`

Comprovante da reserva (RF07). A tela de pagamento também consulta esta rota até o status virar `CONFIRMADA`.

**Corpo** `{ "cpf": "52998224725" }`

**200**
```json
{
  "code": "QSK-7F3K9Q",
  "kioskName": "Quiosque das Mangueiras",
  "kioskCapacity": 30,
  "date": "2026-11-20",
  "startTime": "08:00:00",
  "endTime": "22:00:00",
  "holderName": "Maria Silva",
  "maskedCpf": "***.982.247-**",
  "amount": 150.00,
  "status": "CONFIRMADA"
}
```

**404** `Reserva não encontrada`.

### `POST /api/reservas/consulta`

Consulta sem login só com o CPF (RF08, RN10): devolve **todas** as reservas do CPF, das mais recentes para as mais antigas, no mesmo formato de "Minhas reservas". Limite de 5 tentativas por minuto por IP (RNF06).

**Corpo** `{ "cpf": "529.982.247-25" }`

**200**
```json
{
  "holderName": "Maria Silva",
  "maskedCpf": "***.982.247-**",
  "reservations": [ /* detalhe da reserva */ ]
}
```

Cada item de `reservations` é um *detalhe da reserva* (o status pode ser também `REALIZADA`):
```json
{
  "code": "QSK-7F3K9Q",
  "kioskName": "Quiosque das Mangueiras",
  "kioskCapacity": 30,
  "kioskPhotoUrl": "https://...",
  "date": "2026-11-20",
  "startTime": "08:00:00",
  "endTime": "22:00:00",
  "holderName": "Maria Silva",
  "maskedCpf": "***.982.247-**",
  "amount": 150.00,
  "status": "CONFIRMADA",
  "createdAt": "2026-10-21T15:04:00+00:00",
  "cancellationDeadline": "2026-11-18T03:00:00+00:00",
  "canCancel": true
}
```

**404** `Não encontramos reservas com este CPF` · **422** `CPF inválido` · **429** `Muitas tentativas. Aguarde um minuto e tente novamente.`

### `POST /api/reservas/{code}/cancelamento`

Cancela sem login uma das reservas mostradas na consulta: o código vem da lista e o CPF é o informado na consulta (RF08, RN07). Confirmada → `CANCELADA` com reembolso integral; pendente → `CANCELADA` sem cobrança. Mesmo limite de tentativas da consulta.

**Corpo** `{ "cpf": "52998224725" }` → **200** com o *detalhe da reserva* atualizado.

| Erro | Mensagem |
|---|---|
| 404 | `Reserva não encontrada` |
| 422 | `O cancelamento só é permitido até 48h antes da data reservada` (RN07) |
| 422 | `Esta reserva não pode ser cancelada` (já expirada ou cancelada) |
| 429 | `Muitas tentativas. Aguarde um minuto e tente novamente.` |

---

## Conta do cliente (HU05)

### `POST /api/contas`

Cria a conta de um CPF que já fez ao menos uma reserva (RF09, RN09). Já devolve o token.

**Corpo** `{ "cpf": "52998224725", "email": "maria@exemplo.com", "password": "senhaSegura1" }`

**201** `{ "token": "eyJ...", "expiresAt": "2026-10-21T16:04:00+00:00" }`

| Erro | Mensagem |
|---|---|
| 400 | `E-mail inválido`, `A senha deve ter entre 8 e 72 caracteres` |
| 404 | `Não encontramos reservas com este CPF` (também para CPF inválido) |
| 409 | `Este CPF já possui conta. Faça login.`, `Este e-mail já está em uso por outra conta` |

### `POST /api/login`

**Corpo** `{ "email": "maria@exemplo.com", "password": "senhaSegura1" }`

**200** `{ "token": "eyJ...", "expiresAt": "..." }` · **401** `E-mail ou senha inválidos`.

### 🔒 `GET /api/minha-conta`

Dados para pré-preencher a reserva.

**200** `{ "name": "Maria Silva", "email": "maria@exemplo.com", "phone": "47999990000", "maskedCpf": "***.982.247-**" }`

### 🔒 `GET /api/minhas-reservas`

Histórico do cliente, incluindo reservas feitas antes de criar a conta (RF10, RN09).

**200** `{ "holderName": "Maria Silva", "maskedCpf": "***.982.247-**", "reservations": [ /* detalhe da reserva */ ] }`

### 🔒 `POST /api/minhas-reservas`

Reserva com o CPF da conta; mesmas regras e erros de `POST /api/reservas`.

**Corpo** `{ "kioskId": "...", "date": "2026-11-20", "name": "...", "email": "...", "phone": "..." }` → **201** igual a `POST /api/reservas`.

### 🔒 `GET /api/minhas-reservas/{code}` · `POST /api/minhas-reservas/{code}/pagamento` · `POST /api/minhas-reservas/{code}/cancelamento`

Iguais às versões sem login, mas sem o CPF no corpo: só alcançam reservas do próprio cliente (outra reserva → **404**).

---

## Administração (HU06, HU07)

### `POST /api/admin/login`

Login próprio do administrador (RF11). O primeiro administrador é criado ao subir a API com `Admin:Email` e `Admin:Password`.

**Corpo** `{ "email": "admin", "password": "admin" }`

**200** `{ "token": "eyJ...", "expiresAt": "...", "name": "Administrador" }` · **401** `E-mail ou senha inválidos`.

### 🔒 `POST /api/admin/reservas/busca`

Lista reservas com filtros opcionais (RF12). É `POST` para o CPF não ir para a URL nem para os logs.

**Corpo** `{ "from": "2026-11-01", "to": "2026-11-30", "kioskId": null, "status": "CONFIRMADA", "cpf": null }`

**200** — lista de *reserva da administração*:
```json
[
  {
    "reservation": { /* detalhe da reserva */ },
    "kioskId": "0192f0a1-...",
    "customerEmail": "maria@exemplo.com",
    "customerPhone": "47999990000",
    "paidWith": "PIX",
    "isActive": true
  }
]
```

**400** `Status inválido`.

### 🔒 `GET /api/admin/reservas/{code}`

**200** *reserva da administração* · **404** `Reserva não encontrada`.

### 🔒 `POST /api/admin/reservas`

Cria reserva para qualquer cliente (RF13), sem a antecedência do RN03 e sem o limite do RN08 (RN13). Com `"paid": true` já nasce `CONFIRMADA`, com pagamento `ADMINISTRACAO` (RN14).

**Corpo** `{ "kioskId": "...", "date": "2026-10-22", "cpf": "...", "name": "...", "email": "...", "phone": "...", "paid": true }`

**201** *reserva da administração* · **409** `Data indisponível` (RN01 vale também para o admin).

### 🔒 `PUT /api/admin/reservas/{code}`

Altera quiosque e/ou data de uma reserva ativa (RF14). O valor é mantido.

**Corpo** `{ "kioskId": "...", "date": "2026-11-21" }`

**200** *reserva da administração* · **409** `Data indisponível` · **422** `Só reservas ativas podem ser alteradas`.

### 🔒 `POST /api/admin/reservas/{code}/pagamento`

Cobrança Pix de uma reserva aguardando pagamento, para mostrar ao cliente. **200** igual a `POST /api/reservas/{code}/pagamento`.

### 🔒 `POST /api/admin/reservas/{code}/cancelamento`

Cancela a qualquer momento, sem o prazo de 48h (RF15, RN13). Pago via Pix → reembolso pelo gateway; pago na administração → reembolso fora do sistema (RN14).

**200** *reserva da administração* · **422** `Esta reserva não pode ser cancelada`.

### 🔒 `GET /api/admin/quiosques` · `GET /api/admin/quiosques/{id}`

Todos os quiosques, inclusive desativados.

**200**
```json
{
  "id": "...", "name": "...", "description": "...", "capacity": 30, "dailyRate": 150.00,
  "photoUrl": "/uploads/abc.jpg", "usageRules": "...", "items": ["..."], "photos": ["..."],
  "isActive": true, "hasReservations": false
}
```

### 🔒 `POST /api/admin/quiosques` · `PUT /api/admin/quiosques/{id}`

Cadastra ou edita (RF16). Novas reservas usam o novo valor; as já feitas mantêm o valor antigo.

**Corpo**
```json
{
  "name": "Quiosque G", "description": "...", "capacity": 40, "dailyRate": 180.00,
  "photoUrl": "/uploads/abc.jpg", "usageRules": "...", "isActive": true,
  "items": ["Churrasqueira"], "photos": ["/uploads/def.jpg"]
}
```

**201** (POST) / **200** (PUT) com o quiosque · **400** `A capacidade deve ser entre 1 e 1000 pessoas`, `Valor da diária inválido`, `<campo> é obrigatório e deve ter até N caracteres` · **404** `Quiosque não encontrado`.

### 🔒 `DELETE /api/admin/quiosques/{id}`

Exclui um quiosque que nunca teve reservas (RN15).

**204** · **404** `Quiosque não encontrado` · **409** `Este quiosque tem reservas e não pode ser excluído. Desative-o.`

### 🔒 `POST /api/admin/imagens`

Envio de foto do quiosque como `multipart/form-data`, campo `arquivo`. Aceita JPG, PNG ou WEBP de até 5 MB.

**201** `{ "url": "/uploads/0192f0....jpg" }` · **400** `Envie uma imagem JPG, PNG ou WEBP de até 5 MB`.

## Exemplo com curl

```bash
# 1. Pegar o id de um quiosque
curl http://localhost:5091/api/quiosques

# 2. Reservar
curl -X POST http://localhost:5091/api/reservas -H "Content-Type: application/json" \
  -d '{"kioskId":"<id>","date":"2026-11-20","cpf":"52998224725","name":"Maria","email":"maria@exemplo.com","phone":"47999990000"}'

# 3. Gerar o Pix e simular o pagamento (desenvolvimento)
curl -X POST http://localhost:5091/api/reservas/QSK-XXXXXX/pagamento -H "Content-Type: application/json" -d '{"cpf":"52998224725"}'
curl -X POST http://localhost:5091/api/dev/pagamentos/<transactionId>/aprovar
```
