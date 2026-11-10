-- [AUTH-DESATIVADA] Dados minimos para o usuario mock usado no lugar do token.
-- Execute depois de `dotnet ef database update` e, se quiser, depois de `seed.sql`.

INSERT INTO grupo_empresa (id, nome)
VALUES (3, 'Grupo Norte')
ON CONFLICT DO NOTHING;

INSERT INTO usuario (id, nome, email, grupo_empresa_id, papel)
VALUES (4, 'Admin', 'admin@atos.com', 3, 'Admin')
ON CONFLICT DO NOTHING;

SELECT setval(
  pg_get_serial_sequence('grupo_empresa', 'id')::regclass,
  GREATEST((SELECT COALESCE(MAX(id), 1) FROM grupo_empresa), 3)
);

SELECT setval(
  pg_get_serial_sequence('usuario', 'id')::regclass,
  GREATEST((SELECT COALESCE(MAX(id), 1) FROM usuario), 4)
);
