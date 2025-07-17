using System;
using Microsoft.EntityFrameworkCore;
using SigortaYoxla.Models;

namespace SigortaYoxla.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<QueueItem> QueueItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure QueueItem entity
            modelBuilder.Entity<QueueItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CarNumber).HasMaxLength(20);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Message).HasMaxLength(1000);
                entity.Property(e => e.IsProcessed).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.Error).HasMaxLength(500);
            });
        }
    }
}
