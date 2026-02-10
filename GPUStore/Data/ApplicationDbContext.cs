using GPUStore.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GPUStore.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<VideoCard> VideoCards { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<Technology> Technologies { get; set; }
        public DbSet<CardTechnology> CardTechnologies { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Много-към-Много: Cards <-> Technologies
            builder.Entity<CardTechnology>()
                .HasKey(ct => new { ct.VideoCardId, ct.TechnologyId });

            // 2. Много-към-Много: Orders <-> Cards (OrderItem)
            // Тук използваме Id като PK, но дефинираме връзките
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.VideoCard)
                .WithMany(v => v.OrderItems)
                .HasForeignKey(oi => oi.VideoCardId);
        }
    }
}
