# ADR-001: Escolha do Banco de Dados Relacional — Aprovado

## Contexto

Os dados do sistema são fortemente relacionados: cliente, quiosque, reserva e pagamento. O principal problema a resolver é a **reserva duplicada** (RN01): duas pessoas não podem reservar o mesmo quiosque na mesma data, nem mesmo se clicarem em "reservar" no mesmo instante.

Validar só no código (consultar se a data está livre e depois inserir) não resolve, porque duas requisições simultâneas podem consultar ao mesmo tempo, ambas verem a data livre e ambas inserirem.

## Decisão

Usaremos **PostgreSQL**.

A regra RN01 será garantida pelo próprio banco com um **índice único parcial**, que só considera as reservas ativas:

```sql
CREATE UNIQUE INDEX ux_reserva_quiosque_data_ativa
    ON reserva (quiosque_id, data_reserva)
    WHERE status IN ('PENDENTE_PAGAMENTO', 'CONFIRMADA');
```

Se duas reservas chegarem ao mesmo tempo, o banco aceita uma e rejeita a outra, e a aplicação devolve "Data indisponível". Reservas expiradas ou canceladas ficam fora do índice, então a data é liberada automaticamente.

A expiração (RN05) é feita por um job que roda a cada 1 minuto. Para que uma reserva pendente com o prazo já vencido não continue bloqueando o índice até o job rodar, a criação de uma reserva primeiro marca como `EXPIRADA` as reservas pendentes vencidas daquele quiosque e data, na mesma transação. O calendário também trata essas reservas como livres.

## Consequências

**Vantagens**
- Garantia real contra reservas duplicadas, independentemente de concorrência.
- Transações ACID e integridade referencial (chaves estrangeiras).
- Gratuito e com várias opções de hospedagem gerenciada.

**Desvantagens**
- Índice único parcial não existe em todos os bancos (ex.: MySQL), o que nos prende ao PostgreSQL.
- Parte da regra de negócio fica no banco, então precisa estar bem documentada para o time.

**Alternativas descartadas:** MySQL (não tem índice parcial) e MongoDB (dados são relacionais e a garantia de unicidade condicional é mais trabalhosa).
