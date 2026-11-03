-- Dados de exemplo para testes manuais da API.
-- Execute APÓS o banco estar com o schema criado (dotnet ef database update).

INSERT INTO grupo_empresa (codigo_externo, nome, tipo, criado_em)
VALUES ('grupo-empresa-demo', 'Empresa Demo', 'matriz', now())
ON CONFLICT (codigo_externo) DO NOTHING;

INSERT INTO sla_config (produto, categoria, prioridade, tempo_resposta_min, tempo_resolucao_min)
VALUES
    ('Portal', 'Login', 'Alta', 1, 4),
    ('Portal', 'Erro de sistema', 'Media', 2, 8),
    ('Portal', 'Duvida', 'Baixa', 4, 24)
ON CONFLICT (produto, categoria, prioridade) DO NOTHING;

INSERT INTO usuario (codigo_externo, nome, email, grupo_empresa_id, papel, criado_em)
SELECT 'portal-demo', 'Cliente Demo', 'cliente@demo.com', g.id, 'Cliente', now()
FROM grupo_empresa g
WHERE g.codigo_externo = 'grupo-empresa-demo'
ON CONFLICT (email) DO NOTHING;