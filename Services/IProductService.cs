using System.Collections.Generic;
using System.Threading.Tasks;
using Products.Models;
using Products.Models.DTOs;

namespace Products.Services
{
  public interface IProductService
  {
    Task<ProductDto> GetByIdAsync(string id);
    Task<IEnumerable<ProductDto>> GetAllAsync();
    Task<ProductDto> CreateAsync(CreateProductDto dto);
    Task<ProductDto> UpdateAsync(string id, UpdateProductDto dto);
    Task DeleteAsync(string id);
  }
}