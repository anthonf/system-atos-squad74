export function mascararEmail(email: string): string {
  const [user, domain] = email.split('@');
  if (!domain) return '***';
  const visivel = user.slice(0, 2);
  return `${visivel}***@${domain}`;
}

export function iniciais(nome: string): string {
  return nome
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase())
    .join('');
}
