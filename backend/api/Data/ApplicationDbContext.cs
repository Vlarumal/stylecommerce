using Microsoft.EntityFrameworkCore;
using StyleCommerce.Api.Models;

namespace StyleCommerce.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<PaymentToken> PaymentTokens { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Price).HasPrecision(18, 2);
                entity.Property(e => e.CategoryId);
                entity.Property(e => e.Brand).HasMaxLength(50);
                entity.Property(e => e.Size).HasMaxLength(20);
                entity.Property(e => e.Color).HasMaxLength(30);
                entity.Property(e => e.StockQuantity);
                entity.Property(e => e.ImageUrl).HasMaxLength(200);
                entity.Property(e => e.Model3DUrl).HasMaxLength(200);
                entity.Property(e => e.CreatedAt);
                entity.Property(e => e.UpdatedAt);
                entity.Property(e => e.IsVerified);
                entity.Property(e => e.VerificationScore);
                entity.Property(e => e.EcoScore);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.ActionType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UserId).HasMaxLength(50);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(255);
                entity.Property(e => e.AdditionalData).HasMaxLength(1000);
                entity.Property(e => e.SessionId).HasMaxLength(100);
            });

            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId);
                entity.Property(e => e.SessionId).HasMaxLength(100);
                entity.Property(e => e.CreatedDate);

                entity
                    .HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CartId).IsRequired();
                entity.Property(e => e.ProductId).IsRequired();
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.PriceSnapshot).HasPrecision(18, 2);
                entity.Property(e => e.AddedDate);

                entity
                    .HasOne(ci => ci.Cart)
                    .WithMany(c => c.CartItems)
                    .HasForeignKey(ci => ci.CartId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity
                    .HasOne(ci => ci.Product)
                    .WithMany()
                    .HasForeignKey(ci => ci.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.OrderId);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.OrderDate);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);

                entity
                    .HasOne(o => o.User)
                    .WithMany(u => u.Orders)
                    .HasForeignKey(o => o.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.OrderItemId);
                entity.Property(e => e.OrderId).IsRequired();
                entity.Property(e => e.ProductId).IsRequired();
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.Price).HasPrecision(18, 2);

                entity
                    .HasOne(oi => oi.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity
                    .HasOne(oi => oi.Product)
                    .WithMany()
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed data
            modelBuilder
                .Entity<Product>()
                .HasData(
                    new Product
                    {
                        Id = 1,
                        Name = "Organic Cotton T-Shirt",
                        Description = "Eco-friendly t-shirt made from 100% organic cotton",
                        Price = 29.99m,
                        CategoryId = 1,
                        Brand = "EcoWear",
                        Size = "M",
                        Color = "White",
                        StockQuantity = 100,
                        ImageUrl = "/images/organic-tshirt.png",
                        Model3DUrl = "https://example.com/models/tshirt.glb",
                        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        IsVerified = true,
                        VerificationScore = 95,
                        EcoScore = 95,
                    },
                    new Product
                    {
                        Id = 2,
                        Name = "Recycled Plastic Water Bottle",
                        Description = "Durable water bottle made from recycled plastic materials",
                        Price = 19.99m,
                        CategoryId = 3,
                        Brand = "EcoHydrate",
                        Size = "500ml",
                        Color = "Blue",
                        StockQuantity = 50,
                        ImageUrl = "/images/recycled-bottle.png",
                        Model3DUrl = "https://example.com/models/bottle.glb",
                        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        IsVerified = true,
                        VerificationScore = 85,
                        EcoScore = 85,
                    },
                    new Product
                    {
                        Id = 3,
                        Name = "Bamboo Cutting Board",
                        Description = "Sustainable cutting board made from bamboo fibers",
                        Price = 39.99m,
                        CategoryId = 3,
                        Brand = "EcoKitchen",
                        Size = "Large",
                        Color = "Natural",
                        StockQuantity = 25,
                        ImageUrl = "/images/bamboo-board.png",
                        Model3DUrl = "https://example.com/models/cutting-board.glb",
                        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        IsVerified = true,
                        VerificationScore = 90,
                        EcoScore = 90,
                    },
                    new Product
                    {
                        Id = 4,
                        Name = "Hemp Fabric Tote Bag",
                        Description = "Sustainable tote bag made from hemp fibers",
                        Price = 24.99m,
                        CategoryId = 3,
                        Brand = "EcoUnknown",
                        Size = "M",
                        Color = "Natural",
                        StockQuantity = 30,
                        ImageUrl = "/images/hemp-tote-bag.png",
                        Model3DUrl = "",
                        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        IsVerified = false,
                        VerificationScore = 0,
                        EcoScore = 0,
                    }
                );

            base.OnModelCreating(modelBuilder);
        }
    }
}
