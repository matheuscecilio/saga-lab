# SagaLab

Laboratório executável para estudar uma Saga orquestrada com MassTransit, RabbitMQ e PostgreSQL.

## Arquitetura

```text
Order API -> Order Saga -> Inventory Worker
                         Payment Worker
                         Shipping Worker
```

Os serviços trocam comandos e eventos pelo RabbitMQ. O estado da Saga e a outbox transacional são persistidos no PostgreSQL. O fluxo contempla compensações, tentativas, idempotência e logs estruturados.

## Pré-requisitos

- .NET SDK 10
- Docker Desktop ou Rancher Desktop, em modo Linux containers
- Git

## Executar localmente

Na raiz do projeto:

```powershell
docker compose up --build -d
Invoke-RestMethod http://localhost:8080/health
```

- API: `http://localhost:8080`
- RabbitMQ Management: `http://localhost:15672` (`sagalab` / `sagalab`)
- PostgreSQL: `localhost:5432` (`sagalab` / `sagalab`)

Exemplo de pedido:

```powershell
$body = @{
  items = @(@{ sku = 'SKU-001'; quantity = 1; unitPrice = 15.50 })
  totalAmount = 15.50
  currency = 'BRL'
} | ConvertTo-Json -Depth 4

Invoke-RestMethod http://localhost:8080/orders -Method Post -ContentType 'application/json' -Body $body
```

Para encerrar os serviços preservando os dados locais:

```powershell
docker compose down
```

## Validar sem Docker

```powershell
dotnet build SagaLab.sln --configuration Release
dotnet test SagaLab.sln --configuration Release --no-build
```

## CI

O workflow em `.github/workflows/ci.yml` executa em push e pull request para `main`:

1. restore, build e testes .NET;
2. validação da configuração Docker Compose;
3. construção das imagens de todos os serviços.

Ele possui somente a permissão `contents: read`, não usa segredos e não publica imagens. O Dependabot verifica semanalmente dependências NuGet e Actions.

## Publicar no GitHub

Após criar um repositório vazio no GitHub:

```powershell
git remote add origin https://github.com/SEU_USUARIO/SagaLab.git
git add .
git commit -m "chore: bootstrap SagaLab"
git push -u origin main
```

Consulte também [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) e os arquivos `IMPLEMENTATION_PROGRESS*.md` para retomar o projeto em outro contexto.
