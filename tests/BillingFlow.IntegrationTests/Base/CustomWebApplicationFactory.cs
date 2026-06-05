using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using BillingFlow.Application.Authorization;
using BillingFlow.Infrastructure.Database;
using BillingFlow.Migrations.Programmability.Procedures;

using FluentMigrator.Runner;

using Hangfire;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Respawn;

using Testcontainers.MsSql;

using Xunit;

namespace BillingFlow.IntegrationTests.Base;

/// <summary>
/// Strict Authentication Handler for Integration Tests.
/// Fails explicitly if testing headers are missing to prevent false-positive green tests.
/// </summary>
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Test-Role", out var roleHeader) ||
            !Request.Headers.TryGetValue("Test-UserId", out var userIdHeader))
        {
            //return Task.FromResult(AuthenticateResult.Fail("Explicit authorization headers are missing for this test."));

            // Fail() forcibly rejects the request with 401, which breaks [AllowAnonymous] endpoints like Webhooks.
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userIdHeader.ToString()),
            new(ClaimTypes.Role, roleHeader.ToString())
        };

        if (Request.Headers.TryGetValue("Test-Permissions", out var permissionsHeader))
        {
            var permissions = permissionsHeader.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var permission in permissions)
            {
                claims.Add(new Claim(CustomClaimTypes.Permission, permission.Trim()));
            }
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// WebApplicationFactory utilizing Testcontainers and Respawn.
/// Spins up a real, ephemeral SQL Server Docker container for the duration of the test suite.
/// Guarantees that LINQ translations, foreign keys, and raw SQL queries are evaluated accurately.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder().Build();

    private DbConnection _dbConnection = default!;
    private Respawner _respawner = default!;

    public async Task InitializeAsync()
    {
        // 1. Start the Docker container
        await _dbContainer.StartAsync();

        // 2. Open a persistent connection
        _dbConnection = new SqlConnection(_dbContainer.GetConnectionString());
        await _dbConnection.OpenAsync();

        // 3. Run migrations in a completely isolated DI Container.
        // This ensures the main API Host is oblivious to migrations, mimicking production deployments.
        var migrationServiceProvider = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSqlServer()
                .WithGlobalConnectionString(_dbContainer.GetConnectionString())
                .ScanIn(typeof(AddGetMonthlyRevenueProcedure).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider(false);

        using (var scope = migrationServiceProvider.CreateScope())
        {
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
            runner.MigrateUp();
        }

        // 4. Initialize Respawn Checkpoint mapping
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = ["dbo"],
            // CRITICAL: Do not wipe the FluentMigrator history table!
            TablesToIgnore = ["VersionInfo"]
        });
    }

    /// <summary>
    /// Wipes all tables clean instantly. Called automatically by BaseIntegrationTest between individual tests.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    public new async Task DisposeAsync()
    {
        await _dbConnection.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Safely override configuration using UseSetting to prevent environment variable leakage
        builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());
        builder.UseSetting("JwtSettings:Secret", "SuperSecretTestKeyThatIsAtLeast32BytesLong!!!");
        builder.UseSetting("JwtSettings:Issuer", "TestIssuer");
        builder.UseSetting("JwtSettings:Audience", "TestAudience");

        builder.UseSetting("Stripe:WebhookSecret", "whsec_integration_test_secret_12345");

        builder.ConfigureTestServices(services =>
        {
            // 1. Inject the dynamic Mock Authentication Scheme
            services.AddAuthentication(defaultScheme: "TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });

            // 2. Replace real Hangfire SQL Storage with In-Memory Storage
            var hangfireDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(JobStorage));
            if (hangfireDescriptor != null) services.Remove(hangfireDescriptor);
            services.AddHangfire(config => config.UseInMemoryStorage());

            // 3. Force EF Core to use the Testcontainer connection string explicitly
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<BillingDbContext>));
            if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

            services.AddDbContext<BillingDbContext>((sp, options) =>
            {
                options.UseSqlServer(_dbContainer.GetConnectionString());

                // Use GetRequiredService to fail fast if interceptors are missing
                // Order is critical: 1. Process business logic (Events), 2. Record final state (Audit)
                var domainEventsInterceptor = sp.GetRequiredService<BillingFlow.Infrastructure.Database.Interceptors.DispatchDomainEventsInterceptor>();
                var auditInterceptor = sp.GetRequiredService<BillingFlow.Infrastructure.Database.Interceptors.AuditInterceptor>();

                options.AddInterceptors(domainEventsInterceptor, auditInterceptor);
            });

        });
    }
}
