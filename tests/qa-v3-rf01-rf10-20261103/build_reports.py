"""Consolida apenas evidencias locais existentes; nao faz HTTP ou acesso a banco."""
import collections
import hashlib
import json
from pathlib import Path

P=Path(__file__).resolve().parent
R=json.loads((P/'results.json').read_text(encoding='utf-8'))
BY={r['id']:r for r in R}

TITLES={
'AUTH-01':'API Key válida','AUTH-02':'Sem API Key','AUTH-03':'API Key inválida','AUTH-04':'API Key vazia','AUTH-05':'Somente JWT','AUTH-06':'Chave inválida com JWT','AUTH-07':'Chave válida sem JWT','AUTH-08':'Negativos com corpo v3 completo',
'RF01-01':'Criação completa','RF01-02':'Sem produto','RF01-03':'Sem categoria','RF01-04':'Sem descrição/texto','RF01-05':'Descrição sem mensagemInicial','RF01-06':'Sem título','RF01-07':'Sem anexos','RF01-08':'Identidade no corpo: aninhado/plano','RF01-09':'Sem dados de usuário','RF01-10':'Sem dados de grupo','RF01-11':'Produto/categoria sem SLA','RF01-12':'Consultar criação persistida','RF01-13':'Alternar usuários/grupos','RF01-14':'IDs do portal diferentes de IDs internos',
'RF10-01':'Usuário e grupo novos','RF10-02':'Usuário e grupo existentes','RF10-03':'Usuário novo em grupo existente','RF10-04':'Atualizar nome do usuário','RF10-05':'Atualizar e-mail do usuário','RF10-06':'Atualizar nome do grupo','RF10-07':'Repetir identidade igual','RF10-08':'Alternar usuários no mesmo grupo','RF10-09':'Alternar grupos distintos','RF10-10':'Preservar IDs do portal','RF10-11':'Falha de autenticação','RF10-12':'Falha de validação',
'INT-01':'Consistência referencial SQL','INT-02':'Ausência de dados parciais','INT-03':'Códigos públicos recuperáveis','INT-04':'Entidades necessárias à associação','INT-05':'Isolamento da identidade entre clientes','INT-06':'Repetição sem duplicar entidades'}

def observed(r):
    if r['attempts']:
        pieces=[]
        for a in r['attempts']:
            b=a['response']['body'];h=a['response']['status']
            v=f"{a['variant']}: HTTP {h}"
            if h==201:
                v+=f"; chamado {b['id']}, vínculo {b['usuarioId']}/{b['grupoEmpresaId']}, código {b['codigoPublico']}"
            elif isinstance(b,dict):
                v+='; '+str(b.get('errors',b.get('detail',b.get('title',''))))
            changes={t:{k:len(v) for k,v in d.items()} for t,d in a['differences'].items() if any(d.values())}
            v+='; Δ '+(str(changes) if changes else 'zero em todas as tabelas')
            pieces.append(v)
        return ' / '.join(pieces)
    if r['id']=='INT-01':return str(r['sql_checks'])+'; todas as verificações = 0'
    if r['id']=='INT-02':return '8 rejeições: 7 HTTP 400 + 1 HTTP 500; snapshots antes/depois iguais'
    if r['id']=='INT-03':return '35 GETs retornaram 200; 35 códigos únicos, correspondentes aos IDs SQL'
    return 'Verificação derivada das requisições e snapshots em '+', '.join(r.get('source_evidence',[]))

def write(name,text): (P/name).write_text(text,encoding='utf-8')
def safe(s):return str(s).replace('|','/').replace('\n',' ')

