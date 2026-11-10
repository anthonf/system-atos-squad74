# QA — Título obrigatório na abertura de chamado (issue #74)

- **Responsável:** Gabriel Guimarães (QA)
- **Branch:** `feature/qa-titulo-obrigatorio-74`
- **Base:** `main` (`97baa8c`)
- **Data da execução:** 03/11/2026
- **Escopo:** revisar os testes de abertura de chamado e validar o campo **Título** como obrigatório.
- **Resultado geral:** **REPROVADO** — a API e o domínio aceitam chamado sem título e gravam a categoria no lugar.

---

## 1. Revisão dos testes existentes

| Arquivo | Cobertura atual | Lacuna |
|---|---|---|
| `ChamadoDomainTests.cs` | Criação, SLA, transição de status e interações | Nenhum caso de título ausente/vazio |
| `CriarChamadoUseCaseTests.cs` | Caminho feliz, SLA inexistente e usuário não autenticado | Não envia nem valida `Titulo` |
| `SupportAtosApiTests.cs` | Autenticação, provisionamento, consulta e isolamento por grupo | `CorpoChamado()` não envia `titulo`; nenhum caso de título obrigatório |
| Front-end (`TicketList.jsx`) | Valida `assunto.trim()` antes do envio (linha 93) | Sem teste automatizado; projeto não possui runner de front-end |

## 2. Causa raiz identificada

1. `CriarChamadoInput.Titulo` é opcional: `string? Titulo = null` (`src/backend/src/SupportAtos.Application/DTOs/CriarChamadoInput.cs:7`).
2. `CriarChamadoUseCase` valida produto, categoria e mensagem, mas **não valida título** (`CriarChamadoUseCase.cs:32-45`).
3. `Chamado.Criar` substitui título vazio pela categoria, em vez de rejeitar (`Chamado.cs:127`):

```csharp
var tituloFinal = string.IsNullOrWhiteSpace(titulo) ? categoria.Trim() : titulo.Trim();
```

Consequência: um chamado sem título recebe `201 Created` e o campo `titulo` fica igual à categoria, mascarando o dado faltante.

## 3. Casos de teste adicionados

| ID | Camada | Cenário | Esperado | Obtido | Situação |
|---|---|---|---|---|---|
| CT-01 | Domínio | Título `null`, `""` ou `"   "` | `DomainException("O título é obrigatório.")` | Nenhuma exceção; título vira categoria | **REPROVADO** |
| CT-02 | Domínio | Título válido com espaços nas pontas | Persistir título aparado | Título aparado | APROVADO |
| CT-03 | Caso de uso | Título `null`, `""` ou `"   "` | `ValidationException("O título é obrigatório.")` e nada persistido | Chamado criado com título = categoria | **REPROVADO** |
| CT-04 | Caso de uso | Título válido | Persistir título aparado no chamado | Título aparado | APROVADO |
| CT-05 | API | `titulo` omitido, `null`, `""` ou `"   "` | `400 Bad Request` com mensagem sobre título | `201 Created`; chamado gravado com `titulo = categoria` | **REPROVADO** |
| CT-06 | API | Título válido com espaços | `201 Created` e `titulo` aparado | `201 Created`; título `Erro ao acessar o portal` persistido | APROVADO |
| CT-07 | Front-end | Assunto vazio | Bloquear envio e exibir erro | Bloqueia (`TicketList.jsx:93-95`) | APROVADO por inspeção |

Arquivos alterados:

- `src/backend/tests/SupportAtos.Application.Tests/ChamadoDomainTests.cs`
- `src/backend/tests/SupportAtos.Application.Tests/CriarChamadoUseCaseTests.cs`
- `src/backend/tests/SupportAtos.Integration.Tests/SupportAtosApiTests.cs`

## 4. Evidência de execução

Comando:

```powershell
dotnet test src\backend\tests\SupportAtos.Application.Tests\SupportAtos.Application.Tests.csproj --nologo -v minimal
```

Resultado (íntegra em `resultado-testes-aplicacao.txt`):

```text
Com falha! – Com falha:     6, Aprovado:    28, Ignorado:     0, Total:    34
```

As 6 falhas são exatamente CT-01 (3 variações) e CT-03 (3 variações):

```text
Assert.Throws() Failure: No exception was thrown
Expected: typeof(SupportAtos.Domain.Exceptions.DomainException)
```

