# Personal Finance Manager

Aplicativo profissional de gerenciamento financeiro pessoal multiplataforma com arquitetura **Offline-First**, backend REST e sincronização incremental.

## Visão Geral

- **Mobile:** .NET MAUI (C#, XAML, MVVM)
- **Backend:** ASP.NET Core REST API
- **Local:** SQLite (offline)
- **Servidor:** PostgreSQL (produção) / SQLite (desenvolvimento local)
- **Cache:** Redis (opcional)
- **Autenticação:** JWT + Refresh Token
- **Observabilidade:** Serilog, OpenTelemetry (pacotes)

## Arquitetura

```
PersonalFinance.Maui (UI)
        ↓
PersonalFinance.Maui.Core (ViewModels, Sync, SQLite local)
        ↓
PersonalFinance.Application (Services, DTOs, Validators)
        ↓
PersonalFinance.Domain (Entities, Value Objects, Rules)
        ↓
PersonalFinance.Infrastructure (EF Core, Auth, Persistence)
        ↓
PersonalFinance.Api (REST)
```

- **Clean Architecture** com dependências apontando para o domínio
- **DDD** para regras financeiras (Money, Transfer, Balance)
- **Result Pattern** para erros previsíveis
- **Offline-First** no cliente mobile, com push/pull incremental e conflitos
  resolvidos por last-write-wins com versionamento (`Version` + `ClientOperationId`)

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [.NET MAUI workload](https://learn.microsoft.com/dotnet/maui/get-started/installation) (para compilar o app mobile)
- Docker e Docker Compose (para ambiente completo)
- PostgreSQL 16+ (opcional, via Docker)

## Instalação

```bash
git clone <repo-url>
cd "personal finance manager"
dotnet restore
```

## Executando a API (desenvolvimento local)

```bash
cd src/PersonalFinance.Api
dotnet user-secrets set "Jwt:SecretKey" "<chave-aleatória-com-pelo-menos-32-caracteres>"
dotnet run
```

O segredo JWT é lido de **user-secrets** (fora do repositório). Em produção, use
variáveis de ambiente (`Jwt__SecretKey`) — o `docker-compose.yml` já injeta
`JWT_SECRET_KEY` do `.env`.

A API estará disponível em `https://localhost:5001` (Swagger em `/swagger`).

**Usuário demo (seed automático):**
- E-mail: `demo@finance.local`
- Senha: `Demo@12345`

## Executando com Docker

```bash
cp .env.example .env
docker compose up --build
```

API: `http://localhost:8080`  
Health: `http://localhost:8080/health`

## Executando o MAUI

```bash
dotnet workload install maui   # uma vez
cd src/PersonalFinance.Maui
dotnet build -f net10.0-android   # ou net10.0-ios, net10.0-maccatalyst
```

Configure a URL da API em `AppSettings.cs`.

## Testes

```bash
dotnet test
```

## Funcionalidades

- [x] Autenticação (registro, login, refresh, alteração de senha)
- [x] Contas financeiras (CRUD, saldo derivado)
- [x] Transações (receita/despesa)
- [x] Transferências (relação explícita)
- [x] Categorias hierárquicas
- [x] Cartão de crédito e parcelamentos
- [x] Transações recorrentes
- [x] Orçamentos com alertas
- [x] Metas financeiras
- [x] Dashboard com métricas
- [x] Sincronização offline-first (push/pull, retry, conflitos)
- [x] Dark mode (estrutura de recursos)
- [x] Localização pt-BR

## Segurança

- JWT Bearer com refresh token rotacionável; senhas com PBKDF2
- Segredo JWT em **user-secrets** (dev) ou variável de ambiente `Jwt__SecretKey`
  (produção) — nunca commitado; a API falha no startup se a chave tiver menos de
  32 caracteres
- Autorização por `UserId` (proteção IDOR) e rate limiting no `AuthController`

## Licenças

Todas as dependências são open source (MIT, Apache 2.0, BSD-3). As versões e
pacotes ficam centralizados em `Directory.Packages.props`.

## Roadmap

- Exportação PDF/CSV (QuestPDF)
- Gráficos avançados (LiveCharts)
- Notificações push nativas
- Recuperação de senha por e-mail
- Suporte a múltiplas moedas