order=sorted(R,key=lambda r:(['AUTH','RF01','RF10','INT'].index(r['id'].split('-')[0]),r['id']))
matrix=['# Matriz completa — segunda bateria v3\n',
'40 cenários lógicos: **14 APROVADOS, 24 REPROVADOS, 2 NÃO VERIFICADOS, 0 BLOQUEADOS**. Foram 44 requisições principais, 35 GETs imediatos de confirmação e 35 GETs finais, totalizando 114 requisições funcionais. Uma sondagem `/health` adicional não entra nessa contagem. INT-04/05/06 reutilizam evidências reais desta bateria; não representam novas aberturas.\n',
'A v3 é a única fonte normativa: [documento](../Especificacao%20do%20desafio/Termo_Referencia_Residencia_Software.v3.docx). Apenas os negativos de API Key têm HTTP 401 expresso. Outros códigos são observações. Cada JSON contém entradas, respostas, snapshots e diferenças; INT inclui consultas SQL/GETs ou referências aos testes de origem.\n',
'| ID / cenário | Requisito v3 | Esperado | Obtido / HTTP / SQL | Classificação | Justificativa / evidência |',
'|---|---|---|---|---|---|']
for r in order:
    matrix.append('| '+' | '.join([r['id']+' — '+TITLES[r['id']],safe(r['requirement']),safe(r['expected']),safe(observed(r)),r['classification'],safe(r['justification'])+f" [JSON]({r['evidence']})"])+ ' |')
matrix+=['\n## Limites das aprovações\n',
'AUTH-01/07 comprovam processamento sem JWT, não que a chave foi validada. RF01-06/07 aprovam apenas omissão de título/anexos e correspondência código/SQL; a associação permanece reprovada nos cenários próprios. RF10-07/INT-06 demonstram ausência de duplicação de fixtures existentes, não capacidade de upsert: ignorar o corpo também evita duplicatas. INT-01 aprova FKs estruturais, não identidade de negócio. INT-02 considera as rejeições reais; requisições inválidas de autenticação que retornaram 201 são falhas em AUTH e RF10-11.\n',
'RF01-05 foi executado, mas a classificação normativa depende do mapeamento de descrição/mensagemInicial. RF01-11 foi executado, mas a v3 não determina rejeição por ausência de SLA. NÃO VERIFICADO não significa ausência de HTTP nesses dois casos.\n']
write('MATRIZ_TESTES.md','\n'.join(matrix))

write('CONTRATO_HTTP.md',r'''# Contrato HTTP observado × Termo v3

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
''')

