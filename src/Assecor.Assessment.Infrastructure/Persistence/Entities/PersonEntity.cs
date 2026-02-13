namespace Assecor.Assessment.Infrastructure.Persistence.Entities;

public sealed class PersonEntity
{
    public int Id { get; set; } // DB primary key

    public int CsvLineNumber { get; set; } // public API id (unique)

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string ZipCode { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Colour { get; set; } = string.Empty;
}