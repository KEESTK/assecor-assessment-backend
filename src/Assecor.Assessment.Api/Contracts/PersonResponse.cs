namespace Assecor.Assessment.Api.Contracts;

public sealed record PersonResponse(
    int Id,
    string Name,
    string Lastname,
    string Zipcode,
    string City,
    string Color
);