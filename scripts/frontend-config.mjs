import { createRequire } from 'node:module';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { dirname, join, resolve } from 'node:path';
import { homedir } from 'node:os';

try {
  const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
  const frontend = join(root, 'src', 'frontend');
  const require = createRequire(join(frontend, 'package.json'));
  const { loadEnv } = await import(pathToFileURL(join(dirname(require.resolve('vite/package.json')), 'dist/node/index.js')).href);
  const env = { ...loadEnv('development', frontend, ''), ...process.env };
  if ((env.VITE_API_URL || 'https://localhost:7099').replace(/\/$/, '') !== 'https://localhost:7099') {
    throw new Error('VITE_API_URL deve apontar para https://localhost:7099; override existente preservado.');
  }
  const cert = env.SUPPORTATOS_HTTPS_CERT || join(homedir(), '.aspnet', 'https', 'supportatos-frontend.pem');
  const key = env.SUPPORTATOS_HTTPS_KEY || join(homedir(), '.aspnet', 'https', 'supportatos-frontend.key');
  console.log(JSON.stringify({ cert: resolve(frontend, cert), key: resolve(frontend, key), custom: Boolean(env.SUPPORTATOS_HTTPS_CERT || env.SUPPORTATOS_HTTPS_KEY) }));
} catch {
  console.error('Vite: execute npm ci e confira VITE_API_URL/overrides HTTPS. Nenhum valor sensivel foi exibido.');
  process.exitCode = 1;
}
