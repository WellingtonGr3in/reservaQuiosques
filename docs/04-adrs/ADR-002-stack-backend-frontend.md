# ADR-002: Escolha da Stack (Backend .NET + Frontend React) — Aprovado

## Contexto

O sistema precisa de uma API para as regras de reserva e pagamento e de uma interface web responsiva, com calendário interativo e tela de pagamento com contador de tempo. A equipe tem um semestre de prazo e precisa de ferramentas que já conheça ou que tenham boa documentação.

## Decisão

- **Backend:** API REST em **.NET 9 (ASP.NET Core)** com Entity Framework Core, organizada em camadas (Domínio, Aplicação, Infraestrutura, API).
- **Frontend:** **React + TypeScript** como SPA (Single Page Application), consumindo a API.

## Consequências

**Vantagens**
- Parte da equipe já usa .NET e React profissionalmente, o que acelera o desenvolvimento.
- Separar frontend e API permite desenvolver e publicar cada parte de forma independente.
- TypeScript reduz erros na integração com a API.
- .NET tem recurso nativo para tarefas em segundo plano, usado para expirar reservas não pagas (RN05).

**Desvantagens**
- Membros que só conhecem Java terão curva de aprendizado no backend.
- Validações ficam duplicadas: no frontend (para o usuário) e no backend (para garantir a regra).
- Exige configurar CORS entre frontend e API.

**Alternativas descartadas:** Spring Boot (mais alinhado ao curso, mas menor experiência prática da equipe) e Next.js (renderização no servidor não é necessária, pois o sistema não depende de SEO).
