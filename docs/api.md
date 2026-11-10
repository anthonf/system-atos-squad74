# API de Chamados — Contrato de integração (RF10)

> Projeto: Atos Support Hub (Squad 74) | Requisito: RF10 — Sincronizar usuário e grupo empresa na abertura do chamado
> Fonte: Termo de Referência v3, seções 2.5 e 6.2

## 1. Visão geral

A integração é **servidor-a-servidor**: quem chama a API é o **back-end do portal Atos Capital**, nunca o usuário final. O usuário do portal não faz login na ferramenta de suporte. Não há SSO nem JWT.

Ao abrir um chamado, o portal envia no corpo da requisição os dados do **usuário** e do **grupo empresa**. A API confia nesses dados e faz um **upsert** (cria se não existe, atualiza se já existe), usando **o id do portal como id** do usuário e do grupo. Não é preciso cadastro prévio nem sincronização em lote.

| Item | Valor |
|---|---|
| Base URL (desenvolvimento) | `http://localhost:5177` |
| Formato | JSON (`Content-Type: application/json`) |
| Autenticação | API Key no header `X-Api-Key` |

## 2. Autenticação

Toda requisição deve enviar a API Key no header:

```
X-Api-Key: <chave gerada pela ferramenta de suporte>
```

- A chave é gerada pela ferramenta de suporte e guardada **somente no back-end do portal**.
- Chave **ausente** ou **inválida**: a API responde **401 Unauthorized** e **não grava nada** (nem chamado, nem usuário, nem grupo).
- Nunca coloque a chave real em repositório, collection ou documento. Use variável de ambiente.

## 3. Abrir chamado com sincronização de usuário e grupo

`POST /api/chamados`

### 3.1 Corpo da requisição

> Os nomes dos campos de usuário e grupo **não são fixados pelo Termo v3**. O formato abaixo é a proposta usada na collection e deve ser confirmado com o backend.

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `produto` | texto | sim | Produto do chamado (ex.: `Portal`). Precisa ter SLA configurado. |
| `categoria` | texto | sim | Categoria do chamado (ex.: `Login`). Precisa ter SLA configurado. |
| `descricao` | texto | sim | Descrição do problema. |
| `mensagemInicial` | texto | sim | Primeira mensagem do chamado. Hoje a API exige este campo junto com `descricao`. |
| `titulo` | texto | não | Título do chamado. Se omitido, a API gera um. |
| `usuario.id` | número | sim | Id do usuário **no portal**. Vira o id do usuário na ferramenta. |
| `usuario.nome` | texto | sim | Nome do usuário. |
| `usuario.email` | texto | sim | E-mail do usuário. |
| `grupoEmpresa.id` | número | sim | Id do grupo empresa **no portal**. Vira o id do grupo na ferramenta. |
| `grupoEmpresa.nome` | texto | sim | Nome do grupo empresa. |

### 3.2 Exemplo de requisição

```http
POST /api/chamados HTTP/1.1
Host: localhost:5177
Content-Type: application/json
X-Api-Key: <sua-chave>

{
  "produto": "Portal",
  "categoria": "Login",
  "titulo": "Não consigo entrar no portal",
  "descricao": "Erro ao fazer login desde cedo.",
  "mensagemInicial": "Erro ao fazer login desde cedo.",
  "usuario": {
    "id": 61090,
    "nome": "Maria Souza",
    "email": "maria.souza@cliente.example.com"
  },
  "grupoEmpresa": {
    "id": 71090,
    "nome": "Empresa Cliente S.A."
  }
}
```

### 3.3 Regras do upsert

| Situação | Comportamento esperado |
|---|---|
| Usuário e grupo **novos** | Cria os dois com os ids do portal e vincula o chamado a eles. |
| Usuário e grupo **já existentes** | Não duplica. Atualiza nome e e-mail se mudaram. |
| Usuário **novo** em grupo **existente** | Cria só o usuário e o vincula ao grupo existente. |
| Mesma identidade enviada de novo | Cria outro chamado, mas **não** duplica usuário nem grupo. |
| Falha de autenticação ou validação | Nada é gravado (nem chamado, nem usuário, nem grupo). |

### 3.4 Respostas

| Código | Quando | Corpo |
|---|---|---|
| `201 Created` | Chamado aberto com sucesso. | Dados do chamado (exemplo abaixo). |
| `400 Bad Request` | Campo obrigatório ausente ou produto/categoria sem SLA. | `ProblemDetails` com os erros de validação. |
| `401 Unauthorized` | API Key ausente ou inválida. | `ProblemDetails`. |

Exemplo de resposta `201`:

```json
{
  "id": 12,
  "codigoPublico": "SA-084799423",
  "usuarioId": 61090,
  "grupoEmpresaId": 71090,
  "agenteId": null,
  "produto": "Portal",
  "categoria": "Login",
  "titulo": "Não consigo entrar no portal",
  "descricao": "Erro ao fazer login desde cedo.",
  "status": "Aberto",
  "prioridade": "Alta",
  "slaPrazo": "2026-11-06T22:00:00Z",
  "slaPrazoResposta": "2026-11-06T19:00:00Z",
  "criadoEm": "2026-11-06T18:00:00Z"
}
```

O campo `codigoPublico` é o código que o cliente usa para acompanhar o chamado. Os valores exatos de `status` e `prioridade` seguem os enums da API.

## 4. Consultas auxiliares

Úteis para conferir se o upsert funcionou (por exemplo, nos testes do Postman).

| Requisição | Para que serve |
|---|---|
| `GET /api/chamados/{codigoPublico}` | Consulta um chamado pelo código público. |
| `GET /api/usuarios?grupoEmpresaId={id}` | Lista os usuários de um grupo. Serve para verificar duplicação. |
| `GET /api/usuarios/{id}` | Consulta um usuário pelo id. |

## 5. Como validar (Postman)

A collection `RF10-sincronizacao-usuario-grupo.postman_collection.json` cobre os 4 cenários exigidos pelo Termo v3 (seção 6.2):

1. Usuário e grupo novos.
2. Usuário já existente (sem duplicar).
3. Usuário novo em grupo existente.
4. Requisição sem API Key ou com chave inválida (retorno 401).

Passos:

1. Importar a collection no Postman.
2. Na aba **Variables** da collection, definir `base_url` e `api_key` (a chave real, só na sua máquina).
3. Executar a collection na ordem (Runner). Os cenários 2 e 3 reaproveitam o usuário e o grupo criados no cenário 1.

## 6. Webhooks (fora do escopo desta entrega)

O Termo prevê webhooks de eventos do chamado (criado, atualizado, aguardando ação do cliente, fechado), enviados ao portal e autenticados por API Key no header. O formato do JSON de cada evento e o nome do header serão documentados em uma entrega própria.

## 7. Status da implementação

Em 03/11/2026, o relatório de QA (`tests/qa-v3-rf01-rf10-20261103/NAO_CONFORMIDADES.md`) registrou que a validação da API Key e o upsert por id do portal ainda **não** estavam funcionando. Enquanto isso não for corrigido no backend, os cenários 1 a 4 da collection **vão falhar** — o que é esperado e mostra exatamente o que falta implementar.
