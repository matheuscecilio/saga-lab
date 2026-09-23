# SagaLab — plano de implementação e continuidade

> Última atualização: 21 de setembro de 2026.
>
> Objetivo: construir um laboratório executável em .NET para estudar uma **Saga orquestrada** com MassTransit, RabbitMQ e PostgreSQL. Este documento é a fonte de contexto para retomar o trabalho caso a conversa seja perdida.

## Como retomar com um assistente

Use um pedido como:

> Analise `IMPLEMENTATION_PLAN.md` e o código atual de `SagaLab`. Continue a partir da primeira etapa não concluída, valide antes de implementar e relate bloqueios.

Regras de continuidade:

- Não reimplementar etapas marcadas como concluídas.
- Validar a solução antes e depois de cada etapa material.
- Preservar `TreatWarningsAsErrors`; avisos são defeitos a corrigir, não a suprimir.
- Preferir mensagens imutáveis e contratos sem dependências de infraestrutura.
- Não publicar imagens, criar repositórios remotos ou fazer deploy sem solicitação explícita.

## Resultado esperado

Um pedido percorre o seguinte fluxo:

```text
Order API --SubmitOrder--> Orchestrator (Saga)
                                |
                                +--> Inventory --InventoryReserved/Failed--+
                                |                                         |
                                +--> Payment --PaymentAuthorized/Failed----+--> Saga
                                |                                         |
                                +--> Shipping --ShipmentCreated/Failed-----+
                                                                          |
                                                  OrderCompleted ou OrderCancelled
```

Em caso de falha após uma etapa bem-sucedida, a Saga emite comandos compensatórios, por exemplo `ReleaseInventory` e `RefundPayment`. O objetivo é consistência eventual, rastreabilidade e idempotência — não uma transação distribuída.

## Decisões de arquitetura

| Tema | Decisão | Motivo |
| --- | --- | --- |
| Runtime | .NET 10, C# com nullable habilitado | Base LTS e moderna para o laboratório. |
| Estilo da Saga | Orquestrada, com `MassTransitStateMachine` | Deixa decisões, transições e compensações visíveis em um único lugar. |
| Broker | RabbitMQ | Fácil de executar localmente e bem suportado pelo MassTransit. |
| Persistência da Saga | PostgreSQL + Entity Framework Core | Estado durável e repetível entre reinicializações. |
| Confiabilidade | Transactional Outbox do MassTransit | Evita perder mensagens entre gravação de estado e publicação. |
| Serviços | API de pedidos, Orchestrator, Inventory, Payment e Shipping | Separação didática de responsabilidades. |
| Versionamento NuGet | Central Package Management | Uma versão por pacote em `Directory.Packages.props`. |
| Testes | xUnit + MassTransit Test Harness | Cobertura de transições e fluxo de mensagens sem depender sempre do RabbitMQ. |
| Observabilidade | logs estruturados, health checks e OpenTelemetry | Correlação e diagnóstico do fluxo assíncrono. |
| CI/CD | GitHub Actions + GHCR | Não requer serviço pago para build/publicação de imagens públicas. |
| Deploy gratuito | Oracle Cloud Always Free, Docker Compose | VM ARM gratuita; requer conta elegível e disponibilidade de capacidade. |

## Estrutura alvo

```text
SagaLab/
├── src/
│   ├── SagaLab.Contracts/             # Mensagens públicas, sem infra
│   ├── SagaLab.Order.Api/             # Entrada HTTP e publicação inicial
│   ├── SagaLab.Orchestrator/          # Máquina de estados e persistência
│   ├── SagaLab.Inventory.Worker/      # Reserva/liberação de estoque
│   ├── SagaLab.Payment.Worker/        # Autorização/estorno de pagamento
│   └── SagaLab.Shipping.Worker/       # Criação/cancelamento de entrega
├── tests/
│   ├── SagaLab.ArchitectureTests/     # Regras de dependência entre projetos
│   └── SagaLab.IntegrationTests/      # Harness e cenários da Saga
├── docker/
│   └── Dockerfile                     # Dockerfile multi-stage reutilizável ou por serviço
├── .github/workflows/
│   ├── ci.yml
│   └── publish.yml
├── compose.yaml
├── Directory.Build.props
├── Directory.Packages.props
├── NuGet.config
└── IMPLEMENTATION_PLAN.md
```

