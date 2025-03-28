using Microsoft.EntityFrameworkCore;
using WordWhisperer.Core.Data.Models;

namespace WordWhisperer.Core.Data;

public class DatabaseContext : DbContext
{
    public DbSet<Word> Words { get; set; } = null!;
    public DbSet<WordVariant> WordVariants { get; set; } = null!;
    public DbSet<Favorite> Favorites { get; set; } = null!;
    public DbSet<History> History { get; set; } = null!;
    public DbSet<Setting> Settings { get; set; } = null!;

    public DatabaseContext(DbContextOptions<DatabaseContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Setting uses Key as primary key
        modelBuilder.Entity<Setting>()
            .HasKey(s => s.Key);

        // Relationship mappings
        modelBuilder.Entity<WordVariant>()
            .HasOne(wv => wv.Word)
            .WithMany()
            .HasForeignKey(wv => wv.WordId);

        modelBuilder.Entity<Favorite>()
            .HasOne(f => f.Word)
            .WithMany()
            .HasForeignKey(f => f.WordId);

        modelBuilder.Entity<History>()
            .HasOne(h => h.Word)
            .WithMany()
            .HasForeignKey(h => h.WordId);

        // Configure indexes
        modelBuilder.Entity<Word>()
            .HasIndex(w => w.WordText)
            .IsUnique();

        modelBuilder.Entity<History>()
            .HasIndex(h => h.Timestamp);

        modelBuilder.Entity<Favorite>()
            .HasIndex(f => f.AddedAt);
    }
} 