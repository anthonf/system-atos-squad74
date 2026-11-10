# Contrato HTTP observado × Termo v3

Fonte normativa exclusiva: [Termo v3](../Especificacao%20do%20desafio/Termo_Referencia_Residencia_Software.v3.docx), lido integralmente, com critérios nas seções 2.1, 2.5, 4/RF01/RF10, 6.2 e 8. A [revisão anterior](../qa-rf01-20261103/RF01-Revisao-Termo-v3.md) foi consultada como histórico, sem reutilizar expectativas de JWT.

## Implementação inspecionada — análise estática

`POST /api/chamados`: [ChamadosController](../src/backend/src/SupportAtos.Api/Controllers/ChamadosController.cs) recebe `CriarChamadoInput`. [DTO](../src/backend/src/SupportAtos.Application/DTOs/CriarChamadoInput.cs): `Produto`, `Categoria`, `MensagemInicial`, `Titulo?`, `Descricao?`. Não declara dados de usuário/grupo. O [caso de uso](../src/backend/src/SupportAtos.Application/UseCases/CriarChamadoUseCase.cs) valida produto/categoria/mensagem inicial, procura SLA e usa `ICurrentUser`. [Program](../src/backend/src/SupportAtos.Api/Program.cs) registra [MockCurrentUserProvider](../src/backend/src/SupportAtos.Api/Providers/MockCurrentUserProvider.cs), IDs 4/3; autenticação e autorização estão comentadas. O Swagger descrito no código ainda indica Bearer, diferente da v3.

`GET /api/chamados/{codigoPublico}` foi usado apenas como confirmação da criação/persistência, sem avaliação de RF02. [DI](../src/backend/src/SupportAtos.Infrastructure/DependencyInjection.cs) lê `ConnectionStrings:DefaultConnection`; o seeder de desenvolvimento não executa em Production. Não foi encontrado serviço de saída HTTP/webhook registrado na inicialização/caminho de criação inspecionado. Não foram configurados serviços externos nem usadas credenciais reais. A API foi chamada somente por loopback.

Essas constatações estáticas não substituem os testes HTTP/SQL. As falhas reportadas têm evidências de execução nesta bateria.

## Payload proposto A — aninhado

```json
{
  "produto": "QA",
  "categoria": "Valid",
  "descricao": "Descricao QA v3",
  "mensagemInicial": "Descricao QA v3",
  "titulo": "QA v3",
  "usuario": {"id": 61001, "nome": "QA V3 A", "email": "a@qa.example.invalid"},
  "grupoEmpresa": {"id": 71001, "nome": "QA V3 Grupo A"}
}
```

A v3 define esses dados de identidade, mas **não fixa os nomes JSON ou o aninhamento**. Esse formato é uma proposta de teste, não contrato oficial. `mensagemInicial` foi enviado junto de `descricao` na maioria dos testes para permitir processamento pelo contrato implementado, sem adotá-lo como nova exigência normativa. Não foram enviados anexos. Somente AUTH-05/06 e suas variantes usam JWT.

## Payload proposto B — plano

Em RF01-08, foram removidos `usuario`/`grupoEmpresa` e enviados:

```json
{
  "usuarioId": 61002,
  "usuarioNome": "QA V3 B",
  "usuarioEmail": "b@qa.example.invalid",
  "grupoEmpresaId": 71002,
  "grupoEmpresaNome": "QA V3 Grupo B"
}
```

Os campos de chamado permaneceram iguais. As duas variantes retornaram 201, mas gravaram usuário 4/grupo 3, sem alterar os dados das entidades. Portanto, os campos adicionais foram aceitos sintaticamente e não utilizados para associação/upsert nos formatos enviados; o DTO corrobora que são ignorados no binding. Não se afirma que todas as estruturas JSON concebíveis foram testadas. A falta de contrato acordado é uma limitação real, não impedimento para registrar a aceitação indevida e o vínculo divergente efetivamente observado.

## Comparação

| Elemento | V3 | Observação real |
|---|---|---|
| API Key | Backend do portal autenticado; chave ausente/inválida → 401 (§6.2). | Ausente, inválida e vazia retornaram 201. Somente JWT e chave inválida + JWT também criaram chamados. |
| JWT | Sem SSO/login direto/validação de JWT (§6.2). | Criação ocorre sem JWT; não é preciso reativar JWT para cumprir a v3. |
| Dados de identidade | Usuário: ID/nome/e-mail; grupo: ID/nome, no corpo. | Dois formatos completos não controlaram vínculos nem atualizações; ausência de usuário/grupo também foi aceita. |
| IDs de portal | Próprio ID do portal deve ser o ID da entidade, não um ID gerado pela ferramenta (§§2.5/6.2). | Foram usados IDs numéricos fictícios, compatíveis com os tipos SQL. IDs pedidos 61092/71092 nem surgiram; chamado aponta 4/3. Fixture `codigo_externo=94001/95001` com IDs reais 2301/3301 não foi tratada como equivalência: RF01-14 exigiu IDs reais 94001/95001 e registrou a falha. |
| Upsert | Criar/atualizar automaticamente sem cadastro prévio. | Sem fixture 4/3: 500 por FK e nenhum upsert. Com fixture: chamados criados, mas usuário/grupo novos ausentes e atributos existentes não atualizados. |
| Produto/categoria | Obrigatórios (§2.1). | Ausência → 400 sem diferenças SQL. |
| Descrição | Obrigatória, sem nome JSON definido. | Sem texto → 400. `descricao` preenchida sem `mensagemInicial` → 400. Compatibilidade de mapeamento pendente; não se declara nome oficial de propriedade. |
| Título | Não consta como obrigatório na v3; opcional no DTO. | Sem título → 201, título resultante `Valid` (fallback observado); código confirmado no SQL. |
| Anexos | Opcionais expressamente (§2.1). | Sem anexos → 201. Upload de arquivos não foi avaliado. |
| SLA prévio | V3 não define resposta de abertura sem configuração. | Produto e categoria sem SLA → 400; sem alterações SQL. Regra da implementação, não requisito inventado para RF01. |
| Código público | Retorno imediato (§2.1). | 35 criações tiveram códigos únicos; todos confirmados por SQL e GET. |

Credenciais foram aleatórias e efêmeras. `X-Api-Key` e `Authorization` aparecem ocultados nas evidências. O nome de configuração `Sso__ApiKey` foi usado porque é o da implementação, sem torná-lo requisito v3. A geração/distribuição de chave ao portal não foi testada.
