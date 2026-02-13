using Assecor.Assessment.Infrastructure.Persistence;
using Assecor.Assessment.Infrastructure.Seeding;
using Assecor.Assessment.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Assecor.Assessment.Tests.Infrastructure;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestConnectionString =
        "Host=localhost;Port=5433;Database=assecor_test;Username=postgres;Password=postgres";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // Remove production DbContext registration
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            // Remove CSV import hosted service (tests seed explicitly)
            var csvHostedService = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IHostedService) &&
                d.ImplementationType == typeof(CsvImportHostedService));

            if (csvHostedService is not null)
                services.Remove(csvHostedService);

            // Re-register DbContext to point to dedicated test DB
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(TestConnectionString));

            // Apply migrations once for the test DB
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        });
    }
}