```text
Assert.Throws() Failure: No exception was thrown
Expected: typeof(SupportAtos.Application.Exceptions.ValidationException)
```

### Testes de integração (API + Postgres)

Comando:

```powershell
dotnet test src\backend\tests\SupportAtos.Integration.Tests\SupportAtos.Integration.Tests.csproj --nologo -v minimal --filter "FullyQualifiedName~Titulo"
```

Resultado (íntegra em `resultado-testes-integracao-titulo.txt`):

```text
Com falha! – Com falha:     4, Aprovado:     1, Ignorado:     0, Total:     5
```

As 4 falhas de CT-05 mostram que a API **cria o chamado** em vez de rejeitar:

```text
Assert.Equal() Failure: Values differ
Expected: BadRequest
Actual:   Created
```

O teste positivo CT-06 passou, confirmando que o título válido é aparado e persistido.

### Evidência no banco (`supportatos_test`)

Consulta (íntegra em `evidencia-banco.txt`):

```sql
SELECT id, codigo_publico, titulo, categoria, criado_em
FROM chamado ORDER BY id DESC LIMIT 8;
```

```text
 id | codigo_publico |          titulo          | categoria
----+----------------+--------------------------+-----------
 10 | SA-386829039   | Erro ao acessar o portal | Login
  9 | SA-796006080   | Login                    | Login
  8 | SA-541974638   | Login                    | Login
  7 | SA-147990487   | Login                    | Login
  6 | SA-660068389   | Login                    | Login
```

Os chamados 6 a 9 foram criados **sem título** e receberam `titulo = Login` (a categoria). O chamado 10, com título válido, manteve o título enviado.

## 5. Não conformidade NC-01 — Título não é obrigatório

**Regra esperada:** o campo Título é obrigatório na abertura de chamado (issue #74).
**Comportamento observado:** título ausente/vazio é aceito e substituído pela categoria.

**Sugestão de correção (para o time de desenvolvimento):**

1. Tornar `CriarChamadoInput.Titulo` obrigatório (`string Titulo` ou `[Required]`).
2. Validar em `CriarChamadoUseCase`, antes da busca de SLA:
   ```csharp
   if (string.IsNullOrWhiteSpace(input.Titulo))
       throw new ValidationException("O título é obrigatório.");
   ```
3. Remover o fallback de `Chamado.Criar` e lançar `DomainException("O título é obrigatório.")` quando vazio.
4. Atualizar os testes/chamadas que hoje omitem título (ex.: `CorpoChamado()` em `SupportAtosApiTests.cs` e os testes de status em `ChamadoDomainTests.cs`).
5. No front-end, alinhar o rótulo do campo (`Assunto` → `Título`) ao contrato da API e manter a obrigatoriedade; considerar limite de 200 caracteres, conforme o DDL (`docs/03-arquitetura-dados.md:118`).

**Critérios de aceite para revalidar:**

- POST `/api/chamados` sem `titulo` ou com título em branco retorna `400` e não cria chamado.
- POST com título válido retorna `201` e persiste o título aparado.
- Nenhum chamado é criado com `titulo = categoria` por ausência de título.
- Os 6 testes adicionados passam.

## 6. Como reproduzir

```powershell
# testes de unidade (não precisam de banco)
dotnet test src\backend\tests\SupportAtos.Application.Tests\SupportAtos.Application.Tests.csproj

# testes de integração (precisam do Postgres)
docker compose -f src\backend\docker-compose.yml up -d

# na primeira execução o TestWebAppFactory cria/migra o banco supportatos_test.
# Depois, semear o usuário/grupo mock (id 4/3) usados pela API com auth desativada:
Get-Content -Raw src\backend\database\seed_mock.sql | docker exec -i supportatos-db psql -U admin -d supportatos_test

dotnet test src\backend\tests\SupportAtos.Integration.Tests\SupportAtos.Integration.Tests.csproj --filter "FullyQualifiedName~Titulo"
```

## 7. Observações

- A exigência de título obrigatório diverge do QA v3 da branch `feature/testes-api-74`, que classificou “sem título” como APROVADO por o título ser opcional no DTO. Este relatório considera a regra atual da issue #74.
- Os testes de integração de autenticação da `main` estão divergentes do estado do código (`[AUTH-DESATIVADA]` no `Program.cs` e nos controllers); isso é pré-existente e fora do escopo desta tarefa.
