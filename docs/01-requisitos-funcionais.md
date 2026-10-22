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
| RF08 | O sistema deve permitir consultar as reservas informando o CPF do titular e cancelá-las. | Média |
| RF09 | O sistema deve permitir criar uma conta vinculada ao CPF e fazer login com e-mail e senha. | Média |
| RF10 | O sistema deve listar o histórico de reservas do cliente logado. | Média |
| RF11 | O sistema deve ter uma área de administração com login próprio (e-mail e senha), separada das contas de cliente. | Média |
| RF12 | O administrador deve listar todas as reservas, filtrando por data, quiosque, status e CPF, e ver os detalhes de cada uma. | Média |
| RF13 | O administrador deve criar reservas para qualquer cliente, aguardando pagamento via Pix ou já pagas na administração. | Média |
| RF14 | O administrador deve alterar o quiosque e a data de uma reserva ativa. | Média |
| RF15 | O administrador deve cancelar qualquer reserva ativa, a qualquer momento, com reembolso integral se estiver paga. | Média |
| RF16 | O administrador deve cadastrar, editar, ativar/desativar e excluir quiosques (nome, descrição, capacidade, valor, fotos, itens e regras de uso), enviando as fotos do próprio computador (JPG, PNG ou WEBP de até 5 MB). | Média |

## 1.2 Regras de Negócio

| ID | Regra |
|---|---|
| RN01 | Um quiosque não pode ter duas reservas ativas na mesma data. Reserva ativa é aquela aguardando pagamento ou confirmada. |
| RN02 | A reserva é feita por diária, que corresponde ao horário de uso do condomínio: das 08:00 às 22:00. |
| RN03 | A reserva deve ser feita com no mínimo 2 e no máximo 90 dias de antecedência. |
| RN04 | O CPF informado deve ser válido (dígitos verificadores). |
| RN05 | Enquanto aguarda pagamento, a data fica bloqueada por 15 minutos. Se o pagamento não for aprovado nesse prazo, a reserva expira e a data é liberada. |
| RN06 | A reserva só é confirmada após o pagamento aprovado. |
| RN07 | O cancelamento só é permitido até 48h antes das 00:00 do dia reservado (horário de Brasília). Reserva confirmada é cancelada com reembolso integral; reserva aguardando pagamento é cancelada sem cobrança. A regra vale com ou sem login. |
| RN08 | Cada CPF pode ter no máximo 2 reservas ativas ao mesmo tempo, contando apenas reservas para hoje ou datas futuras. |
| RN09 | Cada CPF pode ter apenas uma conta, e só pode criá-la um CPF que já fez ao menos uma reserva. O cliente informa o CPF, um e-mail e uma senha para entrar. Ao criar a conta, as reservas feitas antes com aquele CPF aparecem no histórico. |
| RN10 | Sem login, as reservas são consultadas só com o CPF do titular: o sistema mostra todas as reservas daquele CPF, e cada uma pode ser cancelada (RN07) ou, se ainda aguarda pagamento, paga. |
| RN11 | Se um pagamento for aprovado depois que a reserva expirou, o valor é reembolsado automaticamente e a reserva continua expirada. |
| RN12 | Se um CPF já cadastrado fizer nova reserva com nome, e-mail ou telefone diferentes, o cadastro do cliente é atualizado com os dados mais recentes. |
| RN13 | O administrador não está sujeito à antecedência de 2 a 90 dias (RN03), ao limite de reservas por CPF (RN08) nem ao prazo de 48h para cancelar (RN07). Continua valendo uma reserva ativa por quiosque e data (RN01). |
| RN14 | A reserva criada pelo administrador como "paga na administração" já nasce confirmada, com um pagamento registrado como feito na administração (exceção ao RN06). Ao cancelar, o reembolso desse pagamento é feito pela administração, fora do sistema. |
| RN15 | Um quiosque só pode ser excluído se nunca teve reservas; caso contrário, deve ser desativado. Quiosque desativado não aparece para os clientes, e suas reservas continuam valendo. |

