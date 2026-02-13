namespace Assecor.Assessment.Application.Ports;

public interface IPersonCsvSource
{
    Task<IReadOnlyList<PersonCsvRecord>> ReadAllAsync(CancellationToken ct = default);
}