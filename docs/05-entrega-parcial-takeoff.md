# 05 — Entrega Parcial Takeoff | Residência Software & IA — Squad 74

> Empresa: Atos Capital | Porto Digital + Unit. Status real do código auditado.

## 1. Problema e Personas IA-Augmented

### 1.1 Problema, Solução, Oportunidade IA

- **Problema (250c):** sem ferramenta interna que centralize atendimento, acompanhamento e gestão de chamados, dificultando fila, SLA e histórico.
- **Solução (250c):** web que centraliza fila, SLA, respostas, notas internas, relatórios, com API REST, PostgreSQL e React.
- **Estado IA:** nenhuma LLM/serviço IA integrado, sem prompts/engine. Diretriz do TR ainda não materializada.
- **Oportunidades (planejadas):** geração (sugestão respostas evolução TemplatesBar manual, resumo histórico); automação (triagem produto/categoria/prioridade); predição (roteamento por carga/especialidade).

**Personas:**

- **Cliente:** Portal com Atos Capital ID (sem senha própria aqui). Acompanha existentes; não abre novos por este front.
- **Agente:** aba Equipe (e-mail/senha protótipo). Fila, SLA, responde, atribui, encerra, notas, relatórios.

### 1.2 Cenários e Bordas

- **C1 Agente acessa:** Equipe → e-mail/senha → valida → área suporte.
- **C2 Agente gerencia:** fila → SLA → responde/notas → atribui/encerra.
- **C3 Cliente acompanha:** Portal → Entrar com Atos ID → consulta existentes.

**Bordas MVP mockado (`src/SupportPortal.jsx`):** sem colunas/paginação; Exportar/Atualizar visuais; seed local. Auth: qualquer e-mail + 4+ chars, papel fixo na sessão. Persistência `useState`, sem request real.

**Bordas full-stack:** JWT inválido/expirado/ausente 401; outro grupo 404 (anti-vazamento); cliente só Resolvido→Aberto senão 403; sem regra SLA 400; falha código único após 5 tentativas 409.

**IA:** N/A.

### 1.3 Entrevistas (opcional)

Sem relatório até o momento. Incluir descoberta/validação com ≥1 humano.

## 2. Backlog AI-First

### 2.1 Priorizado

- **EP1 Auth/provisionamento:** SSO/JWT, JIT usuário+grupo, API Key M2M.
- **EP2 Abertura/ciclo:** API + código público único, 5 estados + transição por papel.
- **EP3 Atendimento:** fila/filtros, pública, internas, manual/auto, templates.
- **EP4 SLA:** por produto/categoria/prioridade, prazo na abertura, visível, jobs alerta planejados.
- **EP5 Relatórios/dashboard:** contadores/SLA, export/filtros em evolução.
- **EP6 Notificações:** webhook + inatividade (planejado).
- **EP7 Multicanal/expansão:** e-mail/WhatsApp, reuso (longo prazo).

**Histórias:** EU-01 abrir (código público); EU-02 consultar sem internas; EU-03 reabrir resolvido; EU-04 fila filtros; EU-05 pública+interna; EU-06 atribuir manual/auto; EU-07 status com confirmação; EU-08 admin papéis; EU-09 dashboard SLA; EU-10 JIT JWT; EU-11 prazo SLA.

| Cód | Requisito | Prior | Status | Evidência |
|---|---|---|---|---|
| RF01 | Criar via API | Ess | Implementado | POST /api/chamados |
| RF02 | Consultar | Ess | Implementado | GET /api/chamados/{codigo} |
| RF03 | Atribuir | Ess | Implementado | PATCH .../atribuir |
| RF04 | Status | Ess | Implementado | PATCH .../status 5 estados |
| RF05 | Notas | Ess | Implementado | POST .../interacoes NotaInterna |
| RF06 | Webhook | Ess | Planejado | sem código |
| RF07 | SLA | Ess | Parcial | sla_categoria + prazo; job pendente |
| RF08 | Relatórios | Ess | Parcial | dashboard; sem endpoint |
| RF09 | Alertas | Ess | Planejado | sem job |
| RF10 | SSO/grupo | Ess | Implementado | UserProvisioningService JIT |
| RF11 | Anexos | Des | Parcial | entidade/DTO; sem upload |
| RF12 | Papel | Des | Implementado | Cliente/Agente/Supervisor/Admin |
| RF13 | Auditoria | Des | Parcial | LogAuditoria + ProvisionamentoAudit |

**Engenharia (Kanban):** Domain, Application (8 UseCases/Ports/DTOs), Infrastructure (repos, UoW, 3 Migrations, seeder), Api (controllers, exceções, /health), JIT (UserProvisioningService, ClaimsReader, Audit), frontend atendente (Login, Fila, Detalhe, Dashboard, Auth, api), testes xUnit + integração, TokenMinter + mint-token.ps1 — concluídos. Webhook, jobs SLA, relatórios/export, upload, e-mail/WhatsApp — planejados/parciais.

**IA no processo:** nenhuma integrada. Manter humano no controle, versionar prompts, sem sensíveis em modelos externos.

### 2.2 Aceite

- POST /api/chamados válido → 201 ChamadoDto `SA-XXXXXXXXXX`, Aberto.
- Branco → 400; sem SLA → 400. Prazo auto via sla_categoria.
- Cliente só seu grupo senão 404. Internas só equipe. Cliente só Resolvido→Aberto senão 403.
- Atribuir exige Agente/Supervisor/Admin; auto dispensa agenteId. POST /api/usuarios só Admin; duplicado 409.
- Primeiro JWT válido → JIT. JWT ruim → 401 RS256 iss/aud/validade.
- Front bloqueia Cliente, confirma irreversível, máscara e-mail LGPD. **IA:** N/A.

