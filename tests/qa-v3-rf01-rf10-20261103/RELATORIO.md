# Segunda bateria funcional e de integração — RF01/RF10 — Termo v3

Execução: 03/11/2026, aproximadamente 11:46 (America/Sao_Paulo). Revisão: `97baa8c604a1d69d5636d3577d280ce89078a213`. **RF01 e RF10 não atendem integralmente à v3 nos contratos exercitados.** API Key ausente/inválida é aceita; a identidade do corpo não controla o vínculo e o upsert solicitado não ocorre.

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

Container exclusivo `qa-v3-138ddc9400`, ID `19fed0ac626399869a9efcab3ee95ad165ba433b85b258325d8c8b5ed05b3faf`, imagem postgres:17. Banco `qa_v3_138ddc9400`, role exclusiva `qa_v3`, publicação somente em `127.0.0.1:19051`. Dados em tmpfs, sem volumes compartilhados. **Antes de migrations ou fixtures**, consulta confirmou banco/role e zero tabelas públicas. Evidência: [isolation.json](isolation.json).

API apenas local em `http://127.0.0.1:19053`. Ambiente de execução Production é somente um nome de perfil para impedir seed automático; não foram usados serviços ou dados de produção. Conexão, API Key e chave RSA foram geradas para os processos; senhas/chaves/tokens não foram gravados nos scripts ou relatórios. EventLog desativado apenas no processo para evitar permissão de escrita no log do Windows. Inspeção não encontrou serviço de notificação/HTTP externo registrado no caminho executado; nenhuma chamada a serviço externo real foi realizada.

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
