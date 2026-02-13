using Assecor.Assessment.Domain.Colours;
using Assecor.Assessment.Domain.Persons;

namespace Assecor.Assessment.Application.Ports;

public interface IPersonRepository
{
    Task<IReadOnlyList<Person>> GetAllAsync(CancellationToken ct = default);

    Task<Person?> GetByIdAsync(PersonId id, CancellationToken ct = default);

    Task<IReadOnlyList<Person>> GetByColourAsync(FavouriteColour colour, CancellationToken ct = default);

    Task<Person> AddAsync(Person person, CancellationToken ct = default);
}