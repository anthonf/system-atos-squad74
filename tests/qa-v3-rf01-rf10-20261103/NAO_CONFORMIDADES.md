# Não conformidades — RF01/RF10 v3

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