## 1.3 Histórias de Usuário e Critérios de Aceite

### HU01 — Ver quiosques e disponibilidade
> **Como** cliente, **quero** ver os quiosques e as datas livres de cada um, **para** escolher onde e quando fazer meu evento.

**Requisitos:** RF01, RF02, RF03 · **Regras:** RN01, RN03, RN05

```gherkin
Cenário: Listar apenas quiosques ativos
  Dado que existem os Quiosques A e B ativos e o Quiosque C inativo
  Quando eu abrir a lista de quiosques
  Então vejo os Quiosques A e B com nome, foto, capacidade e valor da diária
  E não vejo o Quiosque C

Cenário: Ver detalhes do quiosque
  Quando eu abrir o Quiosque A
  Então vejo a descrição, os itens inclusos, as fotos e as regras de uso

Cenário: Ver datas livres e ocupadas
  Dado que o Quiosque A tem uma reserva confirmada em 21/11/2026
  Quando eu abrir o calendário de novembro do Quiosque A
  Então o dia 21 aparece como "Ocupado"
  E os demais dias dentro da janela permitida aparecem como "Livre"

Cenário: Datas fora da janela de antecedência
  Dado que hoje é 09/10/2026
  Quando eu abrir o calendário do Quiosque A
  Então os dias 09/10 e 10/10 aparecem como "Indisponível"
  E o dia 11/10 aparece como "Livre"
  E o dia 07/01/2027 aparece como "Livre"
  E o dia 08/01/2027 aparece como "Indisponível"

Cenário: Reserva pendente com prazo vencido não ocupa a data
  Dado que o Quiosque A tem uma reserva aguardando pagamento em 20/11/2026
  E o prazo de 15 minutos dessa reserva já terminou
  Quando eu abrir o calendário de novembro do Quiosque A
  Então o dia 20 aparece como "Livre"
```

### HU02 — Reservar sem criar conta
> **Como** cliente, **quero** reservar informando só meu CPF e dados de contato, **para** não precisar criar uma conta.

**Requisitos:** RF04 · **Regras:** RN01, RN03, RN04, RN05, RN08, RN12

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

Cenário: Data fora da janela de antecedência
  Dado que hoje é 09/10/2026
  Quando eu tentar reservar o Quiosque A para 10/10/2026 ou para 08/01/2027
  Então o sistema informa que a reserva deve ser feita com 2 a 90 dias de antecedência

Cenário: CPF inválido
  Quando eu informar o CPF "111.111.111-11"
  Então o sistema informa "CPF inválido"

Cenário: Limite de reservas por CPF
  Dado que meu CPF já tem 2 reservas ativas
  Quando eu tentar fazer outra reserva
  Então o sistema informa que o limite foi atingido

Cenário: Dados de contato atualizados
  Dado que meu CPF já fez uma reserva com o e-mail "antigo@exemplo.com"
  Quando eu fizer nova reserva com o e-mail "novo@exemplo.com"
  Então o cadastro do cliente passa a ter o e-mail "novo@exemplo.com"
```

### HU03 — Pagar e receber o comprovante
> **Como** cliente, **quero** pagar via Pix e receber o comprovante, **para** garantir minha reserva.

**Requisitos:** RF05, RF06, RF07 · **Regras:** RN05, RN06, RN11

```gherkin
Cenário: Cobrança Pix gerada
  Dado que tenho uma reserva aguardando pagamento
  Quando eu abrir a tela de pagamento
  Então vejo o QR Code, o código copia-e-cola e o tempo restante

Cenário: Pagamento aprovado
  Dado que tenho uma reserva aguardando pagamento
  Quando o pagamento Pix for aprovado
  Então a reserva passa para "Confirmada"
  E recebo o comprovante por e-mail com o código da reserva e o CPF mascarado

