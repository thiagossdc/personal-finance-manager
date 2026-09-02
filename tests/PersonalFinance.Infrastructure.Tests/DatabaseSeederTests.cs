using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Abstractions;
using PersonalFinance.Application.Common;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;
using PersonalFinance.Infrastructure.Identity;
using PersonalFinance.Infrastructure.Persistence;

namespace PersonalFinance.Infrastructure.Tests;

public class DatabaseSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesDemoUserAndAccounts()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options, new VersionedEntityInterceptor());
        var hasher = new Pbkdf2PasswordHasher();

        await DatabaseSeeder.SeedAsync(db, hasher);

        (await db.Users.CountAsync()).Should().Be(1);
        (await db.Accounts.CountAsync()).Should().Be(2);
        (await db.Categories.CountAsync()).Should().BeGreaterThanOrEqualTo(4);
        (await db.CreditCards.CountAsync()).Should().Be(1);

        var user = await db.Users.FirstAsync();
        hasher.Verify("Demo@12345", user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options, new VersionedEntityInterceptor());
        var hasher = new Pbkdf2PasswordHasher();

        await DatabaseSeeder.SeedAsync(db, hasher);
        await DatabaseSeeder.SeedAsync(db, hasher);

        (await db.Users.CountAsync()).Should().Be(1);
    }
}
