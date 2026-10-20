import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

// Em desenvolvimento, /api é repassado para a API .NET (dotnet run, porta 5091),
// então o navegador fala com uma origem só e não é preciso configurar CORS.
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      '/api': process.env.API_URL ?? 'http://localhost:5091',
      // Fotos de quiosque enviadas pelo admin (servidas pela API).
      '/uploads': process.env.API_URL ?? 'http://localhost:5091',
    },
  },
});
