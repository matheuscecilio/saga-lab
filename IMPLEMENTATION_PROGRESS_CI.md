# Progresso da implementação — Git e CI

Este documento sucede `IMPLEMENTATION_PROGRESS_DOCKER.md`.

## Git local

O diretório foi inicializado como repositório Git com a branch padrão `main`. O `.gitignore` mantém fora do histórico os artefatos `bin/`, `obj/`, resultados de teste, arquivos `.env` e configurações locais de IDE.

Nenhum repositório remoto foi criado ou alterado nesta etapa. Para publicar, crie um repositório vazio na sua conta GitHub e execute os comandos documentados no `README.md`.

## GitHub Actions

O workflow `.github/workflows/ci.yml` é acionado em push, pull request para `main` e execução manual. Ele executa em `ubuntu-latest`:

1. checkout do código;
2. instalação do SDK .NET 10;
3. restore, build Release e testes;
4. validação da configuração Docker Compose;
5. construção das imagens de todos os serviços.

O workflow recebe somente `contents: read`; não consome segredos, não faz push e não publica imagens.

## Atualizações automáticas

`.github/dependabot.yml` agenda verificações semanais para dependências NuGet e para actions usadas no workflow.

## Validação realizada

- build Release: sucesso, sem avisos ou erros;
- testes: 4 aprovados;
- `docker compose config --quiet`: sucesso;
- `git diff --cached --check`: sem erros de whitespace;
- artefatos `bin/` e `obj/`: confirmados como ignorados pelo Git.

## Próxima etapa recomendada

Preparar o deploy gratuito: definir um host de contêiner gratuito compatível com serviços de longa duração, separar credenciais por secrets e criar um workflow de deploy manual, sem misturar a publicação com a CI.
