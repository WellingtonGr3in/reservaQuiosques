import { expect, test } from '@playwright/test';

// RNF09: a interface funciona em telas a partir de 360px (celular), sem rolagem horizontal.
test.use({ viewport: { width: 360, height: 780 } });

for (const path of ['/', '/consultar', '/entrar']) {
  test(`tela ${path} cabe em 360px de largura`, async ({ page }) => {
    await page.goto(path);
    await expect(page.locator('h1').first()).toBeVisible();
    const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
    expect(scrollWidth).toBeLessThanOrEqual(360);
  });
}
