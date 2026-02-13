using Assecor.Assessment.Application.Ports;
using Assecor.Assessment.Domain.Colours;
using Assecor.Assessment.Domain.Persons;

namespace Assecor.Assessment.Infrastructure.Csv;

public sealed class CsvPersonImporter
{
    private readonly IPersonCsvSource _source;

    public CsvPersonImporter(IPersonCsvSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public async Task<IReadOnlyList<Person>> ImportAsync(CancellationToken ct = default)
    {
        var records = await _source.ReadAllAsync(ct);

        var persons = new List<Person>(records.Count);

        for (var i = 0; i < records.Count; i++)
        {
            var r = records[i];
            var id = PersonId.From(i + 1); // logical record index (no header)

            var (zip, city) = SplitZipAndCity(r.ZipAndCity);

            var person = new Person(
                Id: id,
                FirstName: r.FirstName.Trim(),
                LastName: r.LastName.Trim(),
                ZipCode: zip,
                City: city,
                Colour: ColourCodeMapper.FromCode(r.ColourCode)
            );

            persons.Add(person);
        }

        return persons;
    }

    private static (string ZipCode, string City) SplitZipAndCity(string zipAndCity)
    {
        if (string.IsNullOrWhiteSpace(zipAndCity))
            throw new FormatException("ZipAndCity must not be empty.");

        var trimmed = zipAndCity.Trim();
        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
            throw new FormatException($"ZipAndCity must contain zip and city: '{zipAndCity}'");

        var zip = parts[0];

        // Join remainder with single spaces -> guarantees City does not start with spaces
        var city = string.Join(" ", parts.Skip(1)).Trim();

        if (string.IsNullOrWhiteSpace(zip) || string.IsNullOrWhiteSpace(city))
            throw new FormatException($"Invalid ZipAndCity value: '{zipAndCity}'");

        return (zip, city);
    }
}