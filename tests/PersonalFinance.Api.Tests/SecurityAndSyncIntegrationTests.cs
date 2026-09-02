using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PersonalFinance.Api.Common;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Api.Tests;

/// <summary>
/// Testes de segurança (isolamento entre usuários / IDOR), idempotência de sincronização
/// e integridade financeira (saldo derivado de transferências/transações) via API real.
/// </summary>
public sealed class SecurityAndSyncIntegrationTests : IClassFixture<FinanceWebApplicationFactory>
{
    private readonly FinanceWebApplicationFactory _factory;

    public SecurityAndSyncIntegrationTests(FinanceWebApplicationFactory factory) => _factory = factory;

    private static async Task<string> RegisterAndLoginAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Test User", "Password123!"));
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Password123!"));
        login.EnsureSuccessStatusCode();

        var envelope = await login.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        return envelope!.Data!.AccessToken;
    }

    private static HttpClient CreateAuthorizedClient(FinanceWebApplicationFactory factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<Guid> CreateAccountAsync(HttpClient client, string name, decimal initial)
    {
        var response = await client.PostAsJsonAsync("/api/accounts",
            new CreateAccountRequest(name, AccountType.Checking, initial, "BRL"));
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<AccountDto>>();
        return envelope!.Data!.Id;
    }

    private static async Task<IReadOnlyList<AccountDto>> GetAccountsAsync(HttpClient client)
    {
        var envelope = await client.GetFromJsonAsync<ApiResponse<PaginatedList<AccountDto>>>("/api/accounts");
        return envelope!.Data!.Items;
    }
    [Fact]
    public async Task UserCannotReadAnotherUsersAccount_ShouldReturnNotFound_WhenIdDoesNotBelongToUser()
    {
        var emailA = $"owner_{Guid.NewGuid():N}@test.local";
        var emailB = $"attacker_{Guid.NewGuid():N}@test.local";

        var tokenA = await RegisterAndLoginAsync(_factory.CreateClient(), emailA);
        var tokenB = await RegisterAndLoginAsync(_factory.CreateClient(), emailB);

        var clientA = CreateAuthorizedClient(_factory, tokenA);
        var clientB = CreateAuthorizedClient(_factory, tokenB);

        // Usuário A cria uma conta e obtém o id.
        var create = await clientA.PostAsJsonAsync("/api/accounts",
            new CreateAccountRequest("Conta A", AccountType.Checking, 500m, "BRL"));
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<AccountDto>>();
        var accountId = created!.Data!.Id;

        // Usuário B tenta acessar diretamente a conta do usuário A (IDOR).
        var get = await clientB.GetAsync($"/api/accounts/{accountId}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound, because: "recursos de outro usuário devem ser invisíveis");
    }

    [Fact]
    public async Task Push_DuplicateClientOperation_ShouldNotCreateDuplicateAccount()
    {
        var email = $"sync_{Guid.NewGuid():N}@test.local";
        var token = await RegisterAndLoginAsync(_factory.CreateClient(), email);
        var client = CreateAuthorizedClient(_factory, token);

        var operationId = Guid.NewGuid().ToString();
        var entityId = Guid.NewGuid();
        var payload = JsonSerializer.Serialize(new
        {
            name = "Carteira",
            type = (int)AccountType.Checking,
            initialBalance = 100m,
            currency = "BRL",
            isActive = true
        });

        var pushItem = new SyncPushItem(
            "Account", entityId.ToString(), operationId, SyncChangeType.Create, payload, BaseVersion: 0);
        var request = new SyncPushRequest(new[] { pushItem });

        var first = await client.PostAsJsonAsync("/api/sync/push", request);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstResult = await first.Content.ReadFromJsonAsync<ApiResponse<SyncPushResult>>();
        firstResult!.Data!.Applied.Should().Be(1);

        // Reenvia a MESMA operação (mesma ClientOperationId) — deve ser idempotente.
        var second = await client.PostAsJsonAsync("/api/sync/push", request);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var accounts = await GetAccountsAsync(client);
        accounts.Should().HaveCount(1, because: "a operação duplicada não deve criar outra conta");
    }

    [Fact]
    public async Task Transfer_ShouldKeepBalancesConsistent()
    {
        var email = $"transfer_{Guid.NewGuid():N}@test.local";
        var token = await RegisterAndLoginAsync(_factory.CreateClient(), email);
        var client = CreateAuthorizedClient(_factory, token);

        var srcAcc = await CreateAccountAsync(client, "Origem", 1000m);
        var dstAcc = await CreateAccountAsync(client, "Destino", 100m);

        var transfer = await client.PostAsJsonAsync("/api/transfers", new CreateTransferRequest(
            srcAcc, dstAcc, 100m, DateTime.UtcNow.AddDays(-1), "Transferencia"));
        transfer.StatusCode.Should().Be(HttpStatusCode.OK);

        var accounts = await GetAccountsAsync(client);
        var src = accounts.Single(a => a.Id == srcAcc);
        var dst = accounts.Single(a => a.Id == dstAcc);

        // 1000 - 100 = 900 ; 100 + 100 = 200 (Transfer não cria nem destrói dinheiro).
        src.CurrentBalance.Should().Be(900m);
        dst.CurrentBalance.Should().Be(200m);
    }

    [Fact]
    public async Task IncomeAndExpense_ShouldProduceExpectedBalance()
    {
        var email = $"tx_{Guid.NewGuid():N}@test.local";
        var token = await RegisterAndLoginAsync(_factory.CreateClient(), email);
        var client = CreateAuthorizedClient(_factory, token);

        var acc = await CreateAccountAsync(client, "Conta", 1000m);

        var income = await client.PostAsJsonAsync("/api/transactions", new CreateTransactionRequest(
            acc, TransactionType.Income, 500m, "Salário", DateTime.UtcNow, null, null));
        income.StatusCode.Should().Be(HttpStatusCode.OK);

        var expense = await client.PostAsJsonAsync("/api/transactions", new CreateTransactionRequest(
            acc, TransactionType.Expense, 200m, "Aluguel", DateTime.UtcNow, null, null));
        expense.StatusCode.Should().Be(HttpStatusCode.OK);

        var account = (await GetAccountsAsync(client)).Single(a => a.Id == acc);
        account.CurrentBalance.Should().Be(1000m + 500m - 200m);
    }
}
