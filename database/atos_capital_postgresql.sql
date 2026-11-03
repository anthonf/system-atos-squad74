-- =========================================================
-- ATOS CAPITAL - PostgreSQL (ARQUIVO LEGADO / REFERÊNCIA)
-- Fonte oficial do schema: EF Core Migrations em
-- src/backend/src/SupportAtos.Infrastructure/Migrations.
-- Não aplique este .sql junto com `dotnet ef database update`
-- (nomes como sla_config/BIGSERIAL divergem do modelo atual).
-- =========================================================


-- =========================================================
-- REMOVER TABELAS EXISTENTES
-- =========================================================

DROP TABLE IF EXISTS public.anexo CASCADE;
DROP TABLE IF EXISTS public.log_auditoria CASCADE;
DROP TABLE IF EXISTS public.interacao CASCADE;
DROP TABLE IF EXISTS public.chamado CASCADE;
DROP TABLE IF EXISTS public.sla_config CASCADE;
DROP TABLE IF EXISTS public.usuario CASCADE;
DROP TABLE IF EXISTS public.grupo_empresa CASCADE;


-- =========================================================
-- GRUPO EMPRESA
-- =========================================================

CREATE TABLE public.grupo_empresa (
    id BIGSERIAL NOT NULL,
    nome VARCHAR(150) NOT NULL,
    tipo VARCHAR(20) DEFAULT 'matriz',
    grupo_pai_id BIGINT DEFAULT NULL,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (id),

    CONSTRAINT grupo_empresa_ibfk_1
        FOREIGN KEY (grupo_pai_id)
        REFERENCES public.grupo_empresa (id)
);


CREATE INDEX idx_grupo_pai
    ON public.grupo_empresa (grupo_pai_id);


-- =========================================================
-- USUARIO
-- =========================================================

CREATE TABLE public.usuario (
    id BIGSERIAL NOT NULL,
    nome VARCHAR(150) NOT NULL,
    email VARCHAR(255) NOT NULL,
    grupo_empresa_id BIGINT NOT NULL,
    papel VARCHAR(20) NOT NULL DEFAULT 'cliente',
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    atualizado_em TIMESTAMP DEFAULT NULL,

    PRIMARY KEY (id),

    CONSTRAINT email
        UNIQUE (email),

    CONSTRAINT usuario_ibfk_1
        FOREIGN KEY (grupo_empresa_id)
        REFERENCES public.grupo_empresa (id)
);


CREATE INDEX idx_usuario_grupo
    ON public.usuario (grupo_empresa_id);

-- Mantido conforme o MySQL original.
-- Observação: UNIQUE(email) já cria um índice no PostgreSQL,
-- portanto este índice é redundante, mas foi mantido pela conversão.
CREATE INDEX idx_usuario_email
    ON public.usuario (email);


-- =========================================================
-- SLA CONFIG
-- =========================================================

CREATE TABLE public.sla_config (
    id BIGSERIAL NOT NULL,
    produto VARCHAR(100) NOT NULL,
    categoria VARCHAR(100) NOT NULL,
    prioridade VARCHAR(20) NOT NULL,
    tempo_resposta_min INTEGER NOT NULL,
    tempo_resolucao_min INTEGER NOT NULL,
    ativo BOOLEAN DEFAULT TRUE,

    PRIMARY KEY (id),

    CONSTRAINT uq_sla_prod_cat_prio
        UNIQUE (produto, categoria, prioridade)
);


-- =========================================================
-- CHAMADO
-- =========================================================

CREATE TABLE public.chamado (
    id BIGSERIAL NOT NULL,
    codigo_publico VARCHAR(20) NOT NULL,
    usuario_id BIGINT NOT NULL,
    grupo_empresa_id BIGINT NOT NULL,
    agente_id BIGINT DEFAULT NULL,
    sla_config_id BIGINT DEFAULT NULL,
    produto VARCHAR(100) NOT NULL,
    categoria VARCHAR(100) NOT NULL,
    prioridade VARCHAR(20) NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'aberto',
    titulo VARCHAR(200) NOT NULL,
    descricao TEXT NOT NULL,
    sla_prazo_resposta TIMESTAMP DEFAULT NULL,
    sla_prazo_resolucao TIMESTAMP DEFAULT NULL,
    aguardando_desde TIMESTAMP DEFAULT NULL,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    atualizado_em TIMESTAMP DEFAULT NULL,
    fechado_em TIMESTAMP DEFAULT NULL,

    PRIMARY KEY (id),

    CONSTRAINT codigo_publico
        UNIQUE (codigo_publico),

    CONSTRAINT chamado_ibfk_1
        FOREIGN KEY (usuario_id)
        REFERENCES public.usuario (id),

    CONSTRAINT chamado_ibfk_2
        FOREIGN KEY (grupo_empresa_id)
        REFERENCES public.grupo_empresa (id),

    CONSTRAINT chamado_ibfk_3
        FOREIGN KEY (agente_id)
        REFERENCES public.usuario (id),

    CONSTRAINT chamado_ibfk_4
        FOREIGN KEY (sla_config_id)
        REFERENCES public.sla_config (id)
);


CREATE INDEX idx_chamado_usuario
    ON public.chamado (usuario_id);

CREATE INDEX idx_chamado_grupo
    ON public.chamado (grupo_empresa_id);

CREATE INDEX idx_chamado_agente
    ON public.chamado (agente_id);

CREATE INDEX idx_chamado_status
    ON public.chamado (status);

CREATE INDEX idx_chamado_relatorio_RF08
    ON public.chamado
    (produto, categoria, status, grupo_empresa_id, criado_em);

