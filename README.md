# Personal Finance Manager

App multiplataforma de finanças pessoais, offline-first, com API REST em .NET e app mobile em .NET MAUI. Os dados ficam no dispositivo e sincronizam com o servidor quando há conexão.

EN: Cross-platform personal finance app, offline-first, built on a .NET REST API and a .NET MAUI mobile app. Data lives on the device and syncs with the server when a connection is available.

## Stack

- **Mobile:** .NET MAUI (MVVM) · **API:** ASP.NET Core
- **Local:** SQLite · **Servidor:** PostgreSQL (prod) / SQLite (dev)
- **Auth:** JWT + refresh token · **Logs:** Serilog

## Arquitetura / Architecture

Clean Architecture, com dependências apontando sempre para o domínio. O app mobile funciona offline (SQLite) e sincroniza por push/pull incremental, resolvendo conflitos por last-write-wins com versionamento (`Version` + `ClientOperationId`).

```
PersonalFinance.Maui        UI (XAML)
PersonalFinance.Maui.Core   ViewModels, sync, SQLite local
PersonalFinance.Application Serviços, DTOs, validação
PersonalFinance.Domain      Entidades, regras, value objects
PersonalFinance.Infrastructure EF Core, auth, persistência
PersonalFinance.Api         REST
```

## Como rodar / Getting started

Requisitos: [.NET SDK 10](https://dotnet.microsoft.com/download). Docker opcional.

```bash
git clone <repo-url> && cd "personal finance manager"
dotnet restore
```

**API (dev):**

```bash
cd src/PersonalFinance.Api
dotnet user-secrets set "Jwt:SecretKey" "<chave com 32+ caracteres>"
dotnet run
```

Swagger em `https://localhost:5001/swagger`. Usuário demo (seed automático): `demo@finance.local` / `Demo@12345`.

**Docker:**

```bash
cp .env.example .env
docker compose up --build
```

API em `http://localhost:8080` · health em `/health`.

**App MAUI:**

```bash
dotnet workload install maui   # uma vez
cd src/PersonalFinance.Maui
dotnet build -f net10.0-android
```

A URL da API fica em `AppSettings.cs`.

## Testes / Tests

```bash
dotnet test
```

## O que já funciona / Features

Autenticação completa, contas, transações, transferências, categorias hierárquicas, cartão de crédito com parcelamentos, transações recorrentes, orçamentos com alertas, metas, dashboard e sincronização offline-first com retry e resolução de conflitos.

## Segurança / Security

- Senhas com PBKDF2; refresh token rotacionável
- Segredo JWT fora do repositório (user-secrets em dev, variável `Jwt__SecretKey` em prod)
- Autorização por `UserId` (proteção IDOR) e rate limiting no auth

## Roadmap

Exportação PDF/CSV, gráficos avançados, notificações push, recuperação de senha e múltiplas moedas.

