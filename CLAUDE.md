# event-arch

API de contas bancárias em DDD, orientada a eventos: o domínio levanta domain
events, que são gravados em um Outbox na mesma transação e publicados no
RabbitMQ para consumers independentes.

## Stack

- C# / .NET 10, Clean Architecture
- ASP.NET Core com Controllers; handlers de comando próprios (sem MediatR)
- EF Core + PostgreSQL (Npgsql)
- Rebus + RabbitMQ, com Outbox próprio
- FluentValidation (validação de entrada na Application)
- Serilog + Seq (logs estruturados, correlação por `CorrelationId`)
- xUnit; Testcontainers nos testes de integração
- docker-compose para Postgres, RabbitMQ e Seq

## Estrutura

| Pasta | Papel |
|---|---|
| `src/EventArch.Domain` | Aggregates, value objects, domain events, `Result`/`Error`. Sem dependências externas |
| `src/EventArch.Application` | Casos de uso (commands/queries + handlers), validators, interfaces de repositório |
| `src/EventArch.Infrastructure` | EF Core, repositórios, Outbox, publicação via Rebus |
| `src/EventArch.Contracts` | Integration events compartilhados com os consumers |
| `src/EventArch.Api` | Controllers, mapeamento `Result` → ProblemDetails, composição |
| `src/EventArch.Statement.Worker` | Consumer de exemplo: monta o extrato a partir dos eventos |
| `tests/EventArch.Domain.Tests` | Testes unitários das regras do domínio |
| `tests/EventArch.IntegrationTests` | Testes ponta a ponta com containers reais |

Regra de dependência: tudo aponta para o `Domain`. O `Worker` só conhece os
`Contracts`, nunca o domínio.

## Comandos

| Ação | Comando |
|---|---|
| Instalar dependências | `dotnet restore && dotnet tool restore` |
| Subir tudo (infra + API + Worker) | `docker compose up -d --build` |
| Subir só a infra (app na IDE) | `docker compose up -d postgres rabbitmq seq` |
| Compilar | `dotnet build` |
| Rodar a API | `dotnet run --project src/EventArch.Api` |
| Rodar o Worker | `dotnet run --project src/EventArch.Statement.Worker` |
| Testar | `dotnet test` |
| Nova migration (API) | `dotnet ef migrations add <Nome> --project src/EventArch.Infrastructure --output-dir Persistence/Migrations` |
| Nova migration (Worker) | `dotnet ef migrations add <Nome> --project src/EventArch.Statement.Worker --output-dir Persistence/Migrations` |

Ferramentas são **locais** (`dotnet-tools.json`), nunca globais. A API aplica
as migrations sozinha ao subir em Development.

## Convenções

- Código, nomes e comentários **em inglês**.
- Variáveis locais e parâmetros em `camelCase`; métodos, tipos e propriedades
  em `PascalCase`.
- Comentários XML curtos em tipos e membros públicos; comentários inline só
  para explicar o *porquê*, nunca o óbvio.
- Regras de negócio vivem no `Domain` e retornam `Result` — sem exceções no
  fluxo esperado. Exceções só para violação de invariante (bug).
- Validação de **forma** da entrada (FluentValidation) fica na `Application`;
  validação que depende do estado do aggregate fica no `Domain`.
- Tudo de EF Core fica na `Infrastructure`: DbContext, mapeamentos,
  migrations e a factory de design-time. A `Api` não referencia EF.
- O `Statement.Worker` é um serviço independente: banco próprio no schema
  `statement`, migrations próprias, e só conhece os `Contracts`.
- Consumers são idempotentes: a entrega é at-least-once. No extrato, o
  `EventId` é a chave primária da linha.
- Todo novo fato de negócio vira um domain event no tempo passado
  (`MoneyDeposited`, não `DepositMoney`).
