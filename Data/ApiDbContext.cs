using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class ApiDbContext : DbContext
{
  public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
  {
  }

  public DbSet<Product> Products { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // Configure for Cosmos DB
    modelBuilder.HasDefaultContainer("products");

    // Configure Product entity
    modelBuilder.Entity<Product>(entity =>
    {
      // Cosmos DB uses partition keys, UserId is a good partition key
      entity.HasPartitionKey(e => e.UserId);

      // Id is the key
      entity.HasKey(e => e.Id);

      // Configure properties (Cosmos DB handles types differently than SQL)
      entity.Property(e => e.Id).IsRequired();
      entity.Property(e => e.UserId).IsRequired();
      entity.Property(e => e.Name).IsRequired();
      entity.Property(e => e.Price).IsRequired();

      // ToJsonProperty maps C# property names to JSON property names in Cosmos
      entity.Property(e => e.Id).ToJsonProperty("id");
      entity.Property(e => e.UserId).ToJsonProperty("userId");
      entity.Property(e => e.Name).ToJsonProperty("name");
      entity.Property(e => e.Price).ToJsonProperty("price");
      entity.Property(e => e.ImageUrl).ToJsonProperty("imageUrl");
    });
  }
}
