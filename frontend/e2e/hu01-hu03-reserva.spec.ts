import { expect, test } from '@playwright/test';
import { createReservation, findFreeDay, formatCpf, getCalendarDay, getReceipt, maskedCpf, randomCpf } from './helpers';

// HU01, HU02 e HU03 de ponta a ponta: a tela chama a API, que grava no banco, e o resultado volta para a tela.

test.describe('HU01 - Ver quiosques e disponibilidade', () => {
  test('lista os quiosques ativos e abre o detalhe com calendário', async ({ page }) => {
    await page.goto('/');

    await expect(page.getByRole('heading', { name: 'Quiosques disponíveis' })).toBeVisible();
    const cards = page.getByRole('link', { name: 'Ver detalhes e reservar' });
    await expect(cards.first()).toBeVisible();
    expect(await cards.count()).toBeGreaterThan(0);
    await expect(page.getByText(/quiosques ativos no condomínio/)).toBeVisible();

    await cards.first().click();

    // RF02: descrição, itens, fotos e regras de uso.
    await expect(page.getByRole('heading', { name: 'Itens inclusos' })).toBeVisible();
    await expect(page.getByText('Regras de uso')).toBeVisible();
    await expect(page.getByText('08:00 às 22:00').first()).toBeVisible();

    // RF03: calendário com dias livres; sem dia escolhido, não dá para reservar.
    await expect(page.getByText('Selecione o dia')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Reservar agora' })).toBeDisabled();
  });

  test('dia ocupado aparece bloqueado no calendário (RN01)', async ({ page, request }) => {
    const { kiosk, date } = await findFreeDay(request);
    await createReservation(request, { kioskId: kiosk.id, date, cpf: randomCpf() });
    expect(await getCalendarDay(request, kiosk.id, date)).toBe('OCUPADO');

    await page.goto(`/quiosques/${kiosk.id}`);
    await goToMonth(page, date);
    const day = String(Number(date.slice(8, 10)));
    await expect(page.getByRole('button', { name: day, exact: true })).toBeDisabled();
    await expect(page.getByRole('button', { name: day, exact: true })).toHaveAttribute('title', 'Ocupado');
  });
});

test.describe('HU02 e HU03 - Reservar sem conta, pagar e receber o comprovante', () => {
  test('fluxo completo: lista, detalhe, dados, Pix e comprovante', async ({ page, request }) => {
    const { kiosk, date } = await findFreeDay(request);
    const cpf = randomCpf();

    // Tela 1: lista
    await page.goto('/');
    await page
      .getByRole('article')
      .filter({ has: page.getByRole('heading', { name: kiosk.name, exact: true }) })
      .getByRole('link', { name: 'Ver detalhes e reservar' })
      .click();

    // Tela 2: detalhe e escolha do dia no calendário
    await expect(page.getByRole('heading', { level: 1, name: kiosk.name })).toBeVisible();
    await goToMonth(page, date);
    await page.getByRole('button', { name: String(Number(date.slice(8, 10))), exact: true }).click();
    await page.getByRole('button', { name: 'Reservar agora' }).click();

    // Tela 3: seus dados
    await expect(page.getByRole('heading', { name: 'Identificação do responsável' })).toBeVisible();
    await page.getByLabel('CPF').fill(cpf);
    await expect(page.getByLabel('CPF')).toHaveValue(formatCpf(cpf)); // máscara aplicada na digitação
    await page.getByLabel('Nome completo').fill('Maria E2E');
    await page.getByLabel('E-mail').fill('maria.e2e@exemplo.com');
    await page.getByLabel('Telefone / WhatsApp').fill('47999990000');
    await page.getByLabel(/Declaro que li e concordo/).check();
    await page.getByRole('button', { name: 'Ir para o pagamento' }).click();

    // Tela 4: pagamento Pix (RF05) com QR Code, copia-e-cola e tempo restante (RN05)
    await expect(page.getByRole('heading', { name: 'Pagamento via Pix' })).toBeVisible();
    await expect(page.getByText('Tempo restante:')).toBeVisible();
    await expect(page.locator('svg').filter({ has: page.locator('path') }).first()).toBeVisible();
    const code = page.url().match(/reservas\/(QSK-[A-Z0-9]{6})\/pagamento/)![1];

    // Enquanto aguarda o pagamento, a reserva está pendente no banco e a data fica bloqueada.
    expect((await getReceipt(request, code, cpf)).status).toBe('PENDENTE_PAGAMENTO');
    expect(await getCalendarDay(request, kiosk.id, date)).toBe('OCUPADO');

    // "Já paguei" antes de pagar: mensagem informativa, sem avançar.
    await page.getByRole('button', { name: 'Já paguei' }).click();
    await expect(page.getByRole('status')).toContainText('Ainda não identificamos o pagamento');

    // RF06: o gateway simulado aprova o Pix e a tela avança sozinha para o comprovante.
    await page.getByRole('button', { name: 'Simular pagamento do Pix' }).click();

    // Comprovante (RF07): código e CPF mascarado (RNF07).
    await expect(page.getByRole('heading', { name: 'Reserva confirmada!' })).toBeVisible();
    await expect(page.getByText(code).first()).toBeVisible();
    await expect(page.getByText(maskedCpf(cpf))).toBeVisible();
    await expect(page.getByText(formatCpf(cpf))).toHaveCount(0);

    // No banco, a reserva ficou confirmada.
    expect((await getReceipt(request, code, cpf)).status).toBe('CONFIRMADA');
  });

  test('CPF inválido mostra a mensagem da API (RN04)', async ({ page, request }) => {
    const { kiosk, date } = await findFreeDay(request);
    await page.goto(`/quiosques/${kiosk.id}/reservar?data=${date}`);

    await page.getByLabel('CPF').fill('11111111111');
    await page.getByLabel('Nome completo').fill('Teste');
    await page.getByLabel('E-mail').fill('teste@exemplo.com');
    await page.getByLabel('Telefone / WhatsApp').fill('47999990000');
    await page.getByLabel(/Declaro que li e concordo/).check();
    await page.getByRole('button', { name: 'Ir para o pagamento' }).click();

    await expect(page.getByRole('alert')).toContainText('CPF inválido');
    await expect(page).toHaveURL(/\/reservar/);
  });

  test('campos obrigatórios e e-mail inválido são barrados no formulário', async ({ page, request }) => {
    const { kiosk, date } = await findFreeDay(request);
    await page.goto(`/quiosques/${kiosk.id}/reservar?data=${date}`);

    let apiCalled = false;
    page.on('request', (req) => {
      if (req.url().includes('/api/reservas') && req.method() === 'POST') apiCalled = true;
    });

    await page.getByRole('button', { name: 'Ir para o pagamento' }).click();
    expect(await page.getByLabel('CPF').evaluate((el: HTMLInputElement) => el.validity.valueMissing)).toBe(true);

    await page.getByLabel('CPF').fill(randomCpf());
    await page.getByLabel('Nome completo').fill('Teste');
    await page.getByLabel('E-mail').fill('email-sem-arroba');
    await page.getByLabel('Telefone / WhatsApp').fill('47999990000');
    await page.getByLabel(/Declaro que li e concordo/).check();
    await page.getByRole('button', { name: 'Ir para o pagamento' }).click();
    expect(await page.getByLabel('E-mail').evaluate((el: HTMLInputElement) => el.validity.typeMismatch)).toBe(true);

    expect(apiCalled).toBe(false);
    await expect(page).toHaveURL(/\/reservar/);
  });

  test('data já reservada mostra "Data indisponível" (RN01)', async ({ page, request }) => {
    const { kiosk, date } = await findFreeDay(request);
    await createReservation(request, { kioskId: kiosk.id, date, cpf: randomCpf() });

    await page.goto(`/quiosques/${kiosk.id}/reservar?data=${date}`);
    await page.getByLabel('CPF').fill(randomCpf());
    await page.getByLabel('Nome completo').fill('Outro Cliente');
    await page.getByLabel('E-mail').fill('outro@exemplo.com');
    await page.getByLabel('Telefone / WhatsApp').fill('47988887777');
    await page.getByLabel(/Declaro que li e concordo/).check();
    await page.getByRole('button', { name: 'Ir para o pagamento' }).click();

    const alert = page.getByRole('alert');
    await expect(alert).toContainText('Data indisponível');
    await expect(alert.getByRole('link', { name: 'Escolher outro dia' })).toBeVisible();
  });

  test('limite de 2 reservas ativas por CPF (RN08)', async ({ page, request }) => {
    const cpf = randomCpf();
    for (let i = 0; i < 2; i++) {
      const free = await findFreeDay(request);
      await createReservation(request, { kioskId: free.kiosk.id, date: free.date, cpf });
    }

    const { kiosk, date } = await findFreeDay(request);
    await page.goto(`/quiosques/${kiosk.id}/reservar?data=${date}`);
    await page.getByLabel('CPF').fill(cpf);
    await page.getByLabel('Nome completo').fill('Cliente E2E');
    await page.getByLabel('E-mail').fill('e2e@exemplo.com');
    await page.getByLabel('Telefone / WhatsApp').fill('47999990000');
    await page.getByLabel(/Declaro que li e concordo/).check();
    await page.getByRole('button', { name: 'Ir para o pagamento' }).click();

    await expect(page.getByRole('alert')).toContainText('Limite de 2 reservas ativas por CPF atingido');
  });
});

// Avança o calendário até o mês da data (formato "Novembro 2026").
async function goToMonth(page: import('@playwright/test').Page, date: string) {
  const [year, month] = date.split('-').map(Number);
  const label = new Date(year, month - 1, 1).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' }).replace(' de ', ' ');
  const title = label.charAt(0).toUpperCase() + label.slice(1);
  for (let i = 0; i < 4; i++) {
    if ((await page.getByRole('heading', { level: 2, name: title }).count()) > 0) break;
    await page.getByRole('button', { name: 'Próximo mês' }).click();
  }
  await expect(page.getByRole('heading', { level: 2, name: title })).toBeVisible();
  // Espera o calendário do mês carregar.
  await expect(page.getByRole('button', { name: String(Number(date.slice(8, 10))), exact: true })).toBeVisible();
}
