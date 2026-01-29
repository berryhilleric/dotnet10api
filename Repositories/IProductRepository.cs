using System.Collections.Generic;
using System.Threading.Tasks;
using Products.Models;

namespace Products.Repositories
{
  public interface IProductRepository
  {
    Task<Product> GetByIdAsync(string id);
    Task<IEnumerable<Product>> GetAllAsync();
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(string id);
  }
}