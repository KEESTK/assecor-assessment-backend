namespace Assecor.Assessment.Application.Ports;

public sealed record PersonCsvRecord(
    string LastName,
    string FirstName,
    string ZipAndCity,
    int ColourCode
);