using Microsoft.EntityFrameworkCore;
using ArchiverInfrastructure.Entities;

namespace ArchiverInfrastructure.Data;

/// <summary>
/// DbContext Entity Framework Core для бази даних архіватора.
/// </summary>
public class ArchiverDbContext : DbContext
{
    public DbSet<Archive> Archives { get; set; }
    public DbSet<Entry> Entries { get; set; }
    public DbSet<Operation> Operations { get; set; }

    public ArchiverDbContext(DbContextOptions<ArchiverDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Конфігурація сутності Archive
        modelBuilder.Entity<Archive>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FilePath);
            entity.HasIndex(e => e.Checksum);
            
            entity.HasMany(e => e.Entries)
                .WithOne(e => e.Archive)
                .HasForeignKey(e => e.ArchiveId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Operations)
                .WithOne(e => e.Archive)
                .HasForeignKey(e => e.ArchiveId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Конфігурація сутності Entry
        modelBuilder.Entity<Entry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ArchiveId);
        });

        // Конфігурація сутності Operation
        modelBuilder.Entity<Operation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ArchiveId);
            entity.HasIndex(e => e.Timestamp);
        });
    }
}