write('NAO_CONFORMIDADES.md',r'''# Não conformidades — RF01/RF10 v3

Falhas agrupadas por causa comum; 24 cenários reprovados **não representam 24 defeitos independentes**. Não houve correção, Issue, commit ou PR.

## NC-01 — Autenticação servidor-a-servidor não é imposta

**Exigência:** seção 6.2, 401 sem API Key ou com chave inválida; JWT não substitui chave.

**Evidência:** [AUTH-02](AUTH-02.json), [AUTH-03](AUTH-03.json), [AUTH-04](AUTH-04.json), [AUTH-05](AUTH-05.json), [AUTH-06](AUTH-06.json), [AUTH-08](AUTH-08.json), [RF10-11](RF10-11.json). Todos os casos negativos retornaram 201 e inseriram chamado/interação. AUTH-08 repete cinco variantes com usuário/grupo inexistentes no corpo; não houve entidades de portal novas, mas houve gravação indevida de chamados/interações. RF10-11 também cria chamado apesar de chave inválida. Esperado 401, observado 201. Credenciais não são exibidas.

**Impacto:** permite escrita sem autenticar o backend do portal. A aceitação da chave correta em AUTH-01/07 não comprova sua validação. Causa estática coerente: pipeline de autenticação comentado; conclusão funcional sustentada pelas requisições.

## NC-02 — Identidade enviada no corpo não governa a associação

**Exigência:** RF01 e seção 6.2, associar ao usuário/grupo informado pelo portal.

**Evidência:** [RF01-01](RF01-01.json) envia IDs 61001/71001 e recebe 201, código `SA-084799423`, com usuário 4/grupo 3; SQL e GET confirmam. [RF01-08](RF01-08.json) tenta os formatos aninhado e plano com identidades diferentes e obtém o mesmo vínculo 4/3. [RF01-13](RF01-13.json), [RF10-08](RF10-08.json), [RF10-09](RF10-09.json) e [INT-05](INT-05.json) confirmam repetição com clientes/grupos diferentes. Todos os 35 chamados finais apontam 4/3.

**Impacto:** identidade de negócio e separação entre empresas incorretas nos formatos testados. FKs válidas não tornam esse vínculo correto. Limitação: nomes JSON não são fixados pela v3; as duas estruturas foram explicitamente propostas. A reprovação descreve as requisições executadas e a falta de suporte demonstrado, sem inventar um contrato de nomes oficial.

## NC-03 — Upsert e preservação de IDs não demonstrados no processamento executado

**Exigência:** seções 2.5/6.2, RF10 e aceite §8: criar/atualizar entidades usando os próprios IDs do portal.

**Evidência:** [RF10-01](RF10-01.json) envia usuário 61090/grupo 71090 novos antes de qualquer fixture 4/3: HTTP 500, entidades ausentes e nenhum chamado; [api.log](api.log) registra SQLSTATE 23503 e `FK_chamado_grupo_empresa_grupo_empresa_id`. A dependência dos IDs fixos foi confirmada sem modificar o código. Depois da fixture de compatibilidade, [RF10-03](RF10-03.json) não cria usuário novo em grupo existente; [RF10-04](RF10-04.json), [RF10-05](RF10-05.json), [RF10-06](RF10-06.json) retornam 201 mas mantêm nome/e-mail/nome do grupo anteriores. [RF10-10](RF10-10.json) pede 61092/71092, que não aparecem no SQL; chamado aponta 4/3. [RF01-14](RF01-14.json) também não preserva os IDs reais pedidos 94001/95001.

**Impacto:** criação depende de dados locais de compatibilidade e não realiza o comportamento de sincronização nos payloads exercitados. NC-02 e NC-03 compartilham a dependência estática do provider fixo; não se atribui uma causa independente a cada variação. A verificação não se limita à ausência de campos no código: há HTTP e comparação SQL de inserções/atualizações solicitadas.

## NC-04 — Abertura aceita sem os dados obrigatórios de identidade

**Exigência:** §6.2, usuário e grupo enviados no corpo pelo backend autenticado.

**Evidência:** [RF01-09](RF01-09.json) omite usuário e [RF01-10](RF01-10.json) omite grupo. Ambos retornam 201 e gravam chamado/interação com 4/3. A v3 não define status exato para essa falta; a não conformidade é criar a abertura com identidade fixa sem os dados exigidos, não deixar de usar um código HTTP inventado.

## Pontos não classificados como defeito normativo confirmado

- [RF01-05](RF01-05.json): descrição preenchida sem mensagemInicial recebe 400. Restrição extra ou mapeamento de descrição exige contrato acordado; NÃO VERIFICADO quanto à compatibilidade normativa desse nome JSON.
- [RF01-11](RF01-11.json): ausência de SLA recebe 400; v3 não define rejeição/aceitação nessa condição. NÃO VERIFICADO quanto ao critério oficial.
- INT-02 não encontrou gravações parciais nas oito rejeições reais. Os IDs de sequência podem avançar mesmo em rollback; lacunas de IDs não são registros parciais nem defeito por si só.
- RF10-07/INT-06 não duplicaram fixtures existentes. Isso é evidência limitada, compatível também com ignorar a identidade; não aprova RF10 integralmente.
''')

