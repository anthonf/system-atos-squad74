# Documentação — Atos Support Hub (Squad 74)

> Branch: `docs/documentacao` | Fonte: `Documentação de um Produto de Software v3.0` + `Entrega Parcial - Takeoff`
> Projeto: Plataforma de suporte técnico integrada e multicanal — Unidade de Bank, Atos Capital.
> Stack: C#/.NET + React + PostgreSQL + n8n | Método: Kanban + modelagem UML/RUP

**Autores:** João Pedro Alves Seixas; José Henrique Maciel Dantas; Vinícius A. Sampaio; Anthony Santos Costa; Kayone Silva do Nascimento; Danilo Sá Almeida; Marcus Lucas Dantas da Silva (@Marcus-dnts); Pedro Henrique Nunes de Melo.
**Aracaju – 2026 | Docente:** Profa. Ana Paula Gonçalves Serra

## Índice

- [01 — Introdução, Glossário e Visão Geral](./01-visao-geral.md)
  - Tema, objetivo, delimitação, justificativa, método Kanban, organização, glossário, descrição do problema, envolvidos, RN01–RN18
- [02 — Requisitos do Sistema](./02-requisitos.md)
  - RF01–RF13, atores, UC01–UC11b/UC13, RNFs, protótipo, navegação, métricas e cronograma
- [03 — Análise, Design e Dados](./03-arquitetura-dados.md)
  - Arquitetura desacoplada, domínio, interação/sequência, classes, atividades, estados, componentes, modelo lógico + Scripts SQL 1–6, dicionário, ambiente, integrações externas
- [04 — Implementação, Testes, Implantação e Manual](./04-implementacao-testes.md)
  - Boas práticas, plano de testes, implantação, manual usuário, conclusões, bibliografia
- [05 — Entrega Parcial Takeoff — Software & IA](./05-entrega-parcial-takeoff.md)
  - Problema/personas IA-augmented, backlog AI-first, banco/API, arquitetura/stack, relato do processo

## MVP delimitado (RN18)

Obrigatório nesta edição: abertura e ciclo de vida de chamados via portal, notificação por webhook, relatórios/alertas de SLA e sincronização de usuários (RF01–RF10).

Fora do escopo (Fase 2): e-mail inbound, WhatsApp, triagem por IA, base de conhecimento/FAQ e CSAT.

## Ciclo de vida do chamado

`ABERTO → EM_ANDAMENTO → AGUARDANDO_CLIENTE → RESOLVIDO → FECHADO`

Transições inválidas (ex.: `FECHADO → ABERTO`) são bloqueadas. Ver RN04 e UC04a/b/c.

## Diagramas

Os diagramas UML (casos de uso Fig.1, classes Fig.2, sequência, atividades, estados, componentes, implantação, modelo lógico) estão no PDF original. Nesta versão `.md` mantemos o espaço reservado + leitura textual. Na versão final anexar em `docs/img/` e referenciar como `![Figura 1](./img/figura1-casos-de-uso.png)`.

## Como contribuir

1. Você já está em `docs/documentacao` (saiu da `main`).
2. Edite os `.md` desta pasta.
3. Suba por PR:

```powershell
git add docs/
git commit -m "docs: adiciona documentacao em md"
git push -u origin docs/documentacao
```
