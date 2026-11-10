# QA de RF01 RF10 e autenticação por API Key

Suíte xUnit de integração da API real com PostgreSQL 17 descartável. Expressa o comportamento esperado pelo **Termo_Referencia_Residencia_Software.v3.docx**, §§2.1, 2.5, 4 (RF01/RF10), 6.2 e 8. Não consulta a v2 e não corrige produção. O objetivo é reproduzir não conformidades e verificar correções futuras, mesmo com testes vermelhos na implementação atual.

O projeto fica separado de `SupportAtos.Integration.Tests`: aquela suíte contém expectativas anteriores de JWT e um fallback para banco local existente. Esta suíte **não reutiliza esse fallback**, não executa aqueles testes e não foi adicionada à solução para evitar misturar os dois contratos. Execute o `.csproj` indicado abaixo.

## Pré-requisitos e preparação

- SDK .NET 10 ou SDK compatível com `net10.0`; runtime ASP.NET Core/.NET 10.
- Docker CLI no PATH, Docker Desktop/Engine iniciado em modo Linux e acesso à engine pelo usuário do teste. O contexto Docker deve ser local.
- Imagem `postgres:17` disponível; se ausente, Docker tenta obtê-la. Para preparar explicitamente: `docker pull postgres:17`.
- Acesso ao NuGet na primeira restauração, ou pacotes em cache. As versões de xUnit, Test SDK e MVC Testing acompanham as usadas no repositório; não há nova dependência de Testcontainers.
- PowerShell para o launcher (Windows PowerShell ou PowerShell 7); `dotnet test` direto também funciona.

Não é necessário iniciar a API, configurar o banco de desenvolvimento, copiar senhas, executar `docker compose` nem executar `dotnet ef` manualmente. Não configure connection string para esta suíte. Se existirem variáveis `SUPPORTATOS_TEST_CONNECTION`, `QA_CONNECTION_STRING` ou `ConnectionStrings__DefaultConnection`, a execução é bloqueada antes de criar o container; remova essas variáveis somente da sessão dedicada ao QA. Não altere configuração permanente do sistema.

Verificações de preparação, na raiz do repositório:

```powershell
dotnet --info
docker info --format '{{.ServerVersion}}'
dotnet restore ./src/backend/tests/SupportAtos.Qa.Tests/SupportAtos.Qa.Tests.csproj
dotnet build ./src/backend/tests/SupportAtos.Qa.Tests/SupportAtos.Qa.Tests.csproj --no-restore
```

## Executar

Comandos a partir da raiz do repositório:

```powershell
# Todos os 18 cenários, TRX, resumo Markdown e JSON individuais
& ./src/backend/tests/SupportAtos.Qa.Tests/run-qa.ps1

# Somente um grupo
& ./src/backend/tests/SupportAtos.Qa.Tests/run-qa.ps1 -Group AUTH
& ./src/backend/tests/SupportAtos.Qa.Tests/run-qa.ps1 -Group RF01
& ./src/backend/tests/SupportAtos.Qa.Tests/run-qa.ps1 -Group RF10

# Reproduções independentes (cada comando cria outro container/banco)
& ./src/backend/tests/SupportAtos.Qa.Tests/run-qa.ps1 -Scenario AUTH-02
& ./src/backend/tests/SupportAtos.Qa.Tests/run-qa.ps1 -Scenario RF01-13
& ./src/backend/tests/SupportAtos.Qa.Tests/run-qa.ps1 -Scenario RF10-01
& ./src/backend/tests/SupportAtos.Qa.Tests/run-qa.ps1 -Scenario RF10-11-COMPAT
```

O launcher chama somente este projeto, conserva o exit code de `dotnet test` e gera uma pasta nova `TestResults/run-<data>-<id>/`. Se o runner terminar com zero testes ou sem TRX, retorna código 2 em vez de sucesso. Não inicia o Docker Desktop automaticamente. Se a engine não estiver pronta, os cenários recebem diagnóstico de infraestrutura.

Execução direta, sem launcher:

