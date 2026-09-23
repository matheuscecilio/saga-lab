# Estado atual de implementação

> Atualizado em 22 de setembro de 2026. Leia este arquivo junto com `IMPLEMENTATION_PLAN.md`; ele substitui o status e a versão de MassTransit registrados no plano inicial.

## Decisão obrigatória de licenciamento

O projeto começou com MassTransit `9.2.1`, mas a versão exige licença em tempo de execução para iniciar o transporte RabbitMQ. Como o objetivo do SagaLab é ser 100% gratuito para estudo, toda a família MassTransit foi fixada em `8.5.10`, a última linha permissiva usada neste laboratório:

- `MassTransit`
- `MassTransit.RabbitMQ`
- `MassTransit.EntityFrameworkCore`
- `MassTransit.TestFramework`

As versões estão centralizadas em `Directory.Packages.props`.

## Etapas concluídas

- Bootstrap, convenções de build, NuGet.config, Compose e projetos base.
- Contratos de pedidos, inventário, pagamentos e entrega.
- API `POST /orders`, que publica `SubmitOrder`.
- Consumidores MassTransit para ações e compensações nos três workers.
- Máquina de estados `OrderStateMachine` com correlação por `OrderId`.
- Caminho feliz: reserva -> pagamento -> entrega -> `OrderCompleted`.
- Compensação: falha na entrega -> estorno -> liberação de estoque -> `OrderCancelled`.
- Quatro testes automatizados aprovados: dois de Inventory e dois da Saga.
- Convenção obrigatória de testes: `Metodo_Condicao_ResultadoEsperado`, com `// Arrange`, `// Act` e `// Assert`.

## Validação integrada realizada

RabbitMQ e PostgreSQL foram iniciados pelo Docker Compose e ficaram saudáveis. A API, Orchestrator e workers foram iniciados temporariamente em Release.

Um `POST /orders` retornou `202 Accepted` com um `orderId`. Em seguida, as filas `order-state`, `reserve-inventory`, `authorize-payment` e `create-shipment` foram verificadas pelo RabbitMQ Management e estavam drenadas, sem filas `_error` ou `_skipped`.

Os processos .NET temporários foram encerrados após a validação. RabbitMQ e PostgreSQL continuam em execução para desenvolvimento local.

## Próxima etapa

Implementar persistência da Saga, concorrência otimista, EF Core Outbox e idempotência:

1. Criar `SagaDbContext` e mapeamento de `OrderState` em PostgreSQL.
2. Configurar repositório EF Core para `OrderStateMachine`.
3. Criar migração inicial e aplicar no banco local.
4. Habilitar Entity Framework Outbox nos endpoints que avançam a Saga.
5. Criar testes de reinicialização/duplicidade e de compensações persistidas.

Não substituir o repositório em memória antes de haver migração e teste de persistência funcionando.
