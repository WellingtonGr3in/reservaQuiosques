# 2. Requisitos Não Funcionais

| ID | Categoria | Requisito |
|---|---|---|
| RNF01 | Desempenho | A consulta do calendário de um quiosque deve responder em menos de 1s em 95% das requisições, com até 50 usuários simultâneos. |
| RNF02 | Confiabilidade | Com 100 requisições simultâneas para o mesmo quiosque e data, deve ser criada exatamente 1 reserva. |
| RNF03 | Confiabilidade | Se o gateway enviar a mesma confirmação de pagamento mais de uma vez, a reserva deve ser confirmada apenas uma vez. |
| RNF04 | Segurança | Senhas devem ser armazenadas com hash bcrypt (custo mínimo 12) e a sessão (JWT) deve expirar em no máximo 60 minutos. |
| RNF05 | Segurança | Toda comunicação deve usar HTTPS. |
| RNF06 | Segurança | A consulta por código + CPF deve aceitar no máximo 5 tentativas por minuto por IP. |
| RNF07 | Privacidade (LGPD) | O CPF deve aparecer mascarado (ex.: `***.982.247-**`) em telas, comprovantes e e-mails. |
| RNF08 | Usabilidade | O cliente deve concluir uma reserva sem conta passando por no máximo 4 telas da lista de quiosques até o pagamento (lista, detalhe, dados e pagamento), sem contar o comprovante. |
| RNF09 | Usabilidade | A interface deve funcionar em telas a partir de 360px de largura (celular). |
| RNF10 | Disponibilidade | O sistema deve ter disponibilidade mínima de 99% ao mês. |