counts=collections.Counter(r['classification'] for r in R)
runtime=json.loads((P/'runtime.json').read_text(encoding='utf-8'))
iso=json.loads((P/'isolation.json').read_text(encoding='utf-8'))
final=json.loads((P/'final-state.json').read_text(encoding='utf-8'))
report=f'''# Segunda bateria funcional e de integração — RF01/RF10 — Termo v3

Execução: 03/11/2026, aproximadamente 11:46 (America/Sao_Paulo). Revisão: `{runtime['git_head']}`. **RF01 e RF10 não atendem integralmente à v3 nos contratos exercitados.** API Key ausente/inválida é aceita; a identidade do corpo não controla o vínculo e o upsert solicitado não ocorre.

## 1. Resultado e alcance

| Grupo | APROVADO | REPROVADO | NÃO VERIFICADO | BLOQUEADO |
|---|---:|---:|---:|---:|
| AUTH | 2 | 6 | 0 | 0 |
| RF01 | 6 | 6 | 2 | 0 |
| RF10 | 2 | 10 | 0 | 0 |
| INT | 4 | 2 | 0 | 0 |
| **Total** | **14** | **24** | **2** | **0** |

Foram executadas **114 requisições funcionais**: 44 principais (43 POST, 1 GET), 35 GETs de confirmação imediata e 35 GETs finais. Houve uma sondagem `/health` separada. Os POSTs resultaram em 35 respostas 201, sete 400 e uma 500. Todos os 71 GETs funcionais retornaram 200. Os 40 cenários lógicos incluem verificações SQL e verificações derivadas de chamadas da própria bateria; não equivalem a 40 requisições independentes. AUTH-08 contém cinco variantes; outros cenários também têm variantes.

A [matriz completa](MATRIZ_TESTES.md) apresenta os 40 cenários com expectativa, HTTP/SQL observado, classificação, justificativa e evidência individual. O arquivo [results.json](results.json) contém os mesmos resultados estruturados. Falhas que compartilham causa foram agrupadas em [NAO_CONFORMIDADES.md](NAO_CONFORMIDADES.md).

## 2. Fonte normativa e preparação

Única norma: [Termo_Referencia_Residencia_Software.v3.docx](../Especificacao%20do%20desafio/Termo_Referencia_Residencia_Software.v3.docx), lido integralmente, especialmente §§2.1, 2.5, 4, 6.2 e 8. A [revisão anterior](../qa-rf01-20261103/RF01-Revisao-Termo-v3.md) e evidências anteriores foram consultadas como histórico. Não se exigiu JWT, SSO ou login na ferramenta. Só o negativo de API Key tem HTTP 401 expresso; 201/200/400/500 nos demais casos são resultados observados, sem inventar exigência normativa de status.

Backend .NET/EF Core/PostgreSQL inspecionado: controller, DTO, caso de uso, provider, DI e configuração do seeder. A compilação nova passou, com zero avisos/erros; SDK 11 preview e alvo net10.0. A primeira compilação encontrou bloqueio de NuGet; a compilação autorizada com `--ignore-failed-sources` restaurou os projetos e concluiu. Não se alteraram fontes. Migrations existentes foram aplicadas pelo EF instalado diretamente aos assemblies compilados; [migrations.log](migrations.log) registra as três migrations. A consulta inicial à tabela de histórico ausente em banco vazio foi seguida da aplicação normal, confirmada pelos snapshots.

## 3. Isolamento e fixtures

Container exclusivo `{iso['container_name']}`, ID `{iso['container_id']}`, imagem postgres:17. Banco `{iso['before_writes']['database']}`, role exclusiva `{iso['before_writes']['role']}`, publicação somente em `127.0.0.1:{iso['port']['HostPort']}`. Dados em tmpfs, sem volumes compartilhados. **Antes de migrations ou fixtures**, consulta confirmou banco/role e zero tabelas públicas. Evidência: [isolation.json](isolation.json).

API apenas local em `{runtime['base_url']}`. Ambiente de execução Production é somente um nome de perfil para impedir seed automático; não foram usados serviços ou dados de produção. Conexão, API Key e chave RSA foram geradas para os processos; senhas/chaves/tokens não foram gravados nos scripts ou relatórios. EventLog desativado apenas no processo para evitar permissão de escrita no log do Windows. Inspeção não encontrou serviço de notificação/HTTP externo registrado no caminho executado; nenhuma chamada a serviço externo real foi realizada.

Fixtures iniciais: usuários existentes 61001/61002/61003, grupos 71001/71002; fixture legada usuário 2301/grupo 3301 com `codigo_externo` 94001/95001 para testar distinção de IDs. E-mails fictícios `qa.example.invalid`. SLA QA/Valid ativo, prioridade Media, resposta 60 min, resolução 480 min. Produto/categoria sem SLA não foram configurados. IDs 61090/71090, 61091, 61092/71092 e demais identidades novas estavam ausentes antes dos respectivos testes.

O [estado inicial](initial-state.json) foi salvo depois dessas fixtures, **sem usuário 4/grupo 3**. RF10-01 foi executado nessa fase para verificar abertura sem cadastro prévio. Retornou 500 por FK, sem criar usuário/grupo solicitados ou chamado. Só então foi adicionada uma [fixture de compatibilidade 4/3](compatibility-fixture.json), registrada com snapshots próprios, para prosseguir e observar outros comportamentos. Não foi correção de implementação nem upsert pela API. [fixtures.sql](fixtures.sql) permite reproduzir os dados iniciais.

## 4. Respostas às questões de QA

| Questão | Resposta baseada na execução |
|---|---|
| RF01 atende integralmente? | **Não.** Criação/código/persistência funcionam sob fixture de compatibilidade, mas identidade e autenticação divergem. |
| API Key é obrigatória e validada? | **Não nesta execução.** Sem chave, inválida, vazia, somente JWT e chave inválida + JWT retornaram 201 e gravaram. AUTH-02/03/04/05/06/08. |
| Criação funciona sem JWT? | **Sim, em escopo limitado:** 201 e código SQL/GET com chave válida e sem JWT, após fixture 4/3. AUTH-01/07. Isso não aprova vínculo nem validação da chave. |
| Dados de usuário/grupo são recebidos no corpo? | Dois formatos completos foram enviados e aceitos sintaticamente, mas **não usados** para associação/upsert. DTO não os declara. RF01-08. |
| Chamados são associados corretamente? | **Não nos formatos testados.** Todos os 35 apontam usuário 4/grupo 3, independentemente dos IDs do corpo. RF01-01/08/13/14 e INT-05. |
| RF10 realiza upsert automático? | **Não nos casos exercitados.** Sem mock: 500; depois, novos ausentes e atributos existentes inalterados apesar de 201. RF10-01/03/04/05/06. |
| IDs do portal são preservados? | **Não.** IDs novos pedidos não aparecem como IDs reais das entidades ou dos vínculos. `codigo_externo` não foi aceito como equivalente. RF10-10/RF01-14. |
| Há criação indevida ou persistência parcial? | **Criação indevida, sim:** autenticação inválida ou identidade ausente gerou chamado/interação. **Gravações parciais nas rejeições reais, não observadas:** oito respostas 400/500 mantiveram snapshots de todas as tabelas iguais. |
| Não conformidades comprovadas? | NC-01 autenticação; NC-02 associação; NC-03 upsert/IDs; NC-04 identidade ausente aceita. Ver [defeitos e evidências](NAO_CONFORMIDADES.md). |
| Não verificados/bloqueados? | RF01-05: descrição × mensagemInicial depende de contrato acordado. RF01-11: v3 não define abertura sem SLA. Ambos tiveram requisição real; insuficiência é normativa. **Nenhum bloqueado.** |

## 5. Evidências principais

- [RF01-01](RF01-01.json): corpo usuário 61001/grupo 71001 → 201, `SA-084799423`, persistido com 4/3.
- [RF01-08](RF01-08.json): variantes aninhada/plana → 201, ambas vinculadas a 4/3. [RF01-13](RF01-13.json): alternância explícita A/B no corpo não controla o vínculo.
- [RF10-01](RF10-01.json): entidades novas sem mock → 500; [api.log](api.log) registra SQLSTATE 23503/FK do grupo. Nenhum upsert ou resíduo SQL nas tabelas.
- [RF10-04](RF10-04.json), [RF10-05](RF10-05.json), [RF10-06](RF10-06.json): nomes/e-mail enviados modificados permanecem anteriores no SQL, com chamado criado em outro vínculo.
- [AUTH-08](AUTH-08.json): cinco variantes negativas com identidade nova e corpo completo retornam 201. Nenhum novo usuário/grupo, mas cinco chamados/interações indevidos.
- [INT-01](INT-01.json): zero órfãos/diferenças de grupo estrutural; integridade referencial aprovada não implica correção da identidade de negócio.
- [INT-02](INT-02.json): sete rejeições 400 e uma 500 sem diferenças nos snapshots. Lacuna de sequência após rollback não foi tratada como resíduo.
- [INT-03](INT-03.json): os 35 códigos são únicos e recuperáveis; [estado final](final-state.json) contém 35 chamados e 35 interações.

## 6. Limites e interpretação

**Causa comum:** o contrato implementado usa provider fixo 4/3 e não suporta os dados de identidade nos dois formatos testados; autenticação está desativada no pipeline inspecionado. Esses achados estáticos explicam resultados funcionais, mas as classificações são sustentadas pelas requisições e SQL. Os 24 cenários reprovados não devem ser contados como 24 defeitos independentes.

A v3 não fixa nomes JSON. Foi usada proposta aninhada e uma alternativa plana, documentadas em [CONTRATO_HTTP.md](CONTRATO_HTTP.md). A conclusão sobre os formatos é limitada ao que foi enviado; não se afirma ter testado toda estrutura concebível. Ainda assim, aberturas aceitas com vínculo diferente do corpo e sem upsert são resultados reais, e a falta de contrato de identidade é uma incompatibilidade estática adicional.

RF01-05: `descricao` preenchida sem `mensagemInicial` recebeu 400; compatibilidade dessa restrição requer contrato. RF01-11: produto/categoria sem SLA receberam 400 sem alterações; a v3 não fornece critério de rejeição/aceitação. A regra implementada não foi promovida a norma.

As 14 aprovações são propriedades delimitadas: processamento sem JWT, obrigatoriedade de campos, opcionalidade de título/anexos, persistência/códigos, FKs, ausência de resíduos em rejeições reais e ausência de duplicação de entidades já existentes. RF10-07/INT-06 não provam capacidade de upsert, pois ignorar a entrada também evita duplicatas. INT-04 verifica a pós-condição de entidades/vínculo, sem afirmar ordem temporal interna observada por trace. Não foram avaliados anexos enviados, carga, concorrência, RF02, webhooks ou demais requisitos funcionais.

Não houve Postman nesta execução: o executor foi Python com HTTP real. Isso fornece evidências reproduzíveis, mas não substitui a entrega formal da collection e a demonstração pelo Postman prevista na v3. O arquivo Postman entregue como apoio permite reexecução futura; não se declara que ele foi executado.

## 7. Reprodução e encerramento

Scripts [run_qa.py](run_qa.py) e [build_reports.py](build_reports.py) não contêm credenciais; o segundo só lê evidências e escreve relatórios. Em PowerShell, a partir da raiz:

```powershell
dotnet build src/backend/src/SupportAtos.Api --ignore-failed-sources
# Copie run_qa.py e build_reports.py para outra pasta nova, diretamente sob a raiz.
# O executor recusa sobrescrever uma pasta que ja contenha initial-state.json.
python -X utf8 caminho-da-nova-pasta/run_qa.py
python -X utf8 caminho-da-nova-pasta/build_reports.py
```

Pré-requisitos: Docker com postgres:17, .NET compatível com net10.0, dotnet-ef instalado, Python/cryptography. Portas e nome de container são escolhidos para cada execução; segredos são gerados em memória. Todos os POSTs e diferenças são registrados; os GETs auxiliares têm resposta integral nos JSON. O [execution.log](execution.log) contém a transcrição sem segredos. [schema.json](schema.json) registra colunas e constraints. Snapshots cobrem todas as tabelas públicas, não apenas chamado/interação.

API própria encerrada e container temporário removido com conferência de ID/nome/rótulo exclusivo: [cleanup.json](cleanup.json). Não foram removidos containers da bateria anterior. [original-integrity-before.json](original-integrity-before.json) e [original-integrity-after.json](original-integrity-after.json) confirmam **nenhuma alteração nos arquivos originais rastreados, evidências anteriores e Termo v3**. A exclusão prévia da v2 e a presença não rastreada da v3 já constavam no estado inicial; não foram ações desta bateria. Nenhum código, migration ou configuração permanente foi editado; nenhum banco original foi acessado para escrita. Não houve correção, commit, PR ou Issue.

**Parecer:** RF01 reprovado no atendimento integral, e RF10 reprovado nos cenários de sincronização executados com os formatos documentados. Há funcionamento parcial de criação/persistência, mas faltam autenticação obrigatória, identidade correta e sincronização exigidas para aceite integrado no §8. A bateria encerra em RF01/RF10 e aguarda avaliação de QA.
'''
write('RELATORIO.md',report)