## Estado atual confirmado

### Concluído

- [x] Solução .NET criada com projetos Contracts, API, Orchestrator e três workers.
- [x] `global.json` fixa o SDK `10.0.100`.
- [x] Convenções compartilhadas em `Directory.Build.props`: `net10.0`, nullable, implicit usings, analisadores e avisos como erros.
- [x] Central Package Management configurado em `Directory.Packages.props`.
- [x] `NuGet.config` restringe a restauração ao `nuget.org`, evitando conflito com fontes de máquina.
- [x] `compose.yaml` declara RabbitMQ e PostgreSQL com health checks e volumes locais.
- [x] Referências a `Microsoft.Extensions.Hosting` estão centralizadas e declaradas diretamente nos quatro `.csproj` de workers.
- [x] Workarounds removidos: não há `Directory.Build.targets` locais nem `WorkerBootstrap.cs`.
- [x] Contrato inicial `SubmitOrder` e `OrderItem` criado em `SagaLab.Contracts`.
- [x] `POST /orders` implementado na API: valida entrada, normaliza moeda/SKU, gera `OrderId` quando necessário e publica `SubmitOrder`.
- [x] API configurada para RabbitMQ por `ConnectionStrings:RabbitMq`.
- [x] `GET /health` da API validado localmente.
- [x] `dotnet build SagaLab.sln --configuration Release` concluído sem avisos ou erros.
- [x] `dotnet test SagaLab.sln --configuration Release --no-build` retorna sucesso; ainda não existem casos de teste.
- [x] `docker compose config --quiet` é válido.

### Bloqueio externo atual

O Docker CLI não encontrou o daemon do Docker Desktop no Windows (`//./pipe/docker_engine`). Portanto, ainda não foi possível subir RabbitMQ/PostgreSQL nem validar a publicação real de `POST /orders`.

Para desbloquear:

1. Iniciar o Docker Desktop.
2. Executar `docker compose up -d` na raiz do repositório.
3. Confirmar `docker compose ps` com os dois serviços saudáveis.
4. Fazer uma chamada a `POST /orders` e conferir a mensagem no RabbitMQ Management.

## Contratos de mensagens

### Existentes

`SagaLab.Contracts.Orders` contém:

```csharp
public sealed record OrderItem(string Sku, int Quantity, decimal UnitPrice);

public sealed record SubmitOrder(
    Guid OrderId,
    IReadOnlyCollection<OrderItem> Items,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset SubmittedAt);
```

### A criar antes da Saga

Manter um contrato por mensagem, agrupado por capacidade (`Inventory`, `Payments`, `Shipping`, `Orders`). Todas as mensagens devem incluir `OrderId`; usar `CorrelationId` do MassTransit quando for necessário rastrear a conversa além do pedido.

```text
ReserveInventory         -> InventoryReserved | InventoryReservationFailed
ReleaseInventory         -> InventoryReleased
AuthorizePayment         -> PaymentAuthorized | PaymentAuthorizationFailed
RefundPayment            -> PaymentRefunded
CreateShipment           -> ShipmentCreated | ShipmentCreationFailed
CancelShipment           -> ShipmentCancelled
OrderCompleted
OrderCancelled
```

Convenções:

- Comandos usam verbo imperativo (`ReserveInventory`).
- Eventos descrevem fato no passado (`InventoryReserved`).
- Não expor entidades EF, exceções, strings de conexão nem tipos de infraestrutura.
- Incluir motivo de recusa e identificadores externos nos eventos de falha/sucesso.
- Não alterar campos existentes de uma mensagem publicada; adicionar campos opcionais ou criar uma nova versão.

