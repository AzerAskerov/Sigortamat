using System;
using Microsoft.EntityFrameworkCore;
using Sigortamat.Models;

namespace Sigortamat.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Yeni sistem
        public DbSet<Queue> Queues { get; set; }
        public DbSet<InsuranceJob> InsuranceJobs { get; set; }
        public DbSet<WhatsAppJob> WhatsAppJobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Yeni Queue sistemi konfiqurasiyası
            modelBuilder.Entity<Queue>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.Priority).HasDefaultValue(0);
                entity.Property(e => e.RetryCount).HasDefaultValue(0);
                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            });

            modelBuilder.Entity<InsuranceJob>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CarNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.VehicleBrand).HasMaxLength(50);
                entity.Property(e => e.VehicleModel).HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Company).HasMaxLength(200);
                entity.Property(e => e.ResultText).HasMaxLength(4000); // ISB.az raw data
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                
                // Foreign key relationship
                entity.HasOne(e => e.Queue)
                      .WithMany()
                      .HasForeignKey(e => e.QueueId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WhatsAppJob>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.MessageText).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.DeliveryStatus).HasMaxLength(50).HasDefaultValue("pending");
                entity.Property(e => e.ErrorDetails).HasMaxLength(1000);
                
                // Foreign key relationship
                entity.HasOne(e => e.Queue)
                      .WithMany()
                      .HasForeignKey(e => e.QueueId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
