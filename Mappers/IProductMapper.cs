using Products.Models;
using Products.Models.DTOs;

namespace Products.Mappers;

/// <summary>
/// Interface for mapping between Product entities and DTOs
/// </summary>
public interface IProductMapper
{
  /// <summary>
  /// Maps a Product entity to a ProductDto
  /// </summary>
  ProductDto ToDto(Product product);

  /// <summary>
  /// Maps a collection of Product entities to ProductDto collection
  /// </summary>
  IEnumerable<ProductDto> ToDto(IEnumerable<Product> products);

  /// <summary>
  /// Maps a CreateProductDto to a Product entity
  /// </summary>
  Product ToEntity(CreateProductDto createDto);

  /// <summary>
  /// Updates a Product entity with data from UpdateProductDto
  /// </summary>
  void UpdateEntity(Product product, UpdateProductDto updateDto);
}
