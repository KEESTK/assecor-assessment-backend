using System.Net;
using System.Net.Http.Json;
using Assecor.Assessment.Api.Contracts;
using Assecor.Assessment.Infrastructure.Persistence;
using Assecor.Assessment.Infrastructure.Persistence.Entities;
using Assecor.Assessment.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Assecor.Assessment.Tests.Api;

public sealed class PersonsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PersonsApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task SeedAsync(params PersonEntity[] persons)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await DbReset.ResetAsync(db);

        db.Persons.AddRange(persons);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetPersons_ReturnsList()
    {
        await SeedAsync(
            new PersonEntity { CsvLineNumber = 1, FirstName = "Hans", LastName = "Müller", ZipCode = "67742", City = "Lauterecken", Colour = "blau" },
            new PersonEntity { CsvLineNumber = 2, FirstName = "Peter", LastName = "Petersen", ZipCode = "18439", City = "Stralsund", Colour = "grün" }
        );

        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/persons");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<List<PersonResponse>>();
        Assert.NotNull(body);
        Assert.Equal(2, body!.Count);

        Assert.Equal(1, body[0].Id);
        Assert.Equal("Hans", body[0].Name);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsPerson()
    {
        await SeedAsync(
            new PersonEntity { CsvLineNumber = 7, FirstName = "Anders", LastName = "Andersson", ZipCode = "32132", City = "Schweden - ☀", Colour = "grün" }
        );

        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/persons/7");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<PersonResponse>();
        Assert.NotNull(body);
        Assert.Equal(7, body!.Id);
        Assert.Equal("Schweden - ☀", body.City);
        Assert.Equal("grün", body.Color);
    }

    [Fact]
    public async Task GetById_WhenMissing_Returns404()
    {
        await SeedAsync(); // empty DB

        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/persons/999");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetByColor_FiltersCorrectly_AndIsCaseInsensitive()
    {
        await SeedAsync(
            new PersonEntity { CsvLineNumber = 1, FirstName = "Hans", LastName = "Müller", ZipCode = "67742", City = "Lauterecken", Colour = "blau" },
            new PersonEntity { CsvLineNumber = 2, FirstName = "Peter", LastName = "Petersen", ZipCode = "18439", City = "Stralsund", Colour = "grün" },
            new PersonEntity { CsvLineNumber = 3, FirstName = "Klaus", LastName = "Klaussen", ZipCode = "43246", City = "Hierach", Colour = "grün" }
        );

        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/persons/color/GRUEN");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<List<PersonResponse>>();
        Assert.NotNull(body);
        Assert.Equal(2, body!.Count);
        Assert.All(body, p => Assert.Equal("grün", p.Color));
    }

    [Fact]
    public async Task PostPersons_WithValidBody_Returns201_AndAssignsNextId()
    {
        await SeedAsync(
            new PersonEntity { CsvLineNumber = 10, FirstName = "Klaus", LastName = "Klaussen", ZipCode = "43246", City = "Hierach", Colour = "grün" }
        );

        var client = _factory.CreateClient();

        var req = new CreatePersonRequest(
            Name: "New",
            Lastname: "Person",
            Zipcode: "10115",
            City: "Berlin",
            Color: "blau"
        );

        var resp = await client.PostAsJsonAsync("/persons", req);

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<PersonResponse>();
        Assert.NotNull(body);

        Assert.Equal(11, body!.Id); // max + 1
        Assert.Equal("blau", body.Color);
    }

    [Fact]
    public async Task PostPersons_WithInvalidColor_Returns400()
    {
        await SeedAsync();

        var client = _factory.CreateClient();

        var req = new CreatePersonRequest(
            Name: "New",
            Lastname: "Person",
            Zipcode: "10115",
            City: "Berlin",
            Color: "pink"
        );

        var resp = await client.PostAsJsonAsync("/persons", req);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
    [Fact]
    public async Task PostPersons_WithMissingFields_Returns400()
    {
        await SeedAsync();

        var client = _factory.CreateClient();

        var req = new CreatePersonRequest(
            Name: "",
            Lastname: "Person",
            Zipcode: "10115",
            City: "Berlin",
            Color: "blau"
        );

        var resp = await client.PostAsJsonAsync("/persons", req);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}