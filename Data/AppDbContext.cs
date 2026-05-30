using Microsoft.EntityFrameworkCore;
using ChineseAcademicPortal.Models;

namespace ChineseAcademicPortal;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Существующие DbSet
    public DbSet<Category> Categories { get; set; }
    public DbSet<Link> Links { get; set; }
    public DbSet<Thesis> Theses { get; set; }
    public DbSet<VakJournal> VakJournals { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Опционально: настройка сущности VakJournal
        modelBuilder.Entity<VakJournal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Issn).HasMaxLength(20);
            entity.Property(e => e.NameRu).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.Note).HasMaxLength(1000);
        });
    }
}