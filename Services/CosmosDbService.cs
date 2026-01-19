using Microsoft.Azure.Cosmos;
using Api.Models;

namespace Api.Services
{
  public interface ICosmosDbService
  {
    Task<List<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(string id);
    Task<Product> CreateProductAsync(Product product);
    Task<Product> UpdateProductAsync(string id, Product product);
    Task DeleteProductAsync(string id);
  }

  public class CosmosDbService : ICosmosDbService
  {
    private readonly Container _container;

    public CosmosDbService(CosmosClient cosmosClient, string databaseName, string containerName)
    {
      _container = cosmosClient.GetContainer(databaseName, containerName);
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
      var query = _container.GetItemQueryIterator<Product>();
      var results = new List<Product>();

      while (query.HasMoreResults)
      {
        var response = await query.ReadNextAsync();
        results.AddRange(response);
      }

      return results;
    }

    public async Task<Product?> GetProductByIdAsync(string id)
    {
      try
      {
        var response = await _container.ReadItemAsync<Product>(id, new PartitionKey(null));
        return response.Resource;
      }
      catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
      {
        return null;
      }
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
      var response = await _container.CreateItemAsync(product, new PartitionKey(product.UserId));
      return response.Resource;
    }

    public async Task<Product> UpdateProductAsync(string id, Product product)
    {
      var response = await _container.ReplaceItemAsync(product, id, new PartitionKey(null));
      return response.Resource;
    }

    public async Task DeleteProductAsync(string id)
    {
      await _container.DeleteItemAsync<Product>(id, new PartitionKey(null));
    }
  }
}