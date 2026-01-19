using Microsoft.AspNetCore.Mvc;
using Api.Services;
using Api.Models;

namespace Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
  private readonly ICosmosDbService _cosmosDbService;

  public ProductsController(ICosmosDbService cosmosDbService)
  {
    _cosmosDbService = cosmosDbService;
  }

  [HttpGet]
  public async Task<IActionResult> GetAll()
  {
    var products = await _cosmosDbService.GetAllProductsAsync();
    return Ok(products);
  }

  [HttpGet("{id}")]
  public async Task<IActionResult> GetById(string id)
  {
    var product = await _cosmosDbService.GetProductByIdAsync(id);
    if (product == null)
    {
      return NotFound();
    }
    return Ok(product);
  }

  [HttpPost]
  public async Task<IActionResult> Create([FromBody] Product product)
  {
    product.Id = Guid.NewGuid().ToString();
    var createdProduct = await _cosmosDbService.CreateProductAsync(product);
    return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id }, createdProduct);
  }

  [HttpPut("{id}")]
  public async Task<IActionResult> Update(string id, [FromBody] Product product)
  {
    product.Id = id;
    var updatedProduct = await _cosmosDbService.UpdateProductAsync(id, product);
    return Ok(updatedProduct);
  }

  [HttpDelete("{id}")]
  public async Task<IActionResult> Delete(string id)
  {
    await _cosmosDbService.DeleteProductAsync(id);
    return NoContent();
  }
}