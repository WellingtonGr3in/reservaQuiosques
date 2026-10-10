# 1. Requisitos Funcionais, Histórias de Usuário e Regras de Negócio

## 1.1 Requisitos Funcionais

| ID | Descrição | Prioridade |
|---|---|---|
| RF01 | O sistema deve listar os quiosques ativos com nome, foto, capacidade e valor da diária. | Alta |
| RF02 | O sistema deve exibir os detalhes de um quiosque (descrição, itens, fotos e regras de uso). | Alta |
| RF03 | O sistema deve exibir um calendário mensal com as datas livres e ocupadas de cada quiosque. | Alta |
| RF04 | O sistema deve permitir reservar sem conta, informando CPF, nome, e-mail e telefone. | Alta |
| RF05 | O sistema deve gerar a cobrança da reserva via Pix (QR Code e copia-e-cola). | Alta |
| RF06 | O sistema deve confirmar a reserva automaticamente quando o pagamento for aprovado. | Alta |
| RF07 | O sistema deve enviar o comprovante da reserva por e-mail, com o código da reserva. | Alta |
| RF08 | O sistema deve permitir consultar e cancelar uma reserva informando código + CPF. | Média |
| RF09 | O sistema deve permitir criar uma conta vinculada ao CPF e fazer login. | Média |
| RF10 | O sistema deve listar o histórico de reservas do cliente logado. | Média |

## 1.2 Regras de Negócio

| ID | Regra |
|---|---|
| RN01 | Um quiosque não pode ter duas reservas ativas na mesma data. Reserva ativa é aquela aguardando pagamento ou confirmada. |
| RN02 | A reserva é feita por diária (o dia inteiro). |
| RN03 | A reserva deve ser feita com no mínimo 2 e no máximo 90 dias de antecedência. |
| RN04 | O CPF informado deve ser válido (dígitos verificadores). |
| RN05 | Enquanto aguarda pagamento, a data fica bloqueada por 15 minutos. Se o pagamento não for aprovado nesse prazo, a reserva expira e a data é liberada. |
| RN06 | A reserva só é confirmada após o pagamento aprovado. |
| RN07 | O cancelamento só é permitido até 48h antes da data reservada, com reembolso integral. |
| RN08 | Cada CPF pode ter no máximo 2 reservas ativas ao mesmo tempo. |
| RN09 | Cada CPF pode ter apenas uma conta. Ao criar a conta, as reservas feitas antes com aquele CPF aparecem no histórico. |
| RN10 | Sem login, a reserva só pode ser consultada ou cancelada com o código da reserva **e** o CPF. |

## 1.3 Histórias de Usuário e Critérios de Aceite

### HU01 — Ver quiosques e disponibilidade
> **Como** cliente, **quero** ver os quiosques e as datas livres de cada um, **para** escolher onde e quando fazer meu evento.

**Requisitos:** RF01, RF02, RF03 · **Regras:** RN01, RN03

```gherkin
Cenário: Ver datas livres e ocupadas
  Dado que o Quiosque A tem uma reserva confirmada em 21/11/2026
  Quando eu abrir o calendário de novembro do Quiosque A
  Então o dia 21 aparece como "Ocupado"
  E os demais dias dentro da janela permitida aparecem como "Livre"

Cenário: Datas fora da janela de antecedência
  Dado que hoje é 09/10/2026
  Quando eu abrir o calendário do Quiosque A
  Então os dias 09/10 e 10/10 aparecem como "Indisponível"
```

### HU02 — Reservar sem criar conta
> **Como** cliente, **quero** reservar informando só meu CPF e dados de contato, **para** não precisar criar uma conta.

**Requisitos:** RF04 · **Regras:** RN01, RN03, RN04, RN05, RN08

```gherkin
Cenário: Reserva criada com sucesso
  Dado que o Quiosque A está livre em 20/11/2026
  Quando eu informar CPF, nome, e-mail e telefone válidos
  E solicitar a reserva para 20/11/2026
  Então a reserva é criada aguardando pagamento
  E a data fica bloqueada por 15 minutos

Cenário: Data já reservada
  Dado que o Quiosque A já tem reserva ativa em 20/11/2026
  Quando eu tentar reservar a mesma data
  Então o sistema informa "Data indisponível"

Cenário: Duas pessoas reservando ao mesmo tempo
  Dado que o Quiosque A está livre em 20/11/2026
  Quando dois clientes confirmarem a reserva no mesmo instante
  Então apenas uma reserva é criada
  E o outro cliente recebe "Data indisponível"

Cenário: CPF inválido
  Quando eu informar o CPF "111.111.111-11"
  Então o sistema informa "CPF inválido"

Cenário: Limite de reservas por CPF
  Dado que meu CPF já tem 2 reservas ativas
  Quando eu tentar fazer outra reserva
  Então o sistema informa que o limite foi atingido
```

### HU03 — Pagar e receber o comprovante
> **Como** cliente, **quero** pagar via Pix e receber o comprovante, **para** garantir minha reserva.

**Requisitos:** RF05, RF06, RF07 · **Regras:** RN05, RN06

```gherkin
Cenário: Pagamento aprovado
  Dado que tenho uma reserva aguardando pagamento
  Quando o pagamento Pix for aprovado
  Então a reserva passa para "Confirmada"
  E recebo o comprovante por e-mail com o código da reserva

Cenário: Pagamento não realizado no prazo
  Dado que tenho uma reserva aguardando pagamento há 15 minutos
  Quando o prazo terminar sem pagamento
  Então a reserva passa para "Expirada"
  E a data volta a ficar livre no calendário
```

### HU04 — Consultar e cancelar reserva
> **Como** cliente sem conta, **quero** consultar e cancelar minha reserva com o código e meu CPF, **para** gerenciá-la sem precisar de login.

**Requisitos:** RF08 · **Regras:** RN07, RN10

```gherkin
Cenário: Consulta com dados corretos
  Quando eu informar o código "QSK-7F3K9Q" e o CPF da reserva
  Então vejo os dados da reserva

Cenário: Código e CPF não batem
  Quando eu informar um código com um CPF diferente do da reserva
  Então o sistema informa "Reserva não encontrada"

Cenário: Cancelamento dentro do prazo
  Dado que minha reserva confirmada é daqui a 10 dias
  Quando eu solicitar o cancelamento
  Então a reserva passa para "Cancelada", o valor é reembolsado e a data é liberada

Cenário: Cancelamento fora do prazo
  Dado que minha reserva confirmada é daqui a 1 dia
  Quando eu solicitar o cancelamento
  Então o sistema informa que o cancelamento só é permitido até 48h antes
```

### HU05 — Criar conta e ver histórico
> **Como** cliente, **quero** criar uma conta vinculada ao meu CPF, **para** acompanhar todas as minhas reservas.

**Requisitos:** RF09, RF10 · **Regras:** RN09

```gherkin
Cenário: Conta herda reservas anteriores
  Dado que fiz 2 reservas sem conta com meu CPF
  Quando eu criar uma conta com esse CPF
  Então as 2 reservas aparecem em "Minhas reservas"

Cenário: CPF que já tem conta
  Dado que meu CPF já possui conta
  Quando eu tentar criar outra conta
  Então o sistema informa "Este CPF já possui conta. Faça login."
```