# Complemento fiel da transcricao: as respostas abaixo ja foram coletadas durante
# run_qa.py, nao ha novas chamadas. Evita perder detalhes dos GETs auxiliares no log.
with (P/'execution.log').open('a',encoding='utf-8') as log:
    log.write('\nCONSOLIDACAO DOS GETS AUXILIARES JA EXECUTADOS (sem repeticao de requisicoes)\n')
    for r in R:
        for a in r['attempts']:
            if 'public_code_verification' in a:
                v=a['public_code_verification']
                log.write(json.dumps(dict(source=r['id'],method='GET',url=runtime['base_url']+'/api/chamados/'+str(v['code']),headers={'X-Api-Key':'[CHAVE QA VALIDA OCULTA]'},response=v['retrieval']),ensure_ascii=False)+'\n')
    for v in BY['INT-03']['retrievals']:log.write(json.dumps(v,ensure_ascii=False)+'\n')

# Collection de apoio: exportada dos pedidos reais, com variaveis, sem executar.
items=[]
for r in order:
    for a in r['attempts']:
        req=a['request'];headers=[]
        for k,v in req['headers'].items():
            if k.lower()=='authorization':v='Bearer {{qa_jwt}}'
            elif v=='[CHAVE QA VALIDA OCULTA]':v='{{qa_api_key}}'
            elif v=='[CHAVE QA INVALIDA OCULTA]':v='{{qa_invalid_key}}'
            headers.append(dict(key=k,value=v,type='text'))
        headers.append(dict(key='Content-Type',value='application/json',type='text'))
        request=dict(method=req['method'],url=req['url'].replace(runtime['base_url'],'{{base_url}}'),header=headers,description=r['expected']+' Resultado historico: '+r['classification']+'. Collection exportada, NAO executada no Postman.')
        if req['body'] is not None:request['body']=dict(mode='raw',raw=json.dumps(req['body'],ensure_ascii=False,indent=2),options=dict(raw=dict(language='json')))
        items.append(dict(name=r['id']+' '+a['variant'],request=request,response=[]))
collection=dict(info=dict(name='QA v3 RF01 RF10 — apoio de reproducao, nao executado no Postman',schema='https://schema.getpostman.com/json/collection/v2.1.0/collection.json'),variable=[dict(key='base_url',value='http://127.0.0.1:PORTA_QA'),dict(key='qa_api_key',value=''),dict(key='qa_invalid_key',value=''),dict(key='qa_jwt',value='')],item=items)
write('QA-v3.postman_collection.json',json.dumps(collection,ensure_ascii=False,indent=2))
manifest={x.name:hashlib.sha256(x.read_bytes()).hexdigest() for x in P.iterdir() if x.is_file() and x.name!='evidence-sha256.json'}
write('evidence-sha256.json',json.dumps(manifest,indent=2))
print('Relatorios consolidados. Cenarios:',len(R),'Classificacoes:',dict(counts))
