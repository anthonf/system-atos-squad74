import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  // Prefer the current TypeScript components over their legacy JS copies.
  resolve: { extensions: ['.mjs', '.mts', '.ts', '.tsx', '.js', '.jsx', '.json'] },
  plugins: [react()],
  server: {
    port: 5173,
  },
});