## Etapas de implementação

### 0. Bootstrap e execução local — concluída

Critérios: solução compila sem avisos, versões centralizadas, Compose sintaticamente válido e configuração segura do NuGet.

### 1. Contrato inicial e entrada HTTP — concluída parcialmente

Já feito: `SubmitOrder`, `OrderItem`, `POST /orders`, validação de entrada e publicação no broker.

Pendente para encerrar a etapa em ambiente integrado:

- Subir Docker e publicar um pedido real.
- Conferir a exchange/fila no RabbitMQ Management (`http://localhost:15672`, usuário/senha padrão local `guest`/`guest`).
- Documentar exemplo `curl` no README.

Exemplo de requisição:

```json
{
  "totalAmount": 199.80,
  "currency": "BRL",
  "items": [
    { "sku": "BOOK-SAGA", "quantity": 2, "unitPrice": 99.90 }
  ]
}
```

### 2. Contratos completos e workers consumidores — próxima etapa

Objetivo: transformar cada worker em um consumidor MassTransit simples e determinístico, sem iniciar ainda a máquina de estados.

Implementar:

1. Criar os comandos/eventos listados na seção de contratos.
2. Adicionar `MassTransit.RabbitMQ` aos workers, com versões centralizadas já existentes.
3. Criar consumidores `ReserveInventoryConsumer`, `AuthorizePaymentConsumer` e `CreateShipmentConsumer`.
4. Implementar respostas de sucesso inicialmente; permitir falhas controladas por configuração para estudar compensação depois.
5. Configurar endpoints nomeados pelo MassTransit, um por capacidade.
6. Adicionar health checks de RabbitMQ aos serviços.
7. Criar testes de consumidor com `MassTransit.Testing`.

Critérios de aceite:

- `SubmitOrder` ainda é publicado pela API.
- Um comando de reserva enviado ao Inventory produz um evento de resultado.
- Não existem referências diretas entre workers; comunicação ocorre somente por contratos/broker.
- Testes verificam publicação de sucesso e de falha controlada.

### 3. Orchestrator e máquina de estados da Saga

Objetivo: centralizar o fluxo do pedido em `OrderStateMachine`.

Modelo sugerido de estado:

```text
Initial
  -> ReservingInventory
  -> AuthorizingPayment
  -> CreatingShipment
  -> Completed

Falha em inventário  -> Cancelled
Falha em pagamento   -> ReleasingInventory -> Cancelled
Falha em entrega     -> RefundingPayment -> ReleasingInventory -> Cancelled
```

Implementar:

1. `OrderState` com `CorrelationId`, `CurrentState`, `OrderId`, dados mínimos do pedido, IDs externos e timestamps.
2. `OrderStateMachine` correlacionando todas as mensagens por `OrderId`.
3. No evento inicial, emitir `ReserveInventory`.
4. Em cada sucesso, emitir o próximo comando.
5. Em cada falha, emitir a compensação necessária na ordem inversa.
6. Publicar `OrderCompleted` ou `OrderCancelled` no estado terminal.
7. Configurar limite de concorrência/prefetch de forma explícita e documentada.

Critérios de aceite:

- Caminho feliz termina em `Completed`.
- Falha de pagamento libera o estoque.
- Falha de entrega estorna pagamento e libera estoque.
- Eventos duplicados não avançam ou compensam duas vezes.

### 4. Persistência, idempotência e Outbox

Objetivo: não perder a Saga nem publicar efeitos duplicados em reinicializações.

Implementar:

1. Adicionar `SagaDbContext` e mapeamento EF Core do `OrderState`.
2. Configurar PostgreSQL no Orchestrator via `Npgsql.EntityFrameworkCore.PostgreSQL`.
3. Usar repositório de Saga EF Core e estratégia de concorrência otimista.
4. Configurar `UseEntityFrameworkOutbox` nos endpoints que gravam estado e publicam mensagens.
5. Criar migrações e documentar os comandos de migração.
6. Garantir idempotência em cada worker com chave de operação/resultado persistida ou inbox quando apropriado.

