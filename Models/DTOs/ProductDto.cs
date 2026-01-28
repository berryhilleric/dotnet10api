namespace Products.Models.DTOs;

/// <summary>
/// DTO for returning product data to clients
/// </summary>
public class ProductDto
{
  public string Id { get; set; } = string.Empty;
  public int UserId { get; set; }
  public string Name { get; set; } = string.Empty;
  public decimal Price { get; set; }
  public string? ImageUrl { get; set; }
}
