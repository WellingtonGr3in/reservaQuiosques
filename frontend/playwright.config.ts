import { defineConfig, devices } from '@playwright/test';

// Testes de integração ponta a ponta (frontend + API + banco), no navegador.
// Rodam contra o sistema completo do Docker (docker compose up -d --build), em http://localhost:8080.
// Para rodar contra o "npm run dev", use E2E_BASE_URL=http://localhost:3000.
export default defineConfig({
  testDir: './e2e',
  // Os testes compartilham o mesmo banco e o limite de 5 consultas por minuto por IP (RNF06), então rodam em sequência.
  workers: 1,
  fullyParallel: false,
  timeout: 60_000,
  expect: { timeout: 10_000 },
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:8080',
    locale: 'pt-BR',
    timezoneId: 'America/Sao_Paulo',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
});
