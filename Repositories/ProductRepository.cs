using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Products.Data;
using Products.Models;

namespace Products.Repositories
{
  public class ProductRepository : IProductRepository
  {
    private readonly ApiDbContext _context;
    public ProductRepository(ApiDbContext context)
    {
      _context = context;
    }

    public async Task<Product> GetByIdAsync(string id)
    {
      return await _context.Products.FindAsync(id);
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
      return await _context.Products.ToListAsync();
    }

    public async Task AddAsync(Product product)
    {
      await _context.Products.AddAsync(product);
      await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
      _context.Products.Update(product);
      await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
      var product = await _context.Products.FindAsync(id);
      if (product != null)
      {
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
      }
    }
  }
}