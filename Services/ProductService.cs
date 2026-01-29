using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Products.Models;
using Products.Models.DTOs;
using Products.Repositories;
using Products.Mappers;

namespace Products.Services
{
  public class ProductService : IProductService
  {
    private readonly IProductRepository _repository;
    private readonly IProductMapper _mapper;
    public ProductService(IProductRepository repository, IProductMapper mapper)
    {
      _repository = repository;
      _mapper = mapper;
    }

    public async Task<ProductDto> GetByIdAsync(string id)
    {
      var product = await _repository.GetByIdAsync(id);
      if (product == null) return null;
      return _mapper.ToDto(product);
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
      var products = await _repository.GetAllAsync();
      return _mapper.ToDto(products);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
      var product = _mapper.ToEntity(dto);
      // Generate a unique, non-empty string ID for Cosmos DB
      product.Id = Guid.NewGuid().ToString();
      await _repository.AddAsync(product);
      return _mapper.ToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(string id, UpdateProductDto dto)
    {
      var product = await _repository.GetByIdAsync(id);
      if (product == null) return null;
      _mapper.UpdateEntity(product, dto);
      await _repository.UpdateAsync(product);
      return _mapper.ToDto(product);
    }

    public async Task DeleteAsync(string id)
    {
      await _repository.DeleteAsync(id);
    }
  }
}