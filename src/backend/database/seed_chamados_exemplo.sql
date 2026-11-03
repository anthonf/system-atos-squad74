-- Seed de exemplo: 8 chamados variados + interacoes.
-- Como usar pelo pgAdmin (sem terminal):
--   1. Abra o pgAdmin e conecte em SupportAtos Nativo (localhost:5432, db supportatos).
--   2. Databases > supportatos > botao direito > Query Tool.
--   3. Abra este arquivo (icone pasta) e aperte F5 (Execute).
--   4. Pode rodar quantas vezes quiser (idempotente por codigo_publico + mensagem).
-- Pre-requisitos: grupo_empresa 1, usuarios 1/3/5 (clientes) e 2/6 (agentes), sla_config 1/2/3.

-- 1) CHAMADOS ---------------------------------------------------------------
INSERT INTO chamado (codigo_publico, usuario_id, grupo_empresa_id, agente_id, sla_config_id, produto, categoria, titulo, descricao, status, prioridade, sla_prazo_resposta, sla_prazo_resolucao, aguardando_desde, criado_em, atualizado_em, fechado_em)
VALUES
-- Aberto, sem agente, no prazo
('DEMO-0001', 1, 1, NULL, 1, 'Portal', 'Login', 'Nao consigo fazer login', 'Cliente relata erro 401 ao tentar entrar no portal com email corporativo.', 'Aberto', 'Alta', NOW() + INTERVAL '2 hours', NOW() + INTERVAL '8 hours', NULL, NOW() - INTERVAL '1 hour', NOW() - INTERVAL '1 hour', NULL),
-- EmAndamento com agente
('DEMO-0002', 3, 1, 2, 2, 'Portal', 'Erro de sistema', 'Tela de extrato em branco', 'Ao abrir o extrato mensal a tela fica em branco no Chrome e Edge.', 'EmAndamento', 'Media', NOW() + INTERVAL '1 hour', NOW() + INTERVAL '6 hours', NULL, NOW() - INTERVAL '5 hours', NOW() - INTERVAL '30 minutes', NULL),
-- AguardandoCliente
('DEMO-0003', 5, 1, 6, 3, 'Portal', 'Duvida', 'Como emitir 2a via de boleto?', 'Cliente quer passo a passo para emitir segunda via dentro do portal.', 'AguardandoCliente', 'Baixa', NOW() + INTERVAL '3 hours', NOW() + INTERVAL '20 hours', NOW() - INTERVAL '2 hours', NOW() - INTERVAL '1 day', NOW() - INTERVAL '2 hours', NULL),
-- Resolvido
('DEMO-0004', 1, 1, 2, 1, 'Portal', 'Login', 'Reset de senha nao chega', 'Email de reset nao chegava; ajustado provedor e reenviado.', 'Resolvido', 'Alta', NOW() - INTERVAL '1 hour', NOW() + INTERVAL '2 hours', NULL, NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 hour', NULL),
-- Fechado
('DEMO-0005', 3, 1, 6, 2, 'Portal', 'Erro de sistema', 'Botao pagar travava', 'Corrigido na versao da semana passada; cliente confirmou.', 'Fechado', 'Media', NOW() - INTERVAL '2 days', NOW() - INTERVAL '1 day', NULL, NOW() - INTERVAL '3 days', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
-- Aberto SEM agente e com SLA ESTOURADO (para testar badge vermelho)
('DEMO-0006', 5, 1, NULL, 1, 'Portal', 'Login', 'Portal fora do ar (urgente)', 'Cliente reporta portal inacessivel desde ontem a noite.', 'Aberto', 'Alta', NOW() - INTERVAL '5 hours', NOW() - INTERVAL '1 hour', NULL, NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days', NULL),
-- EmAndamento com nota interna
('DEMO-0007', 1, 1, 2, 2, 'Portal', 'Erro de sistema', 'Relatorio com numeros divergentes', 'Total do relatorio nao bate com o extrato; investigacao em curso.', 'EmAndamento', 'Media', NOW() + INTERVAL '4 hours', NOW() + INTERVAL '12 hours', NULL, NOW() - INTERVAL '8 hours', NOW() - INTERVAL '15 minutes', NULL),
-- Aberto sem agente, duvida simples
('DEMO-0008', 3, 1, NULL, 3, 'Portal', 'Duvida', 'Onde altero meus dados?', 'Cliente pergunta onde atualiza telefone e endereco.', 'Aberto', 'Baixa', NOW() + INTERVAL '6 hours', NOW() + INTERVAL '24 hours', NULL, NOW() - INTERVAL '20 minutes', NOW() - INTERVAL '20 minutes', NULL)
ON CONFLICT (codigo_publico) DO NOTHING;

-- 2) INTERACOES --------------------------------------------------------------
-- DEMO-0001: pergunta inicial do cliente
INSERT INTO interacao (chamado_id, autor_id, tipo, mensagem, criado_em)
SELECT c.id, 1, 'Publica', 'Ola, nao consigo acessar o portal desde hoje cedo. Ja limpei o cache.', c.criado_em
FROM chamado c WHERE c.codigo_publico = 'DEMO-0001'
AND NOT EXISTS (SELECT 1 FROM interacao i JOIN chamado c2 ON c2.id = i.chamado_id WHERE c2.codigo_publico = 'DEMO-0001' AND i.mensagem LIKE 'Ola, nao consigo acessar o portal desde hoje%');

-- DEMO-0002: cliente + resposta da equipe
INSERT INTO interacao (chamado_id, autor_id, tipo, mensagem, criado_em)
SELECT c.id, 3, 'Publica', 'A tela de extrato abre em branco, testei em dois navegadores.', c.criado_em
FROM chamado c WHERE c.codigo_publico = 'DEMO-0002'
AND NOT EXISTS (SELECT 1 FROM interacao i JOIN chamado c2 ON c2.id = i.chamado_id WHERE c2.codigo_publico = 'DEMO-0002' AND i.mensagem LIKE 'A tela de extrato abre em branco%');
INSERT INTO interacao (chamado_id, autor_id, tipo, mensagem, criado_em)
SELECT c.id, 2, 'Publica', 'Recebido! Reproduzi aqui e abri investigacao no modulo de extrato. Atualizo em breve.', NOW() - INTERVAL '30 minutes'
FROM chamado c WHERE c.codigo_publico = 'DEMO-0002'
AND NOT EXISTS (SELECT 1 FROM interacao i JOIN chamado c2 ON c2.id = i.chamado_id WHERE c2.codigo_publico = 'DEMO-0002' AND i.mensagem LIKE 'Recebido! Reproduzi aqui%');

-- DEMO-0003: equipe aguardando cliente
INSERT INTO interacao (chamado_id, autor_id, tipo, mensagem, criado_em)
SELECT c.id, 6, 'Publica', 'Segue o passo a passo em anexo. Consegue confirmar se emitiu a 2a via?', NOW() - INTERVAL '2 hours'
FROM chamado c WHERE c.codigo_publico = 'DEMO-0003'
AND NOT EXISTS (SELECT 1 FROM interacao i JOIN chamado c2 ON c2.id = i.chamado_id WHERE c2.codigo_publico = 'DEMO-0003' AND i.mensagem LIKE 'Segue o passo a passo%');

-- DEMO-0004: resolvido com confirmacao
INSERT INTO interacao (chamado_id, autor_id, tipo, mensagem, criado_em)
SELECT c.id, 2, 'Publica', 'Provedor ajustado e email de reset reenviado. Por favor teste novamente.', NOW() - INTERVAL '2 hours'
FROM chamado c WHERE c.codigo_publico = 'DEMO-0004'
AND NOT EXISTS (SELECT 1 FROM interacao i JOIN chamado c2 ON c2.id = i.chamado_id WHERE c2.codigo_publico = 'DEMO-0004' AND i.mensagem LIKE 'Provedor ajustado%');
INSERT INTO interacao (chamado_id, autor_id, tipo, mensagem, criado_em)
SELECT c.id, 1, 'Publica', 'Testei e funcionou, obrigada!', NOW() - INTERVAL '1 hour'
FROM chamado c WHERE c.codigo_publico = 'DEMO-0004'
AND NOT EXISTS (SELECT 1 FROM interacao i JOIN chamado c2 ON c2.id = i.chamado_id WHERE c2.codigo_publico = 'DEMO-0004' AND i.mensagem LIKE 'Testei e funcionou%');

-- DEMO-0005: fechado
INSERT INTO interacao (chamado_id, autor_id, tipo, mensagem, criado_em)
SELECT c.id, 6, 'Publica', 'Correcao publicada. Chamado encerrado apos confirmacao do cliente.', NOW() - INTERVAL '1 day'
FROM chamado c WHERE c.codigo_publico = 'DEMO-0005'
AND NOT EXISTS (SELECT 1 FROM interacao i JOIN chamado c2 ON c2.id = i.chamado_id WHERE c2.codigo_publico = 'DEMO-0005' AND i.mensagem LIKE 'Correcao publicada%');

-- DEMO-0006: urgente sem resposta ainda
INSERT INTO interacao (chamado_id, autor_id, tipo, mensagem, criado_em)
SELECT c.id, 5, 'Publica', 'Portal segue fora do ar aqui, impacto total na operacao!', c.criado_em
FROM chamado c WHERE c.codigo_publico = 'DEMO-0006'
AND NOT EXISTS (SELECT 1 FROM interacao i JOIN chamado c2 ON c2.id = i.chamado_id WHERE c2.codigo_publico = 'DEMO-0006' AND i.mensagem LIKE 'Portal segue fora do ar%');

-- DEMO-0007: publica + NOTA INTERNA (so equipe ve)
INSERT INTO interacao (chamado_id, autor_id, tipo, mensagem, criado_em)
SELECT c.id, 1, 'Publica', 'Os numeros do relatorio de junho divergem do extrato em R$ 120,50.', c.criado_em
FROM chamado c WHERE c.codigo_publico = 'DEMO-0007'
AND NOT EXISTS (SELECT 1 FROM interacao i JOIN chamado c2 ON c2.id = i.chamado_id WHERE c2.codigo_publico = 'DEMO-0007' AND i.mensagem LIKE 'Os numeros do relatorio%');
INSERT INTO interacao (chamado_id, autor_id, tipo, mensagem, criado_em)
SELECT c.id, 2, 'NotaInterna', 'Suspeita de arredondamento no job noturno. Verificar com o time de dados antes de responder.', NOW() - INTERVAL '15 minutes'
FROM chamado c WHERE c.codigo_publico = 'DEMO-0007'
AND NOT EXISTS (SELECT 1 FROM interacao i JOIN chamado c2 ON c2.id = i.chamado_id WHERE c2.codigo_publico = 'DEMO-0007' AND i.tipo = 'NotaInterna');

-- DEMO-0008: duvida simples
INSERT INTO interacao (chamado_id, autor_id, tipo, mensagem, criado_em)
SELECT c.id, 3, 'Publica', 'Onde atualizo meu telefone e endereco no portal?', c.criado_em
FROM chamado c WHERE c.codigo_publico = 'DEMO-0008'
AND NOT EXISTS (SELECT 1 FROM interacao i JOIN chamado c2 ON c2.id = i.chamado_id WHERE c2.codigo_publico = 'DEMO-0008' AND i.mensagem LIKE 'Onde atualizo meu telefone%');

-- 3) CONFERENCIA ---------------------------------------------------------------
SELECT codigo_publico, status, prioridade, agente_id, titulo FROM chamado WHERE codigo_publico LIKE 'DEMO-%' ORDER BY codigo_publico;
SELECT c.codigo_publico, i.tipo, left(i.mensagem, 50) AS mensagem FROM interacao i JOIN chamado c ON c.id = i.chamado_id WHERE c.codigo_publico LIKE 'DEMO-%' ORDER BY c.codigo_publico, i.id;
