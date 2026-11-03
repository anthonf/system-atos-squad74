# 01 — Introdução, Glossário e Visão Geral

## Prefácio

Este documento registra, de forma estruturada e sob notação UML, a documentação técnica e acadêmica do projeto **Atos Support Hub**, desenvolvido na disciplina de Residência de Software (Tecnologia — UNIT), a partir do desafio da empresa **Atos Capital**.

Roteiro baseado no modelo de documentação de produtos de software, adaptado à realidade do projeto: plataforma de suporte técnico multicanal, cujo MVP está delimitado à abertura e ao ciclo de vida de chamados via portal, conforme Termo de Referência da Atos Capital.

Documento construído de forma incremental, capítulo a capítulo, acompanhando a evolução em Kanban. Público-alvo:

- equipe de alunos responsável pelo desenvolvimento;
- responsável técnico e stakeholders da Atos Capital;
- corpo docente da Residência, para avaliação acadêmica.

Diagramas UML (casos de uso, classes, sequência, atividades, componentes e implantação) serão referenciados ao longo do texto e anexados nas versões finais; nas versões intermediárias o foco é na especificação textual.

## 1.1 Introdução ao Documento

### 1.1 Tema

Desenvolvimento de plataforma de suporte técnico integrada e multicanal, denominada provisoriamente **Atos Support Hub**, para substituir o GLPI na Unidade de Bank da Atos Capital. Núcleo: abertura, acompanhamento e resolução de chamados via portal web, com arquitetura preparada para expansão a outras plataformas e canais.

Contexto: empresas de tecnologia voltadas à automação de processos financeiros e integração com SAP Business One e SAP S/4HANA, nas quais suporte contínuo e rastreável é crítico.

### 1.2 Objetivo do Projeto

**Geral:** desenvolver ferramenta de suporte exclusiva para a Unidade de Bank, integrada nativamente ao portal Atos Capital, que resolva limitações da ferramenta atual e possa ser reaproveitada por outros produtos.

**Específicos:**

- Abertura de chamados via portal, com SSO/JWT e vínculo automático a usuário e grupo empresa (matriz/filial).
- Ciclo de vida completo (aberto, em andamento, aguardando cliente, resolvido, fechado), com atribuição e notas internas.
- Notificações automáticas a cada evento relevante, sem verificação manual.
- SLA por prioridade/categoria, com alertas de estouro.
- Relatórios gerenciais filtráveis por produto, cliente/grupo empresa, categoria e período.
- Código-fonte próprio, documentado e testado, mantível pela Atos após a residência.

### 1.3 Delimitação do Problema

**Público-alvo:** clientes da Unidade Bank (Joe SFA B1, portal Atos Capital) e equipe interna de suporte.

**Dimensão:** GLPI não notifica automaticamente, não gera relatórios segmentados por produto/cliente/categoria e, por ser de terceiros sem código-fonte, impede integrações como SSO. Gera verificação manual, perda de visibilidade e retrabalho.

**Solução proposta:** ferramenta própria com abertura via portal, webhook, SLA com alertas, relatórios nativos, integrada via SSO/JWT.

**Escopo desta edição:** abertura e ciclo de vida via portal, webhook, relatórios/alertas SLA, sincronização de usuários. **Fora:** e-mail, WhatsApp, triagem por IA, base self-service, CSAT.

### 1.4 Justificativa

- **Prática/negócio:** limitações concretas do GLPI impactam a operação do Bank. Há responsável técnico e critérios de aceite formais.
- **Acadêmica:** aplica OO, UML, arquitetura desacoplada, SSO/JWT, documentação e testes automatizados em cenário real.
- **Continuidade:** IP compartilhada garante aplicabilidade real após a disciplina.

### 1.5 Método de Trabalho

Kanban, com fluxo contínuo (Backlog, Em Análise, Em Desenvolvimento, Em Teste, Concluído), WIP limitado, foco em RF01–RF10 antes de RF11–RF13, reuniões semanais com responsável técnico da Atos e modelagem OO/UML (artefatos RUP).