Critérios de aceite:

- Reiniciar o Orchestrator durante uma Saga não perde o estado.
- A mesma mensagem entregue duas vezes não gera dois pagamentos/envios.
- Falha entre gravação e publicação é recuperada pelo outbox.

### 5. Testes automatizados

Objetivo: tornar os cenários da Saga verificáveis sem inspeção manual.

Criar:

- Testes unitários de validação da API.
- Testes de consumidores com `MassTransit.Testing`.
- Testes da máquina de estados: feliz, falha em estoque, pagamento, entrega e mensagens duplicadas.
- Testes de integração com PostgreSQL/RabbitMQ via Docker Compose ou Testcontainers quando o ambiente suportar Docker.
- Testes de arquitetura: Contracts não referencia infraestrutura; workers não se referenciam entre si.

Pacotes a introduzir de modo centralizado quando esta etapa iniciar:

```text
Microsoft.NET.Test.Sdk
xunit
xunit.runner.visualstudio
MassTransit.Testing
Microsoft.AspNetCore.Mvc.Testing
```

Critérios de aceite:

- `dotnet test` descobre e executa cenários reais.
- Casos de compensação são cobertos.
- A CI bloqueia pull requests com teste ou build falho.

### 6. Docker e experiência de desenvolvimento

Objetivo: executar toda a solução com um único comando.

Implementar:

1. Criar Dockerfiles multi-stage para API e workers.
2. Estender `compose.yaml` com os cinco processos .NET, dependências e health checks.
3. Passar strings de conexão exclusivamente por variáveis de ambiente em containers.
4. Adicionar perfis `infra` (somente RabbitMQ/PostgreSQL) e `full` (solução inteira), se simplificar o uso.
5. Incluir instruções de bootstrap, logs e limpeza de volumes no README.

Comandos esperados:

```powershell
docker compose up -d
docker compose logs -f
docker compose down
```

Não executar `down -v` sem intenção explícita: isso remove os dados locais do PostgreSQL/RabbitMQ.

### 7. Observabilidade e operação

Implementar:

- Health endpoints liveness/readiness em todos os serviços.
- Logs estruturados com `OrderId`/`CorrelationId` em todos os consumidores e na Saga.
- OpenTelemetry para traces, métricas e logs; exportador de desenvolvimento opcional (Aspire Dashboard, Jaeger ou OTLP).
- Names previsíveis para filas e endpoints.
- Política de retry, delayed redelivery e fila de erro documentadas.

Critérios de aceite:

- Um pedido é rastreável ponta a ponta pelo `OrderId`.
- Mensagem defeituosa aparece em fila de erro sem paralisar os consumidores.

### 8. GitHub e CI/CD

Implementar:

1. Criar repositório GitHub quando solicitado.
2. Adicionar `.github/workflows/ci.yml` em `pull_request` e `push` para `main`:
   - checkout;
   - setup .NET pela versão de `global.json`;
   - restore;
   - build Release;
   - test Release;
   - `docker compose config --quiet`.
3. Adicionar workflow de publicação em tags `v*`:
   - login em GHCR com `GITHUB_TOKEN`;
   - build/push de imagens por serviço;
   - tags de versão e `latest` apenas em `main`/release.
4. Configurar permissões mínimas (`contents: read`, `packages: write`).
5. Nunca armazenar senha/URI real no repositório; usar GitHub Secrets para deploy.

Critérios de aceite:

- PR não aprovável com build/teste falho.
- Uma tag produz imagens reproduzíveis no GHCR.

### 9. Deploy gratuito

Opção principal: **Oracle Cloud Always Free** com uma VM Ampere A1, Docker Compose, imagens públicas no GHCR e domínio/IP público opcional.

Passos previstos:

