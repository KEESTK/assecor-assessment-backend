namespace Assecor.Assessment.Api.Contracts;

public sealed record CreatePersonRequest(
    string Name,
    string Lastname,
    string Zipcode,
    string City,
    string Color
);