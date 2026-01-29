using System.ComponentModel.DataAnnotations;

namespace Products.Models.DTOs;

/// <summary>
/// DTO for updating an existing product
/// </summary>
public class UpdateProductDto
{

  [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
  public string? Name { get; set; }

  [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
  public decimal? Price { get; set; }

  [Url(ErrorMessage = "Image URL must be a valid URL")]
  public string? ImageUrl { get; set; }
}

