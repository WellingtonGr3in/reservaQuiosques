# ADR-004: Reserva sem Login Obrigatório — Aprovado

## Contexto

A proposta inicial exigia cadastro e login para reservar. Porém, a maioria das pessoas reserva um quiosque poucas vezes por ano, e obrigar a criação de conta adiciona etapas e faz o usuário desistir.

Sem login, ainda é preciso identificar quem reservou, permitir que a pessoa consulte ou cancele depois e impedir que outra pessoa mexa na reserva dela.

## Decisão

A reserva pode ser feita **sem conta**:

1. O cliente é identificado pelo **CPF** (validado). Toda reserva fica ligada ao cliente.
2. A reserva só é confirmada após o **pagamento via Pix** (RN06).
3. Para consultar ou cancelar sem login, é preciso o **código da reserva + CPF** (RN10). Só o CPF não basta, porque CPF não é informação secreta.
4. A **conta é opcional**. Se o cliente criar uma depois, as reservas antigas daquele CPF aparecem no histórico (RN09).

## Consequências

**Vantagens**
- Fluxo de reserva mais curto (RNF08), o que resolve o problema do processo manual sem criar uma nova barreira.
- O CPF permite aplicar limites por pessoa (RN08).
- Quem cria conta depois não perde nada.

**Desvantagens**
- Menos seguro que login: quem souber o código e o CPF consegue acessar a reserva. Mitigado com limite de tentativas (RNF06) e envio do código só para o e-mail informado.
- Coletar CPF exige cuidado com a LGPD (mascaramento, RNF07).
- O pagamento passa a fazer parte do MVP, exigindo integração com um gateway Pix (ex.: Mercado Pago em modo sandbox).

**Alternativas descartadas:** login obrigatório (mais fricção) e acesso só pelo CPF (inseguro).
