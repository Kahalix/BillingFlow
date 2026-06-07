using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;

using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Features.Identity.Commands.LoginUser;
using BillingFlow.Application.Features.Identity.Commands.RefreshSession;
using BillingFlow.Application.Features.Identity.Commands.RegisterUser;
using BillingFlow.Domain.Enums;
using BillingFlow.Infrastructure.Database;
using BillingFlow.IntegrationTests.Base;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace BillingFlow.IntegrationTests.EndpointTests;

public class AuthEndpointsTests : BaseIntegrationTest
{
    public AuthEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Register_WhenCallerIsAdmin_ShouldCreateUserAndReturn201Created()
    {
        // 1. Arrange
        var (admin, _) = await DataFactory.CreateUserWithClientAsync(role: Role.Admin);
        var client = CreateAuthorizedClient("Admin", AppPermissions.UsersCreate, admin.Id);

        var request = new RegisterUserCommand("newuser@test.com", "StrongP@ssw0rd!", Role.Customer);

        // 2. Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // 3. Assert (HTTP Layer)
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<RegisterUserResponse>();
        result.Should().NotBeNull();
        result!.UserId.Should().NotBeEmpty();

        // 4. Verify DB State
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        var createdUser = await db.Users.FindAsync(result.UserId);
        createdUser.Should().NotBeNull();
        createdUser!.Email.Should().Be("newuser@test.com");
        createdUser.Role.Should().Be(Role.Customer);
        createdUser.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task Register_WhenCallerIsCustomer_ShouldReturn403Forbidden()
    {
        // 1. Arrange
        var (customer, _) = await DataFactory.CreateUserWithClientAsync(role: Role.Customer);

        // Note: Customer lacks the AppPermissions.UsersCreate permission by definition,
        // but even if they somehow had the permission claim, the RegisterUserPolicy
        // enforcing the RoleHierarchy would block them from creating an Admin.
        var client = CreateAuthorizedClient("Customer", string.Empty, customer.Id);

        var request = new RegisterUserCommand("hacker@test.com", "P@ssword123", Role.Admin);

        // 2. Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokens()
    {
        // 1. Arrange
        // For testing actual login, we bypass the TestEntityFactory because we need to know the raw password,
        // and we need the real PasswordHasher to hash it correctly before saving to the database.
        var rawPassword = "MySecurePassword123!";
        var email = "login_test@domain.com";

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<BillingFlow.Application.Interfaces.IPasswordHasher>();
            var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

            var user = new BillingFlow.Domain.Entities.AppUser(email, hasher.HashPassword(rawPassword), Role.Customer, timeProvider.GetUtcNow());
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var client = Factory.CreateClient(); // Standard anonymous client
        var command = new LoginUserCommand(email, rawPassword);

        // 2. Act
        var response = await client.PostAsJsonAsync("/api/auth/login", command);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokens = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        tokens.Should().NotBeNull();
        tokens!.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refresh_WithValidRefreshToken_ShouldReturnNewTokensAndConsumeOldToken()
    {
        // 1. Arrange - We need a user to log in first to get a valid refresh token.
        var rawPassword = "RefreshPassword123!";
        var email = "refresh_test@domain.com";

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<BillingFlow.Application.Interfaces.IPasswordHasher>();
            var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

            var user = new BillingFlow.Domain.Entities.AppUser(email, hasher.HashPassword(rawPassword), Role.Customer, timeProvider.GetUtcNow());
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var client = Factory.CreateClient();

        // Initial Login to acquire tokens
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginUserCommand(email, rawPassword));
        var initialTokens = await loginResponse.Content.ReadFromJsonAsync<AuthTokensResponse>();

        var refreshCommand = new RefreshSessionCommand(initialTokens!.RefreshToken);

        // 2. Act
        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", refreshCommand);

        // 3. Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var newTokens = await refreshResponse.Content.ReadFromJsonAsync<AuthTokensResponse>();
        newTokens.Should().NotBeNull();
        newTokens!.AccessToken.Should().NotBeNullOrWhiteSpace();
        newTokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
        newTokens.RefreshToken.Should().NotBe(initialTokens.RefreshToken); // Ensure token rotation occurred
    }

    [Fact]
    public async Task Refresh_WithConsumedRefreshToken_ShouldTriggerReplayAttackMitigationAndRevokeSession()
    {
        // 1. Arrange
        var rawPassword = "ReplayPassword123!";
        var email = "replay_test@domain.com";
        Guid userId;

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<BillingFlow.Application.Interfaces.IPasswordHasher>();
            var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

            var user = new BillingFlow.Domain.Entities.AppUser(email, hasher.HashPassword(rawPassword), Role.Customer, timeProvider.GetUtcNow());
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;
        }

        var client = Factory.CreateClient();

        // Initial Login
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginUserCommand(email, rawPassword));
        var initialTokens = await loginResponse.Content.ReadFromJsonAsync<AuthTokensResponse>();

        // First Refresh (Consumes the initial token, issues a new active one)
        var firstRefreshCommand = new RefreshSessionCommand(initialTokens!.RefreshToken);
        await client.PostAsJsonAsync("/api/auth/refresh", firstRefreshCommand);

        // 2. Act - ATTEMPT TO REUSE THE CONSUMED TOKEN (Replay Attack)
        var replayAttackCommand = new RefreshSessionCommand(initialTokens.RefreshToken);
        var replayResponse = await client.PostAsJsonAsync("/api/auth/refresh", replayAttackCommand);

        // 3. Assert Response
        replayResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var errorBody = await replayResponse.Content.ReadAsStringAsync();
        errorBody.Should().Contain("Session compromise detected");

        // 4. Assert DB State 
        // Scoped DB check explicitly bound to the current UserId 
        // to prevent false positives from background seeds or concurrent test data.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            var activeTokensCount = await db.UserTokens.CountAsync(t => t.UserId == userId && t.ConsumedAt == null);
            activeTokensCount.Should().Be(0); // Everything was revoked
        }
    }

    [Fact]
    public async Task LogoutAll_WhenCalled_ShouldRevokeAllActiveRefreshTokensForUser()
    {
        // 1. Arrange - We need a user with multiple active sessions
        var rawPassword = "LogoutAllPassword123!";
        var email = "logout_all_test@domain.com";
        Guid userId;

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<BillingFlow.Application.Interfaces.IPasswordHasher>();
            var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

            var user = new BillingFlow.Domain.Entities.AppUser(email, hasher.HashPassword(rawPassword), Role.Customer, timeProvider.GetUtcNow());
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;
        }

        var anonClient = Factory.CreateClient();

        // Login from "Device A"
        var loginA = await anonClient.PostAsJsonAsync("/api/auth/login", new LoginUserCommand(email, rawPassword));
        loginA.StatusCode.Should().Be(HttpStatusCode.OK);

        // Login from "Device B"
        var loginB = await anonClient.PostAsJsonAsync("/api/auth/login", new LoginUserCommand(email, rawPassword));
        loginB.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

            // Verify pre-condition: User has exactly 2 active sessions
            var activeTokens = await db.UserTokens.CountAsync(t => t.UserId == userId && t.ConsumedAt == null);
            activeTokens.Should().Be(2);

            // Now, we create an authorized client to call the secure endpoint
            var authorizedClient = CreateAuthorizedClient("Customer", string.Empty, userId);

            // 2. Act
            var response = await authorizedClient.PostAsync("/api/auth/logout-all", null);

            // 3. Assert Response
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        // 4. Assert DB State (Both sessions should be revoked)
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            var activeTokensCount = await db.UserTokens.CountAsync(t => t.UserId == userId && t.ConsumedAt == null);
            activeTokensCount.Should().Be(0);
        }
    }
}
