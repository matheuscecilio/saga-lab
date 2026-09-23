# Progresso da implementação — conteinerização

Este documento sucede `IMPLEMENTATION_PROGRESS_2026-09-23_RESILIENCE.md` e registra o estágio de Docker concluído.

## Topologia executável

`compose.yaml` agora sobe todos os componentes da solução:

- RabbitMQ e PostgreSQL, com health checks;
- `orchestrator-migrations`, job efêmero que restaura o `dotnet-ef` local e executa a migration `OrderSagaDbContext`;
- orquestrador, iniciado somente após o job de migrations terminar com sucesso;
- workers de inventário, pagamento e entrega;
- API de pedidos, exposta em `http://localhost:8080`.

Os serviços internos usam os nomes DNS do Compose (`rabbitmq` e `postgres`), em vez de `localhost`. As configurações de conexão são injetadas por variáveis de ambiente e as imagens finais executam como o usuário não privilegiado `app`.

Cada executável tem Dockerfile multi-stage próprio. O estágio `build` usa o SDK .NET 10 e o estágio final usa a imagem mínima apropriada: `aspnet` para a API e `runtime` para orquestrador/workers. `.dockerignore` evita enviar artefatos locais de build ao contexto Docker.

## Comandos locais

Na raiz do repositório (`SagaLab`):

```powershell
docker compose up --build -d
docker compose ps
Invoke-RestMethod http://localhost:8080/health
docker compose logs -f order-api orchestrator
```

Para desligar os serviços preservando os dados locais:

```powershell
docker compose down
```

## Validação realizada

- `docker compose config --quiet`: sucesso.
- Construção das imagens da API, orquestrador, três workers e job de migrations: sucesso.
- Job de migrations: sucesso; banco já estava atualizado.
- `GET /health`: `Healthy`.
- `POST /orders`: pedido `893ef6f3-2aeb-4779-ab71-8ee2884a0277` processado até `Completed` em `order_states`.
- RabbitMQ: nenhuma mensagem em filas `_error` ou `_skipped`.

## Ajustes encontrados durante a validação

O job de migrations exigiu `dotnet tool restore` dentro da imagem de build. Também foi necessário definir `entrypoint: ["/bin/sh", "-c"]` e passar o comando como um único item de lista, para que o operador `&&` execute restore e migration na mesma sessão.

## Próxima etapa recomendada

Criar a automação de GitHub: inicialização do repositório, workflow de CI no GitHub Actions (restore, build, testes e validação de imagens) e preparação do deploy gratuito baseado em contêiner.
