using Assecor.Assessment.Application.Ports;
using Assecor.Assessment.Infrastructure.Csv;
using Assecor.Assessment.Infrastructure.Persistence;
using Assecor.Assessment.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Missing connection string: ConnectionStrings:Default");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repository registration
builder.Services.AddScoped<IPersonRepository, EfPersonRepository>();

// CSV source (copied to output directory by MSBuild)
var csvPath =
    builder.Configuration["CSV_PATH"]
    ?? Path.Combine(AppContext.BaseDirectory, "sample-input.csv");
builder.Services.AddScoped<IPersonCsvSource>(_ => new CsvPersonSource(csvPath));
builder.Services.AddScoped<CsvPersonImporter>();

// Seed on startup
builder.Services.AddHostedService<CsvImportHostedService>();

var app = builder.Build();

//Ensures Migrations are applied in Docker
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware
app.UseHttpsRedirection();
app.MapControllers();

app.Run();