```powershell
dotnet test ./src/backend/tests/SupportAtos.Qa.Tests/SupportAtos.Qa.Tests.csproj --logger trx --results-directory ./src/backend/tests/SupportAtos.Qa.Tests/TestResults/direct
dotnet test ./src/backend/tests/SupportAtos.Qa.Tests/SupportAtos.Qa.Tests.csproj --filter 'Group=AUTH'
dotnet test ./src/backend/tests/SupportAtos.Qa.Tests/SupportAtos.Qa.Tests.csproj --filter 'Group=RF01'
dotnet test ./src/backend/tests/SupportAtos.Qa.Tests/SupportAtos.Qa.Tests.csproj --filter 'Group=RF10'
dotnet test ./src/backend/tests/SupportAtos.Qa.Tests/SupportAtos.Qa.Tests.csproj --filter 'DisplayName~RF10-01'
```

Sem `QA_REPORT_DIRECTORY`, os relatórios JSON/Markdown ficam em `bin/Debug/net10.0/qa-results/<id>/`; a saída de cada teste informa o caminho. `--results-directory` controla o TRX, não essa variável. Prefira o launcher para reunir todos os arquivos numa pasta. `bin/`, `obj/` e `TestResults/` estão ignorados pelo Git desta suíte.

## Cenários e interpretação

| Código | Verificação | Preparação de identidade |
|---|---|---|
| AUTH-01 | Chave válida, sem JWT, permite abertura | A existente + compatibilidade 4/3 |
| AUTH-02/03/04/05/06 | Ausente, inválida, vazia, JWT isolado, inválida+JWT: 401 e estado SQL inalterado | A existente + compatibilidade 4/3 |
| RF01-01 | Usuário/grupo A e autor da interação corretos | A existente + compatibilidade 4/3 |
| RF01-13 | Alternar A/B; confirmar associações distintas e ausência de identidade fixa | A e B existentes + compatibilidade 4/3 |
| RF01-09/10 | Identidade de usuário/grupo omitida impede criação | A existente + compatibilidade 4/3 |
| RF10-01 | Criar usuário e grupo novos automaticamente | Somente SLA; **sem 4/3** |
| RF10-03 | Criar usuário novo em grupo existente | Somente grupo A e SLA; **sem 4/3** |
| RF10-04/05/06 | Atualizar nome/e-mail de A e nome do grupo, preservar IDs/vínculos e não duplicar | A existente + compatibilidade 4/3 |
| RF10-07 | Repetir identidade existente sem duplicação e com dois vínculos corretos | A existente + compatibilidade 4/3 |
| RF10-10 | IDs novos do portal existem como PKs reais e FKs corretas | Somente SLA; **sem 4/3** |
| RF10-11-COMPAT | Diagnóstico complementar: mesmo permitindo a gravação atual por 4/3, a identidade nova deve ser criada e usada | Somente compatibilidade 4/3 e SLA; alvo novo ausente |

Identificadores: A=61001/71001, B=61002/71002, novo=61090/71090. O cenário RF10-03 usa usuário novo 61090/grupo existente 71001. Cada cenário ganha banco vazio próprio; repetir IDs entre cenários não compartilha dados.

**Por que existe compatibilidade 4/3?** O código atual usa `MockCurrentUserProvider` com essas referências fixas. Sem elas, a FK pode falhar antes de observar autenticação, associação ou atualização de atributos. As fixtures de compatibilidade são registros SQL de QA, não mocks de serviços e não implementação de upsert. Esses cenários são marcados no catálogo, nos JSON e no resumo. As verificações continuam exigindo IDs do portal, portanto os vínculos 4/3 falham. Nenhum cenário primário de criação de identidade nova depende da fixture 4/3; RF10-11-COMPAT é diagnóstico separado, nunca substitui RF10-01/03/10.

