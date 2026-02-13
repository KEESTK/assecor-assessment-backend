using Assecor.Assessment.Application.Ports;
using Assecor.Assessment.Domain.Colours;
using Assecor.Assessment.Domain.Persons;
using Assecor.Assessment.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Assecor.Assessment.Infrastructure.Persistence;

public sealed class EfPersonRepository : IPersonRepository
{
    private readonly AppDbContext _db;

    public EfPersonRepository(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IReadOnlyList<Person>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _db.Persons
            .AsNoTracking()
            .OrderBy(p => p.CsvLineNumber)
            .ToListAsync(ct);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<Person?> GetByIdAsync(PersonId id, CancellationToken ct = default)
    {
        var entity = await _db.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.CsvLineNumber == id.Value, ct);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<Person>> GetByColourAsync(FavouriteColour colour, CancellationToken ct = default)
    {
        var c = colour.Value;

        var entities = await _db.Persons
            .AsNoTracking()
            .Where(p => p.Colour == c)
            .OrderBy(p => p.CsvLineNumber)
            .ToListAsync(ct);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<Person> AddAsync(Person person, CancellationToken ct = default)
    {
        // Enforce uniqueness at application level (DB also has unique index)
        var exists = await _db.Persons.AnyAsync(p => p.CsvLineNumber == person.Id.Value, ct);
        if (exists)
            throw new InvalidOperationException($"Person with CsvLineNumber '{person.Id.Value}' already exists.");

        var entity = ToEntity(person);

        _db.Persons.Add(entity);
        await _db.SaveChangesAsync(ct);

        return person;
    }

    private static Person ToDomain(PersonEntity e) =>
        new(
            Id: PersonId.From(e.CsvLineNumber),
            FirstName: e.FirstName,
            LastName: e.LastName,
            ZipCode: e.ZipCode,
            City: e.City,
            Colour: FavouriteColour.From(e.Colour)
        );

    private static PersonEntity ToEntity(Person p) =>
        new()
        {
            CsvLineNumber = p.Id.Value,
            FirstName = p.FirstName,
            LastName = p.LastName,
            ZipCode = p.ZipCode,
            City = p.City,
            Colour = p.Colour.Value
        };
}