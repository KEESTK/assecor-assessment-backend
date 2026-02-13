using Assecor.Assessment.Domain.Colours;

namespace Assecor.Assessment.Domain.Persons;

public sealed record Person(
    PersonId Id,
    string FirstName,
    string LastName,
    string ZipCode,
    string City,
    FavouriteColour Colour
);