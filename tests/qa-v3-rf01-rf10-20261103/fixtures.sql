INSERT INTO grupo_empresa(id,nome,codigo_externo,tipo,criado_em) VALUES
    (71001,'QA V3 Grupo A','v3-grupo-a','matriz',now()),(71002,'QA V3 Grupo B','v3-grupo-b','matriz',now()),(3301,'QA V3 Legado','95001','matriz',now());
    INSERT INTO usuario(id,nome,email,codigo_externo,grupo_empresa_id,papel,criado_em) VALUES
    (61001,'QA V3 A','a@qa.example.invalid','v3-a',71001,'Cliente',now()),(61002,'QA V3 B','b@qa.example.invalid','v3-b',71002,'Cliente',now()),(61003,'QA V3 C','c@qa.example.invalid','v3-c',71001,'Cliente',now()),(2301,'QA V3 Legado','legado@qa.example.invalid','94001',3301,'Cliente',now());
    INSERT INTO sla_config(id,produto,categoria,prioridade,tempo_resposta_min,tempo_resolucao_min,ativo) VALUES(1,'QA','Valid','Media',60,480,true);