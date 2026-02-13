using Assecor.Assessment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Assecor.Assessment.Tests.Infrastructure;

public static class DbReset
{
    public static Task ResetAsync(AppDbContext db)
        => db.Database.ExecuteSqlRawAsync(@"TRUNCATE ""Persons"" RESTART IDENTITY;");
}