import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    // Lyssnar på alla nätverkskort, så att mobilerna når sidan via laptopens IP.
    host: true,
    // QR-koden pekar på 5173, så porten får inte flytta sig om den är upptagen.
    port: 5173,
    strictPort: true,
    // Mobilerna pratar bara med Vite. Hubben skickas vidare till backenden, så ingen CORS behövs.
    proxy: {
      '/gamehub': {
        target: 'http://localhost:5080',
        ws: true,
      },
    },
  },
});
