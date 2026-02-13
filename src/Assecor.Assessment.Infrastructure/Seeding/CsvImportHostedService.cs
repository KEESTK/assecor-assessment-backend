using Assecor.Assessment.Infrastructure.Csv;
using Assecor.Assessment.Infrastructure.Persistence;
using Assecor.Assessment.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace Assecor.Assessment.Infrastructure.Seeding;

public sealed class CsvImportHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CsvImportHostedService> _logger;

    public CsvImportHostedService(IServiceProvider services, ILogger<CsvImportHostedService> logger)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var importer = scope.ServiceProvider.GetRequiredService<CsvPersonImporter>();

        _logger.LogInformation("CSV import seeding started.");

        var persons = await importer.ImportAsync(cancellationToken);

        foreach (var p in persons)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existing = await db.Persons
                .FirstOrDefaultAsync(x => x.CsvLineNumber == p.Id.Value, cancellationToken);

            if (existing is null)
            {
                db.Persons.Add(new PersonEntity
                {
                    CsvLineNumber = p.Id.Value,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    ZipCode = p.ZipCode,
                    City = p.City,
                    Colour = p.Colour.Value
                });
            }
            else
            {
                // Idempotent upsert (update to match CSV)
                existing.FirstName = p.FirstName;
                existing.LastName = p.LastName;
                existing.ZipCode = p.ZipCode;
                existing.City = p.City;
                existing.Colour = p.Colour.Value;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("CSV import seeding finished. Imported/updated {Count} persons.", persons.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}