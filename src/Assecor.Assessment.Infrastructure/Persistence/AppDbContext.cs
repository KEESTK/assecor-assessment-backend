using Assecor.Assessment.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Assecor.Assessment.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<PersonEntity> Persons => Set<PersonEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PersonEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.CsvLineNumber)
                  .IsUnique();

            entity.Property(e => e.FirstName).IsRequired();
            entity.Property(e => e.LastName).IsRequired();
            entity.Property(e => e.ZipCode).IsRequired();
            entity.Property(e => e.City).IsRequired();
            entity.Property(e => e.Colour).IsRequired();
        });
    }
}