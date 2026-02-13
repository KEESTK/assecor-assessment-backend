using Assecor.Assessment.Api.Contracts;
using Assecor.Assessment.Application.Ports;
using Assecor.Assessment.Domain.Colours;
using Assecor.Assessment.Domain.Persons;
using Microsoft.AspNetCore.Mvc;

namespace Assecor.Assessment.Api.Controllers;

[ApiController]
[Route("persons")]
public sealed class PersonsController : ControllerBase
{
    private readonly IPersonRepository _repo;

    public PersonsController(IPersonRepository repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PersonResponse>>> GetAll(CancellationToken ct)
    {
        var persons = await _repo.GetAllAsync(ct);
        return Ok(persons.Select(ToResponse).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PersonResponse>> GetById(int id, CancellationToken ct)
    {
        if (id <= 0) return BadRequest("id must be > 0");

        var person = await _repo.GetByIdAsync(PersonId.From(id), ct);
        if (person is null) return NotFound();

        return Ok(ToResponse(person));
    }

    [HttpGet("color/{color}")]
    public async Task<ActionResult<IReadOnlyList<PersonResponse>>> GetByColor(string color, CancellationToken ct)
    {
        FavouriteColour favouriteColour;
        try
        {
            favouriteColour = FavouriteColour.From(color);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or FormatException)
        {
            return BadRequest("Unsupported color.");
        }

        var persons = await _repo.GetByColourAsync(favouriteColour, ct);
        return Ok(persons.Select(ToResponse).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<PersonResponse>> Create([FromBody] CreatePersonRequest request, CancellationToken ct)
    {
        // Basic validation
        if (request is null) return BadRequest("Missing request body.");

        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Lastname) ||
            string.IsNullOrWhiteSpace(request.Zipcode) ||
            string.IsNullOrWhiteSpace(request.City) ||
            string.IsNullOrWhiteSpace(request.Color))
        {
            return BadRequest("All fields (name, lastname, zipcode, city, color) are required.");
        }

        FavouriteColour colour;
        try
        {
            colour = FavouriteColour.From(request.Color);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or FormatException)
        {
            return BadRequest("Unsupported color.");
        }

        // Assign new CsvLineNumber = max + 1
        var all = await _repo.GetAllAsync(ct);
        var nextId = all.Count == 0 ? 1 : all.Max(p => p.Id.Value) + 1;

        var person = new Person(
            Id: PersonId.From(nextId),
            FirstName: request.Name.Trim(),
            LastName: request.Lastname.Trim(),
            ZipCode: request.Zipcode.Trim(),
            City: request.City.Trim(),
            Colour: colour
        );

        await _repo.AddAsync(person, ct);

        var response = ToResponse(person);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    private static PersonResponse ToResponse(Person p) =>
        new(
            Id: p.Id.Value,
            Name: p.FirstName,
            Lastname: p.LastName,
            Zipcode: p.ZipCode,
            City: p.City,
            Color: p.Colour.Value
        );
}