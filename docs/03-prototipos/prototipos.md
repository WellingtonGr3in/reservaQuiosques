#  Protótipo de Interface

https://www.figma.com/design/XcScmvykeXCxQF483gxKgR/Sem-t%25C3%25ADtulo?node-id=0-1&p=f&t=3tnbNGrZ8H2WO1nX-0

---

#  Telas do Protótipo

##  Entrar / Criar Conta

<p align="center">
  <img src="../imagens/entrar-criar-conta.png" width="800">
</p>

---

##  Lista de Quiosques

<p align="center">
  <img src="../imagens/lista-quisques.png" width="800">
</p>

---

##  Detalhe do Quiosque

<p align="center">
  <img src="../imagens/detalhe-do-quiosque.png" width="800">
</p>

---

##  Pagamento

<p align="center">
  <img src="../imagens/pagamento.png" width="800">
</p>

---

##  Comprovante de Reserva

<p align="center">
  <img src="../imagens/comprovante-reserva.png" width="800">
</p>

---

##  Minhas Reservas

<p align="center">
  <img src="../imagens/minhas-reservas.png" width="800">
</p>

---

##  Consulta de Reserva

<p align="center">
  <img src="../imagens/consulta-reserva.png" width="800">
</p>

---

##  Seus Dados

<p align="center">
  <img src="../imagens/seus-status.png" width="800">
</p>

---

#  Elementos fora do escopo do MVP

Os elementos abaixo aparecem no protótipo, mas não fazem parte do MVP porque não há requisito funcional para eles:

| Elemento | Tela | Tratamento no MVP |
|---|---|---|
| "Esqueceu a senha?" | Entrar / Criar Conta | Não implementado |
| "Falar com suporte" | Minhas Reservas | Não implementado |
| "Regulamento" / "Ler termos completos" | Lista e Minhas Reservas | Não implementado |
| "Imprimir comprovante / Salvar PDF" e "Baixar comprovante" | Comprovante e Consulta | Usa a impressão do próprio navegador; o sistema não gera PDF |
| Aceite do regulamento | Seus Dados | Obrigatório marcar para continuar, mas o aceite não é gravado |

O horário de uso exibido nas telas é sempre **08:00 às 22:00** (RN02). A tela "Lista de Quiosques" mostra 09:00 e deve ser corrigida no Figma.

---

#  Área de administração (HU06, HU07)

As telas da administração seguem o mesmo visual das telas do cliente e ainda não têm protótipo no Figma. Acesso por `/admin` (link "Administração" no rodapé), com login próprio (RF11).

| Tela | Rota | O que faz |
|---|---|---|
| Entrar | `/admin/entrar` | Login do administrador com e-mail e senha |
| Reservas | `/admin/reservas` | Lista todas as reservas com filtros por período, quiosque, status e CPF (RF12) |
| Nova reserva | `/admin/reservas/nova` | Cria reserva para qualquer cliente e data, "paga na administração" ou aguardando Pix (RF13, RN13, RN14) |
| Detalhe da reserva | `/admin/reservas/{código}` | Dados e contato do cliente, alterar quiosque/data (RF14) e cancelar a qualquer momento (RF15) |
| Quiosques | `/admin/quiosques` | Lista todos os quiosques, inclusive desativados; excluir só quem nunca teve reservas (RF16, RN15) |
| Cadastro/edição de quiosque | `/admin/quiosques/novo` e `/admin/quiosques/{id}` | Nome, descrição, capacidade, diária, regras de uso, ativo, itens, foto de capa e galeria; as fotos são enviadas do computador pelos botões "Escolher imagem" e "Adicionar fotos" (RF16) |
