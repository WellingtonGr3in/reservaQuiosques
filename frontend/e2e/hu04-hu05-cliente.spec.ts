import { expect, test } from '@playwright/test';
import { createReservation, findFreeDay, formatCpf, getReceipt, maskedCpf, randomCpf } from './helpers';

// HU04 (consultar só pelo CPF e cancelar sem conta) e HU05 (conta e histórico), da tela até o banco.
// A consulta e o cancelamento sem login aceitam 5 tentativas por minuto por IP (RNF06):
// estes testes fazem 3 no total para não estourar o limite.

test.describe('HU04 - Consultar e cancelar reserva', () => {
  test('consulta só pelo CPF, lista as reservas e cancela uma aguardando pagamento', async ({ page, request }) => {
    const cpf = randomCpf();
    const { kiosk, date } = await findFreeDay(request);
    const { code } = await createReservation(request, { kioskId: kiosk.id, date, cpf });
    const other = await findFreeDay(request);
    const { code: otherCode } = await createReservation(request, { kioskId: other.kiosk.id, date: other.date, cpf });

    await page.goto('/');
    await page.getByRole('link', { name: /Consultar/ }).first().click();
    await expect(page.getByRole('heading', { name: 'Consultar reserva' })).toBeVisible();

    await page.locator('#cpf-titular').fill(formatCpf(cpf));
    await page.getByRole('button', { name: 'Consultar' }).click();

    // As duas reservas do CPF, com o CPF mascarado (RNF07).
    await expect(page.getByRole('heading', { name: '2 reservas encontradas' })).toBeVisible();
    await expect(page.getByRole('heading', { name: `Reserva ${code}` })).toBeVisible();
    await expect(page.getByRole('heading', { name: `Reserva ${otherCode}` })).toBeVisible();
    await expect(page.getByText(kiosk.name).first()).toBeVisible();
    await expect(page.getByText(maskedCpf(cpf)).first()).toBeVisible();
    await expect(page.getByText('Aguardando pagamento').first()).toBeVisible();

    // RN07: pendente é cancelada sem cobrança.
    const card = page.locator('div.rounded-xl').filter({ has: page.getByRole('heading', { name: `Reserva ${code}` }) });
    await card.getByRole('button', { name: 'Cancelar reserva' }).click();
    await expect(page.getByRole('heading', { name: 'Confirmar cancelamento' })).toBeVisible();
    await expect(page.getByText('A reserva ainda não foi paga, então nada será cobrado.')).toBeVisible();
    await page.getByRole('button', { name: 'Sim, cancelar reserva' }).click();

    await expect(page.getByText(`Reserva ${code} cancelada.`)).toBeVisible();
    expect((await getReceipt(request, code, cpf)).status).toBe('CANCELADA');
    expect((await getReceipt(request, otherCode, cpf)).status).toBe('PENDENTE_PAGAMENTO');
  });

  test('CPF sem reservas mostra "Não encontramos reservas com este CPF"', async ({ page }) => {
    await page.goto('/consultar');
    await page.locator('#cpf-titular').fill(formatCpf(randomCpf()));
    await page.getByRole('button', { name: 'Consultar' }).click();

    await expect(page.getByRole('alert')).toContainText('Não encontramos reservas com este CPF');
  });
});

test.describe('HU05 - Criar conta e ver histórico', () => {
  test('conta herda as reservas feitas antes com o mesmo CPF', async ({ page, request }) => {
    const cpf = randomCpf();
    const first = await findFreeDay(request);
    const { code } = await createReservation(request, { kioskId: first.kiosk.id, date: first.date, cpf });

    await page.goto('/entrar');
    await page.getByRole('tab', { name: 'Criar conta' }).click();
    await page.locator('#register-cpf').fill(cpf);
    await page.locator('#register-email').fill(`e2e.${cpf}@exemplo.com`);
    await page.locator('#register-password').fill('SenhaE2E123');
    await page.locator('form').getByRole('button', { name: 'Criar conta' }).click();

    // Minhas reservas (RF10) já mostra a reserva feita sem conta (RN09).
    await expect(page).toHaveURL(/minhas-reservas/);
    await expect(page.getByText(code).first()).toBeVisible();
    await expect(page.getByText(maskedCpf(cpf)).first()).toBeVisible();
  });

  test('CPF sem reservas não pode criar conta (RN09)', async ({ page }) => {
    await page.goto('/entrar');
    await page.getByRole('tab', { name: 'Criar conta' }).click();
    await page.locator('#register-cpf').fill(randomCpf());
    await page.locator('#register-email').fill('sem.reserva@exemplo.com');
    await page.locator('#register-password').fill('SenhaE2E123');
    await page.locator('form').getByRole('button', { name: 'Criar conta' }).click();

    await expect(page.getByRole('alert')).toContainText('Não encontramos reservas com este CPF');
  });

  test('login com senha errada mostra erro', async ({ page }) => {
    await page.goto('/entrar');
    await page.locator('#login-email').fill('naoexiste@exemplo.com');
    await page.locator('#login-password').fill('senhaErrada1');
    await page.locator('form').getByRole('button', { name: 'Entrar' }).click();

    await expect(page.getByRole('alert')).toContainText('E-mail ou senha inválidos');
  });
});
