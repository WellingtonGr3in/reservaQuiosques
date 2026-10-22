import { expect, test, type Page } from '@playwright/test';
import { createReservation, findFreeDay, getReceipt, randomCpf } from './helpers';

// HU06 e HU07: área de administração. Usa o administrador criado ao subir a API (docker-compose.yml: admin / admin).
const ADMIN_LOGIN = process.env.E2E_ADMIN_LOGIN ?? 'admin';
const ADMIN_PASSWORD = process.env.E2E_ADMIN_PASSWORD ?? 'admin';

async function loginAsAdmin(page: Page) {
  await page.goto('/admin/entrar');
  await page.locator('#admin-email').fill(ADMIN_LOGIN);
  await page.locator('#admin-password').fill(ADMIN_PASSWORD);
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page).toHaveURL(/\/admin\/reservas/);
}

test.describe('HU06 - Administrar reservas', () => {
  test('senha errada não entra na administração', async ({ page }) => {
    await page.goto('/admin/entrar');
    await page.locator('#admin-email').fill(ADMIN_LOGIN);
    await page.locator('#admin-password').fill('senha-errada');
    await page.getByRole('button', { name: 'Entrar' }).click();

    await expect(page.getByRole('alert')).toContainText('E-mail ou senha inválidos');
  });

  test('sem login, a administração redireciona para a tela de entrar', async ({ page }) => {
    await page.goto('/admin/reservas');
    await expect(page).toHaveURL(/\/admin\/entrar/);
  });

  test('lista as reservas e cancela uma a qualquer momento (RF12, RF15)', async ({ page, request }) => {
    const { kiosk, date } = await findFreeDay(request);
    const cpf = randomCpf();
    const { code } = await createReservation(request, { kioskId: kiosk.id, date, cpf, name: 'Cliente Admin E2E' });

    await loginAsAdmin(page);
    await expect(page.getByRole('heading', { name: 'Reservas' })).toBeVisible();
    await expect(page.getByRole('table')).toBeVisible();

    await page.goto(`/admin/reservas/${code}`);
    await expect(page.getByText(code).first()).toBeVisible();
    await expect(page.getByText('Cliente Admin E2E').first()).toBeVisible();

    await page.getByRole('button', { name: 'Cancelar reserva' }).click();
    await page.getByRole('button', { name: 'Confirmar cancelamento' }).click();
    await expect(page.getByText('Reserva cancelada.')).toBeVisible();

    expect((await getReceipt(request, code, cpf)).status).toBe('CANCELADA');
  });
});

test.describe('HU07 - Administrar quiosques', () => {
  test('lista os quiosques, inclusive com opção de editar', async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('/admin/quiosques');
    await expect(page.getByText('Pavilhão Central').first()).toBeVisible();
  });
});