Cenário: Confirmação de pagamento repetida
  Dado que minha reserva já foi confirmada
  Quando o gateway enviar de novo a mesma confirmação de pagamento
  Então a reserva continua "Confirmada"
  E nenhum novo comprovante é enviado

Cenário: Pagamento não realizado no prazo
  Dado que tenho uma reserva aguardando pagamento há 15 minutos
  Quando o prazo terminar sem pagamento
  Então a reserva passa para "Expirada"
  E a data volta a ficar livre no calendário

Cenário: Pagamento aprovado depois do prazo
  Dado que minha reserva expirou
  Quando o pagamento Pix dessa reserva for aprovado
  Então o valor é reembolsado automaticamente
  E a reserva continua "Expirada"
```

### HU04 — Consultar e cancelar reserva
> **Como** cliente sem conta, **quero** consultar e cancelar minhas reservas informando só o meu CPF, **para** gerenciá-las sem precisar de login nem guardar o código da reserva.

**Requisitos:** RF08 · **Regras:** RN07, RN10

```gherkin
Cenário: Consulta pelo CPF
  Dado que fiz uma reserva com meu CPF
  Quando eu informar o meu CPF
  Então vejo os dados da reserva, com o CPF mascarado

Cenário: CPF com mais de uma reserva
  Dado que meu CPF tem uma reserva confirmada e outra aguardando pagamento
  Quando eu informar o meu CPF
  Então vejo as duas reservas, cada uma com a opção de cancelar

Cenário: CPF sem reservas
  Quando eu informar um CPF que nunca fez reserva
  Então o sistema informa "Não encontramos reservas com este CPF"

Cenário: CPF inválido na consulta
  Quando eu informar o CPF "111.111.111-11"
  Então o sistema informa "CPF inválido"

Cenário: Muitas tentativas de consulta
  Dado que fiz 5 consultas no último minuto
  Quando eu fizer a 6ª consulta
  Então o sistema informa que devo aguardar antes de tentar novamente

Cenário: Cancelamento dentro do prazo
  Dado que minha reserva confirmada é daqui a 10 dias
  Quando eu solicitar o cancelamento
  Então a reserva passa para "Cancelada", o valor é reembolsado e a data é liberada

Cenário: Cancelamento no limite do prazo
  Dado que minha reserva confirmada é para 20/11/2026
  E agora são 17/11/2026 às 23:59
  Quando eu solicitar o cancelamento
  Então a reserva passa para "Cancelada"

Cenário: Cancelamento fora do prazo
  Dado que minha reserva confirmada é daqui a 1 dia
  Quando eu solicitar o cancelamento
  Então o sistema informa que o cancelamento só é permitido até 48h antes

Cenário: Cancelamento de reserva aguardando pagamento
  Dado que tenho uma reserva aguardando pagamento
  Quando eu solicitar o cancelamento
  Então a reserva passa para "Cancelada", sem reembolso, e a data é liberada
```

### HU05 — Criar conta e ver histórico
> **Como** cliente, **quero** criar uma conta vinculada ao meu CPF, **para** acompanhar todas as minhas reservas.

**Requisitos:** RF09, RF10 · **Regras:** RN07, RN09

```gherkin
Cenário: Conta herda reservas anteriores
  Dado que fiz 2 reservas sem conta com meu CPF
  Quando eu criar uma conta com esse CPF
  Então as 2 reservas aparecem em "Minhas reservas"

Cenário: CPF sem reservas
  Dado que meu CPF ainda não fez nenhuma reserva
  Quando eu tentar criar uma conta com esse CPF
  Então o sistema informa "Não encontramos reservas com este CPF"

Cenário: Reservar logado com dados já preenchidos
  Dado que estou logado na minha conta
  Quando eu escolher um quiosque e uma data
  Então meu nome, e-mail e telefone já vêm preenchidos e o CPF da conta aparece mascarado
  E a reserva é feita com o CPF da conta, com as mesmas regras da reserva sem conta

