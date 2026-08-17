using Microsoft.EntityFrameworkCore;
using PetMarketplaceAPI.Models;

namespace PetMarketplaceAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Pet> Pets { get; set; }
        public DbSet<PetImage> PetImages { get; set; }
        public DbSet<VaccinationRecord> VaccinationRecords { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<PetServiceProvider> PetServiceProviders { get; set; }
        public DbSet<Negotiation> Negotiations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Pet relationships
            modelBuilder.Entity<Pet>()
                .HasOne(p => p.Seller)
                .WithMany()
                .HasForeignKey(p => p.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pet>()
                .HasOne(p => p.Buyer)
                .WithMany()
                .HasForeignKey(p => p.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure ChatMessage relationships
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Pet)
                .WithMany()
                .HasForeignKey(m => m.PetId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Negotiation relationships - Use Restrict to avoid cascade issues
            modelBuilder.Entity<Negotiation>()
                .HasOne(n => n.Pet)
                .WithMany()
                .HasForeignKey(n => n.PetId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Negotiation>()
                .HasOne(n => n.Buyer)
                .WithMany()
                .HasForeignKey(n => n.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Negotiation>()
                .HasOne(n => n.Seller)
                .WithMany()
                .HasForeignKey(n => n.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure indexes
            modelBuilder.Entity<Pet>()
                .HasIndex(p => p.Status);

            modelBuilder.Entity<Pet>()
                .HasIndex(p => p.Species);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<PetServiceProvider>()
                .HasIndex(sp => sp.ServiceType);
        }
    }
}