## 3. Banco e API

PostgreSQL, Migrations EF (InitialCreate, AddPortalExternalId, AlinharAoSqlOficial), snake_case, enums texto, UTC. Seeder dev se vazio.

| Tabela | Finalidade | Campos | Rel |
|---|---|---|---|
| grupo_empresa | cliente | id, nome | auto pai (legado) |
| usuario | clientes+equipe | id, nome, email único, grupo_empresa_id, papel, portal_external_id | N:1 grupo |
| sla_categoria | SLA | id, produto, categoria, prioridade, tempos min | único trio |
| chamado | suporte | id, codigo_publico único, usuario/grupo/agente/sla ids, produto, categoria, prioridade, status, titulo, descricao, prazos, aguardando_desde, datas | N:1 usuario, grupo, sla |
| interacao | mensagens | id, chamado, autor, tipo, mensagem, criado | N:1 chamado, usuario |
| anexo | arquivos | id, interacao, nome, url/caminho, bytes, mime | N:1 interacao, chamado |
| log_auditoria | rastro | id, chamado, usuario, acao, campo, anterior, novo, criado | N:1 ambos |
| provisionamento_audit | JIT | id, usuario, grupo, claims/external, criado | N:1 ambos |

Sem entidades IA.

**Auth:** JWT RS256 Portal (Bearer, iss/aud/assinatura) + X-Api-Key M2M. Enums string. Swagger `/swagger` (:5177 dev).

| Método | Rota | Corpo | Resposta | Acesso |
|---|---|---|---|---|
| GET | /health | — | 200 | anônimo |
| POST | /api/usuarios | nome,email,grupo,papel | 201/400/409 | JWT+Admin |
| GET | /api/usuarios/{id} | id | 200/404 | Admin/Sup/Agente |
| GET | /api/usuarios?grupoEmpresaId= | obrigatório | 200[] | Admin/Sup/Agente |
| POST | /api/chamados | produto,categoria,mensagemInicial,titulo?,descricao? | 201/400/404 | UsuarioPortal |
| GET | /api/chamados | filtros | 200[] só grupo | UsuarioPortal |
| GET | /api/chamados/{codigo} | codigo | 200+interacoes/404 | equipe vê internas |
| POST | .../{codigo}/interacoes | mensagem,tipo | 201/400/403/404 | UsuarioPortal |
| PATCH | .../{codigo}/status | novoStatus | 200/400/403/404 | cliente só reabre |
| PATCH | .../{codigo}/atribuir | agenteId?,automatico? | 200/400/403/404 | Admin/Sup/Agente |

**Fluxo:** front JWT em memória → Controller→UseCase→repo→UoW. Primeiro acesso JIT via claims. Abertura valida, busca sla_categoria (400), gera SA- (5 tentativas, 409), calcula SLA. PostgreSQL+UoW, log_auditoria. Front traduz ProblemDetails pt-BR, polling 30s, `/` buscar `R` atualizar. Completo: login JIT → POST cria+SLA+1ª pública 201 → GET fila → GET timeline → responde/altera/atribui. Pendente: webhook, job SLA, inatividade, upload.

**Erros (ProblemDetails):** 400 Validation/Domain, 403 Forbidden, 404 NotFound (fora grupo), 409 Conflict (e-mail/código), 401 sem/inválido → /login, 500 genérica sem stack. Front timeout 15s AbortController.

## 4. Arquitetura/Stack

| Camada | Tec | Obs |
|---|---|---|
| Back | C#/.NET 10, ASP.NET, EF 10, Npgsql | Clean, Swagger, string enums |
| Banco | Postgres :5432/:5433 Docker | migrations+seeder |
| Auth | JWT RS256 + ApiKey | iss/aud/JIT/TokenMinter |
| Front atendente | React18 TS Vite6 MUI router sonner Vitest | API real, polling 30s, JWT memória, LGPD, claro/escuro |
| Front legado | React18 Vite5 Tailwind Recharts lucide | mock seed, sem API |
| Testes | xUnit, Vitest | TestWebAppFactory+Postgres |
| DevOps | dotnet-ef, Secrets, Compose, ps1 | segredos fora repo, gitignore chaves |

Local: back :5177 front :5173 proxy /api. Requer .NET10, Postgres, Node20+. Sem IA paga/gratuita.

Clean: Domain→Application→Infrastructure→Api, controller nunca acessa banco. Ports&Adapters, UoW, ProblemDetails. Front: api/, auth/, types/, components/ (Layout, Chips, SlaBadge, Confirm, TemplatesBar), pages/ (Login, Fila, Detalhe, Dashboard), hooks usePolling, utils/. Repo: src/backend/, src/frontend/, src/SupportPortal.jsx legado, database/ DDL, LOGOTIPO/, Atos_Capital/, Especificacao do desafio/. Front sem IA. Timeout 15s, pt-BR, 401→login, polling 30s, 500 genérica.

## 5. Processo/Prompt (opcional)

Sem prompts (sem IA). Quando houver: versionar objetivo/contexto/saída/problemas, registrar versão, agente decide.

**Feito:** F1 MVP mockado (seed, login simulado, fila, notas, relatórios); F2 back full (Clean, 3 migrations, JWT+Key, JIT, ProblemDetails, 2 testes); F3 front integrado (JWT memória, polling, timeline, templates, confirmação, SLA, LGPD, temas). **Pendente:** webhook RF06, jobs RF07/09, relatórios RF08, upload RF11, multicanal, log todos fluxos RF13.

Kanban essenciais→desejáveis, WIP, alinhamentos, segredos fora (Secrets/gitignore), READMEs. Impacto IA processo não descrito — complementar.