### 1.6 Organização do Trabalho

Cap.1 projeto; Cap.2 sistema/envolvidos/regras; Cap.3 requisitos/protótipos/cronograma; Cap.4 análise/design; Cap.5 implementação; Cap.6 testes; Cap.7 implantação; Cap.8 manual; Cap.9 conclusões.

### 1.7 Glossário

- **API:** comunicação entre portal Atos Capital, front React e API .NET.
- **Chamado:** solicitação de suporte com ciclo controlado.
- **Cliente:** usuário vinculado a empresa atendida, abre/acompanha chamados.
- **Agente:** atende fila, responde, nota interna, status, SLA.
- **Supervisor:** acompanha operação, indicadores, relatórios, redistribui.
- **Administrador:** papéis, categorias, prioridades, SLA, configurações.
- **Grupo empresa:** matriz/filial do cliente.
- **SLA:** prazos de resposta/resolução por prioridade/categoria.
- **SSO:** autenticação única do portal.
- **JWT:** token com usuário + grupo empresa.
- **Webhook:** envia eventos (criado, atualizado, aguardando cliente, fechado) ao portal.
- **Upsert:** cria ou atualiza (sincronização usuário/grupo).
- **n8n:** orquestra webhooks, alertas e futuras integrações.
- **MVP:** RF01–RF10.
- **Auditoria:** quem, quando, campo, anterior/novo valor.

## 2. Descrição Geral do Sistema

### 2.1 Descrição do Problema

**Quem é afetado:** clientes do Bank, equipe de suporte, gestores do Bank.

**Impacto:** substitui GLPI, elimina verificação manual, entrega relatórios segmentados e integrações nativas (SSO) inviáveis antes.

**Boa solução:** plataforma própria desacoplada, reaproveitável (ex.: Joe SFA B1) e evolutiva para e-mail/WhatsApp (Fase 2).

### 2.2 Envolvidos

**Usuários:**

- **Cliente:** portal Atos Capital. Abre (produto/módulo, categoria, descrição, anexos opcionais), consulta status/histórico público, responde quando aguardado.
- **Agente:** fila, assume/recebe, responde, nota interna, status, anexos, SLA.
- **Supervisor:** filas, indicadores, SLA, relatórios filtráveis, redistribui.
- **Administrador:** papéis, categorias, prioridades, SLA.

**Desenvolvedores:** alunos Residência (C#/.NET, React, PostgreSQL, n8n) + Atos (validação, responsável técnico semanal, integração SSO/webhook) + docentes (avaliação).

### 2.3 Regras de Negócio

- **RN01:** todo chamado do portal vinculado a usuário + grupo empresa.
- **RN02:** gera identificador público (código público).
- **RN03:** exige produto/módulo, categoria, descrição; anexos opcionais.
- **RN04:** estados: aberto, em andamento, aguardando cliente, resolvido, fechado.
- **RN05:** atribuível manual + automática por categoria.
- **RN06:** histórico público + alterações relevantes, por perfil.
- **RN07:** nota interna só equipe, nunca cliente.
- **RN08:** SLA por prioridade/categoria (SlaConfig).
- **RN09:** monitora prazos (no prazo, em risco, estourado).
- **RN10:** alerta via job/n8n a agente + supervisor.
- **RN11:** aguardando cliente além do prazo → escalonamento/encerramento por inatividade.
- **RN12:** eventos (criado, atualizado, aguardando cliente, fechado) via webhook.
- **RN13:** upsert usuário/grupo via SSO/JWT.
- **RN14:** acesso por papel (cliente, agente, supervisor, admin).
- **RN15:** auditoria com responsável, momento, campo, anterior/novo.
- **RN16:** relatórios por produto, cliente/grupo, categoria, período + recorrência.
- **RN17:** identifica chamados/categorias recorrentes.
- **RN18:** e-mail, WhatsApp, IA, FAQ, CSAT fora do MVP.
