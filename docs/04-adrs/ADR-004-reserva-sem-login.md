# ADR-004: Reserva sem Login Obrigatório — Aprovado

## Contexto

A proposta inicial exigia cadastro e login para reservar. Porém, a maioria das pessoas reserva um quiosque poucas vezes por ano, e obrigar a criação de conta adiciona etapas e faz o usuário desistir.

Sem login, ainda é preciso identificar quem reservou, permitir que a pessoa consulte ou cancele depois e impedir que outra pessoa mexa na reserva dela.

## Decisão

A reserva pode ser feita **sem conta**:

1. O cliente é identificado pelo **CPF** (validado). Toda reserva fica ligada ao cliente.
2. A reserva só é confirmada após o **pagamento via Pix** (RN06).
3. Para consultar sem login, basta o **CPF** do titular (RN10): o sistema mostra todas as reservas daquele CPF, e o cliente pode cancelar ou pagar cada uma. O cliente não precisa guardar o código da reserva. *(Revisado: a versão inicial exigia código + CPF; a equipe optou pela consulta só com o CPF por ser mais simples para o cliente, aceitando o risco descrito abaixo.)*
4. A **conta é opcional**. Para criá-la, o cliente informa o **CPF** de quem já fez ao menos uma reserva, além de um e-mail e uma senha para entrar; as reservas antigas daquele CPF aparecem no histórico (RN09).

## Consequências

**Vantagens**
- Fluxo de reserva mais curto (RNF08), o que resolve o problema do processo manual sem criar uma nova barreira.
- O CPF permite aplicar limites por pessoa (RN08).
- Quem cria conta depois não perde nada.

**Desvantagens**
- Menos seguro que login: CPF não é segredo, então quem souber o CPF de outra pessoa consegue ver as reservas dela (com o CPF mascarado, RNF07) e cancelá-las. Risco aceito conscientemente no MVP para simplificar a consulta. Mitigação: limite de 5 tentativas por minuto por IP (RNF06), que dificulta testar CPFs em massa. A evolução seria confirmar a posse do CPF com um código enviado para o e-mail usado nas reservas.
- Criar conta só com o CPF é simples para o usuário, mas CPF não é segredo: quem souber o CPF de outra pessoa que ainda não tem conta consegue criá-la antes, ver o histórico e cancelar reservas dela. Risco aceito conscientemente no MVP para simplificar o cadastro; a evolução seria confirmar a posse do CPF com um código enviado para o e-mail usado nas reservas.
- Coletar CPF exige cuidado com a LGPD (mascaramento, RNF07).
- O pagamento passa a fazer parte do MVP. No MVP o gateway Pix é simulado (o QR Code é um Pix real, mas a aprovação é feita pelo botão "Simular pagamento"), atrás da interface `IPaymentGateway`; trocar por um gateway real (ex.: Mercado Pago) exige uma URL pública para as notificações (ADR-003).

**Alternativas descartadas:** login obrigatório (mais fricção) e consulta com código + CPF (mais segura, mas obriga o cliente a guardar o código; foi a decisão inicial e acabou substituída pela consulta só com o CPF).