1. Criar conta Oracle e uma instância Always Free disponível na região escolhida.
2. Proteger acesso SSH e firewall; expor somente portas necessárias (normalmente 80/443).
3. Instalar Docker Engine e Compose Plugin.
4. Criar arquivo `.env` no servidor com senhas fortes e variáveis de produção.
5. Baixar `compose.production.yaml` e executar pull/up das imagens do GHCR.
6. Colocar Caddy ou Nginx como proxy reverso para API e TLS automático.
7. Fazer deploy por SSH a partir de GitHub Actions usando chave/secrets — somente após o ambiente estar configurado.
8. Fazer backup do volume do PostgreSQL ou, para um laboratório, declarar explicitamente que os dados são descartáveis.

Limitações conhecidas:

- Capacidade de VM gratuita pode não estar disponível em algumas regiões.
- A conta pode exigir método de pagamento para verificação, apesar de o uso Always Free permanecer gratuito dentro das cotas.
- Serviços e dados em uma única VM não oferecem alta disponibilidade; isso é adequado ao laboratório, não à produção.

Alternativa exclusivamente demonstrativa: rodar toda a solução localmente com Docker Compose e usar GitHub Actions apenas para CI e imagens. Isso é gratuito e evita exposição pública.

## Configuração local

Pré-requisitos:

- .NET SDK compatível com `global.json`.
- Docker Desktop iniciado para fluxo integrado.
- Porta `5672`, `15672` e `5432` livres para a infraestrutura atual.

Comandos de verificação:

```powershell
cd SagaLab
dotnet restore SagaLab.sln
dotnet build SagaLab.sln --configuration Release
dotnet test SagaLab.sln --configuration Release --no-build
docker compose config --quiet
docker compose up -d
docker compose ps
```

Para executar a API:

```powershell
dotnet run --project src/SagaLab.Order.Api/SagaLab.Order.Api.csproj
```

Health check:

```powershell
Invoke-RestMethod http://localhost:<porta>/health
```

Pedido de exemplo:

```powershell
$body = @{
  totalAmount = 199.80
  currency = 'BRL'
  items = @(@{ sku = 'BOOK-SAGA'; quantity = 2; unitPrice = 99.90 })
} | ConvertTo-Json -Depth 4

Invoke-RestMethod -Method Post -Uri http://localhost:<porta>/orders -ContentType 'application/json' -Body $body
```

## Arquivos importantes no estado atual

| Arquivo | Papel |
| --- | --- |
| `global.json` | SDK fixado para a solução. |
| `Directory.Build.props` | Convenções e qualidade de compilação. |
| `Directory.Packages.props` | Versões centralizadas. |
| `NuGet.config` | Fonte e mapeamento de pacotes. |
| `compose.yaml` | RabbitMQ e PostgreSQL locais. |
| `src/SagaLab.Contracts/Orders/OrderContracts.cs` | Contrato inicial do pedido. |
| `src/SagaLab.Order.Api/Program.cs` | Endpoint, validação e publicação MassTransit. |
| `src/SagaLab.Order.Api/Contracts/OrderRequests.cs` | DTOs HTTP, separados do contrato de mensageria. |
| `src/SagaLab.Order.Api/appsettings.json` | URI local do RabbitMQ. |

## Checklist antes de considerar o laboratório pronto

- [ ] Docker inicia RabbitMQ e PostgreSQL localmente.
- [ ] Um `POST /orders` chega ao Orchestrator.
- [ ] Caminho feliz completa pedido e publica `OrderCompleted`.
- [ ] Falhas provocadas em cada worker executam compensações corretas.
- [ ] Estado da Saga persiste em PostgreSQL.
- [ ] Outbox e idempotência impedem perdas/duplicidades.
- [ ] Testes unitários, de Saga e integração executam na CI.
- [ ] Imagens Docker publicadas pelo GitHub Actions.
- [ ] Deploy de demonstração documentado e reproduzível.
- [ ] README contém arquitetura, sequência, configuração e troubleshooting.