CREATE INDEX idx_chamado_sla_RF09
    ON public.chamado (sla_prazo_resolucao);

CREATE INDEX sla_config_id
    ON public.chamado (sla_config_id);


-- =========================================================
-- INTERACAO
-- =========================================================

CREATE TABLE public.interacao (
    id BIGSERIAL NOT NULL,
    chamado_id BIGINT NOT NULL,
    autor_id BIGINT NOT NULL,
    tipo VARCHAR(20) NOT NULL,
    mensagem TEXT NOT NULL,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (id),

    CONSTRAINT interacao_ibfk_1
        FOREIGN KEY (chamado_id)
        REFERENCES public.chamado (id),

    CONSTRAINT interacao_ibfk_2
        FOREIGN KEY (autor_id)
        REFERENCES public.usuario (id)
);


CREATE INDEX idx_interacao_chamado
    ON public.interacao (chamado_id);

CREATE INDEX autor_id
    ON public.interacao (autor_id);


-- =========================================================
-- ANEXO
-- =========================================================

CREATE TABLE public.anexo (
    id BIGSERIAL NOT NULL,
    interacao_id BIGINT DEFAULT NULL,
    chamado_id BIGINT NOT NULL,
    nome_arquivo VARCHAR(255) NOT NULL,
    caminho VARCHAR(500) NOT NULL,
    tamanho_bytes INTEGER DEFAULT NULL,
    tipo_mime VARCHAR(100) DEFAULT NULL,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (id),

    CONSTRAINT anexo_ibfk_1
        FOREIGN KEY (interacao_id)
        REFERENCES public.interacao (id),

    CONSTRAINT anexo_ibfk_2
        FOREIGN KEY (chamado_id)
        REFERENCES public.chamado (id)
);


CREATE INDEX anexo_interacao_id
    ON public.anexo (interacao_id);

CREATE INDEX anexo_chamado_id
    ON public.anexo (chamado_id);


-- =========================================================
-- LOG AUDITORIA
-- =========================================================

CREATE TABLE public.log_auditoria (
    id BIGSERIAL NOT NULL,
    chamado_id BIGINT NOT NULL,
    usuario_id BIGINT DEFAULT NULL,
    acao VARCHAR(50) NOT NULL,
    campo_alterado VARCHAR(100) DEFAULT NULL,
    valor_anterior TEXT,
    valor_novo TEXT,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (id),

    CONSTRAINT log_auditoria_ibfk_1
        FOREIGN KEY (chamado_id)
        REFERENCES public.chamado (id),

    CONSTRAINT log_auditoria_ibfk_2
        FOREIGN KEY (usuario_id)
        REFERENCES public.usuario (id)
);


CREATE INDEX idx_log_chamado
    ON public.log_auditoria (chamado_id);

CREATE INDEX idx_log_data
    ON public.log_auditoria (criado_em);

CREATE INDEX usuario_id
    ON public.log_auditoria (usuario_id);


-- =========================================================
-- DADOS INICIAIS - GRUPO EMPRESA
-- =========================================================

INSERT INTO public.grupo_empresa
    (id, nome, tipo, grupo_pai_id, criado_em)
VALUES
    (1, 'Atos Capital - Matriz', 'matriz', NULL, '2026-09-01 20:03:03'),
    (2, 'Filial Recife', 'filial', 1, '2026-09-01 20:22:14'),
    (3, 'Filial São Paulo', 'filial', 1, '2026-09-01 20:22:14');


-- =========================================================
-- DADOS INICIAIS - USUARIO
-- =========================================================

INSERT INTO public.usuario
    (id, nome, email, grupo_empresa_id, papel, criado_em, atualizado_em)
VALUES
    (1, 'Atos', 'atos@gmail.com', 1, 'Gerente', '2026-09-01 20:11:28', NULL),
    (2, 'Maria Silva', 'maria.silva@cliente.com', 1, 'cliente', '2026-09-01 20:22:14', NULL),
    (3, 'João Pereira', 'joao.pereira@cliente.com', 2, 'cliente', '2026-09-01 20:22:14', NULL),
    (4, 'Ana Costa', 'ana.costa@cliente.com', 3, 'cliente', '2026-09-01 20:22:14', NULL);


-- =========================================================
-- AJUSTAR SEQUENCES DOS BIGSERIAL
-- =========================================================

SELECT setval(
    pg_get_serial_sequence('public.grupo_empresa', 'id'),
    COALESCE((SELECT MAX(id) FROM public.grupo_empresa), 1),
    true
);


SELECT setval(
    pg_get_serial_sequence('public.usuario', 'id'),
    COALESCE((SELECT MAX(id) FROM public.usuario), 1),
    true
);


SELECT setval(
    pg_get_serial_sequence('public.sla_config', 'id'),
    COALESCE((SELECT MAX(id) FROM public.sla_config), 1),
    true
);


SELECT setval(
    pg_get_serial_sequence('public.chamado', 'id'),
    COALESCE((SELECT MAX(id) FROM public.chamado), 1),
    true
);


SELECT setval(
    pg_get_serial_sequence('public.interacao', 'id'),
    COALESCE((SELECT MAX(id) FROM public.interacao), 1),
    true
);


SELECT setval(
    pg_get_serial_sequence('public.anexo', 'id'),
    COALESCE((SELECT MAX(id) FROM public.anexo), 1),
    true
);


SELECT setval(
    pg_get_serial_sequence('public.log_auditoria', 'id'),
    COALESCE((SELECT MAX(id) FROM public.log_auditoria), 1),
    true
);