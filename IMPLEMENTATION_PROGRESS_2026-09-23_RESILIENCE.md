# Progresso da implementação — resiliência e observabilidade

Este é o registro mais recente; leia-o com os demais arquivos `IMPLEMENTATION_PROGRESS*` e com `IMPLEMENTATION_PLAN.md` ao retomar o projeto.

## Etapa concluída

- Os workers de Inventory, Payment e Shipping têm retry imediato em 100 ms, 500 ms e 1 s.
- Os mesmos endpoints usam In-Memory Outbox: mensagens publicadas por uma tentativa que falha não são enviadas prematuramente ao broker.
- A Orchestrator mantém o Entity Framework Consumer Outbox criado na etapa anterior, pois ela altera estado persistente.
- API, Orchestrator e workers produzem logs no formato JSON no console.
- A API tem `GET /health`, exposto pelo ASP.NET Core Health Checks e adequado como readiness probe, e `GET /alive` para liveness.
- A submissão de pedido usa `LoggerMessage` gerado em compilação, com `OrderId`, quantidade de itens, valor e moeda como propriedades estruturadas.

## Decisões relevantes

- Não foi configurado redelivery agendado: RabbitMQ no Compose não inclui o plugin de delayed exchange. Retry curto é apropriado para falhas transitórias rápidas; a configuração de redelivery virá junto da infraestrutura que suporte agendamento.
- Não foi adicionada telemetria OpenTelemetry ainda. Os logs JSON e os health checks estabelecem o contrato operacional; a instrumentação OTLP será ligada quando houver um coletor no ambiente Docker/deploy.

## Validação realizada

- Build Release aprovado, sem avisos nem erros.
- Suíte automatizada: 4 testes aprovados.
- API, Orchestrator e os três workers iniciaram com a nova configuração.
- `GET /health` retornou `200 Healthy`; `GET /alive` retornou `200` com o serviço saudável.
- Um pedido percorreu o fluxo completo e foi persistido com estado `Completed`.
- O log JSON contém `EventId` 1000 e as propriedades estruturadas esperadas.
- RabbitMQ não apresentou filas `_error` ou `_skipped`.

## Próxima etapa recomendada

Criar Dockerfiles para API, Orchestrator e workers, atualizar o Compose para executar a solução inteira em containers e adicionar CI com GitHub Actions (restore, build, testes e validação da imagem).