Cenário: CPF que já tem conta
  Dado que meu CPF já possui conta
  Quando eu tentar criar outra conta
  Então o sistema informa "Este CPF já possui conta. Faça login."

Cenário: Login com dados corretos
  Dado que tenho uma conta
  Quando eu entrar com meu e-mail e senha
  Então acesso "Minhas reservas"

Cenário: Login com senha errada
  Quando eu entrar com uma senha errada
  Então o sistema informa "E-mail ou senha inválidos"
```

### HU06 — Administrar reservas
> **Como** administrador, **quero** ver, criar, alterar e cancelar reservas de qualquer cliente, **para** atender pedidos feitos na administração e resolver imprevistos.

**Requisitos:** RF11, RF12, RF13, RF14, RF15 · **Regras:** RN01, RN13, RN14

```gherkin
Cenário: Login do administrador
  Dado que existe o administrador "admin@quiosques.local"
  Quando ele entrar com o e-mail e a senha corretos
  Então acessa a área de administração

Cenário: Cliente não acessa a administração
  Dado que estou logado como cliente
  Quando eu tentar acessar a lista de reservas da administração
  Então o acesso é negado

Cenário: Listar e filtrar reservas
  Dado que existem reservas no Quiosque A e no Quiosque B
  Quando o administrador filtrar pelo Quiosque A
  Então vê apenas as reservas do Quiosque A

Cenário: Criar reserva já paga na administração
  Dado que o Quiosque A está livre amanhã
  Quando o administrador criar uma reserva para amanhã como "paga na administração"
  Então a reserva é criada como "Confirmada"
  E não depende da antecedência mínima de 2 dias

Cenário: Criar reserva aguardando Pix
  Quando o administrador criar uma reserva sem marcar como paga
  Então a reserva é criada aguardando pagamento

Cenário: Administrador também respeita a data ocupada
  Dado que o Quiosque A já tem reserva ativa em 20/11/2026
  Quando o administrador criar ou mover outra reserva para o Quiosque A em 20/11/2026
  Então o sistema informa "Data indisponível"

Cenário: Alterar quiosque e data
  Dado que existe uma reserva confirmada no Quiosque A em 20/11/2026
  Quando o administrador alterar para o Quiosque B em 21/11/2026
  Então a reserva passa a ser do Quiosque B em 21/11/2026
  E a data 20/11/2026 do Quiosque A fica livre

Cenário: Cancelar a qualquer momento
  Dado que existe uma reserva confirmada para amanhã, paga via Pix
  Quando o administrador cancelar a reserva
  Então a reserva passa para "Cancelada" e o valor é reembolsado
```

### HU07 — Administrar quiosques
> **Como** administrador, **quero** cadastrar e manter os quiosques, **para** que os clientes vejam informações e preços atualizados.

**Requisitos:** RF16 · **Regras:** RN15

```gherkin
Cenário: Cadastrar quiosque
  Quando o administrador cadastrar o "Quiosque G" com capacidade, valor, fotos, itens e regras de uso
  Então o Quiosque G aparece na lista de quiosques para os clientes

Cenário: Editar quiosque
  Quando o administrador alterar o valor da diária do Quiosque G para R$ 180,00
  Então novas reservas do Quiosque G custam R$ 180,00
  E reservas já feitas mantêm o valor do momento da reserva

Cenário: Desativar quiosque
  Quando o administrador desativar o Quiosque G
  Então o Quiosque G deixa de aparecer para os clientes

Cenário: Excluir quiosque com reservas
  Dado que o Quiosque A tem reservas
  Quando o administrador tentar excluir o Quiosque A
  Então o sistema informa que o quiosque tem reservas e deve ser desativado

Cenário: Excluir quiosque sem reservas
  Dado que o Quiosque G nunca teve reservas
  Quando o administrador excluir o Quiosque G
  Então o Quiosque G deixa de existir
```