**Contrato JSON:** a v3 define dados de identidade, mas não fixa nomes nem aninhamento. `QaCase.Post` centraliza a proposta `usuario: {id,nome,email}` e `grupoEmpresa: {id,nome}`. Ela envia também `produto`, `categoria`, `descricao`, `mensagemInicial` e `titulo`. `mensagemInicial` permite atravessar a validação existente, sem virar exigência normativa adicional. Se o contrato de nomes for formalizado de outra forma, atualize esse helper; mantenha a exigência de identidade e IDs reais. Os cenários de ausência aceitam rejeição 4xx sem exigir especificamente 400; 500 não é contado como rejeição controlada bem-sucedida.

**JWT isolado:** o helper gera RS256 efêmero com emissor/audiência e chave pública configurados somente no host de QA. Um token com assinatura válida de teste não substitui a API Key pela v3. Não há token ou chave privada versionados. AUTH-01 demonstra somente processamento de uma chave válida; a validação obrigatória da chave depende também dos negativos.

## Relatórios

Abra `TestResults/run-.../RESUMO.md` e `resultados.trx` (Visual Studio ou leitor de TRX). Os JSON por cenário trazem requisito, expectativa, perfil de compatibilidade, banco exclusivo, payload, HTTP/resposta, snapshots SQL antes/depois e verificações individuais. A saída do xUnit contém as mesmas mensagens úteis e o caminho dos relatórios. Headers de autenticação e connection strings não são gravados.

- **PASSOU:** todas as verificações do cenário passaram. Não significa aprovação de todos os RFs; a aprovação de AUTH-01 isolado, por exemplo, não prova que a chave é obrigatória.
- **FALHOU / REQUISITO:** houve resposta/estado SQL que diverge do contrato exercitado. Um 201 com associação 4/3 falha; asserções não foram adaptadas para aceitar o defeito atual.
- **NÃO EXECUTADO / INFRAESTRUTURA:** Docker indisponível, proteção de isolamento bloqueou execução, criação/migration/consulta de banco falhou, host não iniciou ou descarte falhou. O xUnit marca erro/falha para o comando não terminar com sucesso silencioso; o resumo não trata isso como defeito funcional confirmado.
- **NÃO EXECUTADO / INCONCLUSIVO:** uma condição anterior impediu verificar a próxima. Exemplo: 500 em RF10-01 reprova a abertura esperada, mas associação/atributos ficam inconclusivos. Não transforma uma FK em confirmação automática de todos os defeitos.
- Cenário não selecionado/não alcançado fica **NÃO EXECUTADO**, sem evidência. Uma falha de build/discovery anterior à fixture gera um resumo de infraestrutura pelo launcher.

Exit code não zero é esperado quando houver falhas conhecidas. Em CI, preserve esse código e publique os artefatos; não use `|| true` para ocultá-lo. Os snapshots usam consultas independentes ao PostgreSQL, não apenas os DTOs das respostas. Consultam chamado, interação, usuário, grupo e auditoria; não afirmam auditar todas as tabelas do sistema.

## Isolamento e descarte

`QaPostgres` controla o ciclo de vida sem depender de Testcontainers: inicia um único `postgres:17` por execução, com nome/role aleatórios, senha criptograficamente aleatória em memória, rótulos de propriedade, dados em tmpfs e publicação em `127.0.0.1` numa porta dinâmica. A senha é passada ao Docker por ambiente, não argumento. Como em qualquer PostgreSQL Docker, administradores locais do Docker podem inspecionar o ambiente do container durante sua vida; não são usadas credenciais reais.

Antes de criar bancos, a suíte confere ID/nome/rótulos/tmpfs do container e identidade `current_database()/current_user`. Cria banco `qa_<guid>` novo por teste e exige esquema público vazio. Usa `Database.MigrateAsync()` do projeto real, confirma migrations aplicadas e insere apenas SLA QA/Valid (prioridade Media, 60/480 minutos) e as identidades declaradas para o cenário. Nunca usa `EnsureCreated`, EF InMemory ou banco compartilhado.

O host usa `WebApplicationFactory<Program>`/TestServer em processo, pipeline HTTP real, ambiente `Testing`, EF/Npgsql e PostgreSQL reais. Não abre porta da API e não troca `ICurrentUser`, autenticação, casos de uso ou repositórios. A configuração da conexão é própria e é conferida contra o `DbContext` resolvido da API. O ambiente impede o seeder de Development; os providers de logging são substituídos por um coletor de metadados de erros SQL (SQLSTATE, tabela e constraint), evitando EventLog do Windows e exposição de parâmetros/mensagens formatadas. Não há mock dos comportamentos validados.

