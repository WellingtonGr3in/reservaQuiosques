# ADR-003: Escolha da Hospedagem (PaaS gratuito) — Aprovado

## Contexto

O projeto é acadêmico e não tem orçamento. Mesmo assim, o sistema precisa estar acessível na internet com HTTPS (RNF05), principalmente porque o gateway de pagamento precisa de uma URL pública para avisar quando um Pix é pago.

## Decisão

Usaremos serviços **PaaS com plano gratuito**:

| Parte | Serviço |
|---|---|
| Frontend (React) | Vercel |
| API (.NET, em container Docker) | Render |
| Banco (PostgreSQL) | Neon |

O deploy é automático a cada push na branch `main` do GitHub.

## Consequências

**Vantagens**
- Custo zero, HTTPS automático e deploy simples.
- Como a API roda em Docker, é fácil trocar de provedor se precisar.

**Desvantagens**
- No plano gratuito, a API "dorme" após um tempo sem uso, e a primeira requisição fica lenta (pode violar RNF01).
- Limites de recursos e nenhuma garantia formal de disponibilidade.

**Mitigação:** um monitor (ex.: UptimeRobot) faz uma requisição a cada 5 minutos para manter a API ativa.

**Alternativas descartadas:** AWS/Azure (configuração complexa e risco de cobrança) e VPS própria (exige manter servidor, certificados e backups).
