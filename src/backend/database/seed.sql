-- Dados de exemplo para testes manuais da API.
-- Execute APÓS o banco estar com o schema criado (dotnet ef database update).

INSERT INTO grupo_empresa (nome) VALUES ('Empresa Demo') ON CONFLICT DO NOTHING;

INSERT INTO sla_categoria (produto, categoria, prioridade, tempo_resposta, tempo_resolucao)
VALUES
    ('Portal', 'Login', 'Alta', 1, 4),
    ('Portal', 'Erro de sistema', 'Media', 2, 8),
    ('Portal', 'Duvida', 'Baixa', 4, 24)
ON CONFLICT DO NOTHING;

INSERT INTO usuario (nome, email, grupo_empresa_id, papel)
VALUES ('Cliente Demo', 'cliente@demo.com', 1, 'Cliente')
ON CONFLICT (email) DO NOTHING;