Cada teste fecha seu host e descarta somente o banco que registrou como pertencente à execução. A fixture remove somente seu container após reconferir ID/nome/rótulos/tmpfs. Os dados desaparecem ao removê-lo. Se o processo for interrompido à força, o `finally` pode não executar; o nome `atos-qa-<guid>` aparece no resumo. Para uma limpeza manual, primeiro confira `docker inspect <ID>` e os rótulos `atos.qa.owner` e `atos.qa.purpose=rf01-rf10`, depois remova **somente o ID correspondente** com `docker rm --force <ID>`. Não use comandos globais de limpeza. Falhas normais de descarte são reportadas, não ocultadas.

## Após correções

Execute novamente os mesmos comandos. Cada execução produz bancos e relatórios novos; não é necessário apagar ou ajustar dados de testes anteriores. Compare os JSON e o TRX, especialmente os cenários primários RF10 sem compatibilidade. A implementação deve passar pelas expectativas existentes; não substituir 401 por 201, IDs do portal por 4/3 ou inserir previamente os usuários novos para obter verde.

O catálogo de cenários está em `QaScenarios.cs`; o transporte/payload e snapshots em `QaApplication.cs`; segurança Docker/banco em `QaPostgres.cs`; resultados em `QaReport.cs`; execução em `run-qa.ps1`. Nenhum arquivo de produção é alterado por esta suíte.

## Validação desta entrega

Validação em 03/11/2026, branch `feature/testes-api-74`, backend da revisão `dc916a6ecb072b3f4da98776c008111acea64954`. Compilação concluída sem erros/avisos de código. O ambiente disponível utilizou SDK 11 preview com alvo/runtime .NET 10 e Docker 29.9.1; a mensagem NETSDK1057 informa o SDK preview.

- Execução completa com diagnóstico SQL: **18 cenários, 1 passou (AUTH-01), 17 falharam por requisito, zero falhas de infraestrutura**. TRX e evidências locais em `TestResults/run-20261103-162048-d987f0bb/`.
- AUTH-02/03/04/05/06 retornaram 201 e gravaram chamado/interação; RF01 demonstrou vínculos 4/3 e aceitação de identidade omitida. RF10-04/05/06 não atualizaram os atributos enviados; o diagnóstico RF10-11-COMPAT mostrou identidade nova ausente apesar do 201.
- RF10-01/03/10, sem compatibilidade, retornaram 500 com `SQLSTATE=23503`, tabela `chamado`, constraint `FK_chamado_grupo_empresa_grupo_empresa_id`. As pós-condições subsequentes ficaram explicitamente inconclusivas.
- Filtro `-Group AUTH` validado: seis cenários selecionados, um passou/cinco falharam, sem infraestrutura. Evidências em `TestResults/run-20261103-162727-dab21f50/`.
- Filtro individual e proteção de conexão externa validados com `QA_CONNECTION_STRING` apontando para um endereço fictício: somente AUTH-02 selecionado, **NÃO EXECUTADO / INFRAESTRUTURA**, bloqueado antes de criar container/conectar. Evidências em `TestResults/run-20261103-162528-628c791a/`; esse bloqueio intencional não é reprovação funcional da API.
- Os containers da suíte foram descartados; consulta Docker posterior pelo rótulo `atos.qa.purpose=rf01-rf10` não encontrou remanescentes. Nenhum banco preexistente foi utilizado.

Os artefatos de execução são locais e ignorados pelo Git; compartilhe a pasta desejada com os desenvolvedores ou gere nova evidência pelos comandos acima. O teste em processo não cobre proxy/rede de um deployment e não substitui a demonstração formal via Postman prevista no Termo. O formato JSON de identidade continua sendo uma proposta da suíte, centralizada para adaptação quando houver contrato formal.
