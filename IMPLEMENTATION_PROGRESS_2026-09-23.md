# Progresso da implementação — 23 de setembro de 2026

Este é o registro de progresso mais recente. Leia-o junto de `IMPLEMENTATION_PLAN.md` e de `IMPLEMENTATION_PROGRESS.md` ao retomar o trabalho.

## Etapa concluída: Saga persistente e outbox transacional

- `OrderStateMachine` deixou o repositório em memória e agora usa `OrderSagaDbContext` com PostgreSQL.
- O repositório usa concorrência pessimista e o lock provider do PostgreSQL.
- A Saga usa `OrderStateDefinition` com retry curto e `UseEntityFrameworkOutbox`.
- O mesmo banco contém `order_states`, `InboxState`, `OutboxMessage` e `OutboxState`.
- O inbox fornece deduplicação de entregas pelo `MessageId`; o outbox só disponibiliza mensagens após a transação da Saga ser concluída.
- `Items` foi removido do estado persistido: ele é usado diretamente no comando inicial `SubmitOrder`, e não é necessário nas etapas posteriores.
- A migração versionada é `20260923002854_InitialOrderSagaPersistence`.
- `dotnet-ef` 10.0.0 está registrado em `dotnet-tools.json`. Para outra máquina, execute `dotnet tool restore` antes dos comandos EF.
- A dependência transitiva vulnerável `System.Security.Cryptography.Xml` foi fixada centralmente em 10.0.12.

## Comandos de banco

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src\SagaLab.Orchestrator\SagaLab.Orchestrator.csproj --startup-project src\SagaLab.Orchestrator\SagaLab.Orchestrator.csproj --context OrderSagaDbContext
```

## Validação realizada

- `dotnet build SagaLab.sln --configuration Release`: aprovado, sem avisos nem erros.
- `dotnet test SagaLab.sln --configuration Release --no-build`: 4 testes aprovados.
- PostgreSQL recebeu a migração e contém as cinco tabelas esperadas.
- Fluxo real validado com API, Orchestrator e os três workers: um pedido atingiu o estado `Completed`, com IDs de reserva, pagamento e entrega persistidos.
- O inbox registrou quatro mensagens recebidas; `OutboxMessage` e `OutboxState` ficaram sem mensagens pendentes depois da entrega.
- RabbitMQ não apresentou filas `_error` ou `_skipped`.

## Próxima etapa recomendada

Adicionar resiliência e observabilidade: políticas de retry/redelivery por worker, health checks, logs estruturados e telemetria. Em seguida, criar Dockerfiles por serviço e o workflow de CI no GitHub Actions.
