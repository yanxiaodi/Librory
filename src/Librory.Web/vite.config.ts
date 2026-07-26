import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { fileURLToPath } from 'url'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  build: {
    outDir: 'build',
    emptyOutDir: false,
  },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: Number(process.env.VITE_PORT ?? 5180),
    proxy: {
      '/api': {
        target: process.env.LIBRORY_API_URL ?? 'http://localhost:5178',
        changeOrigin: true,
        secure: false,
      },
      '/auth': {
        target: process.env.LIBRORY_API_URL ?? 'http://localhost:5178',
        changeOrigin: true,
        secure: false,
      },
      '/signin-google': {
        target: process.env.LIBRORY_API_URL ?? 'http://localhost:5178',
        changeOrigin: true,
        secure: false,
      },
      '/signin-microsoft': {
        target: process.env.LIBRORY_API_URL ?? 'http://localhost:5178',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
