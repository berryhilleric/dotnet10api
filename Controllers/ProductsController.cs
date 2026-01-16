using Microsoft.AspNetCore.Mvc;

namespace MyFirstApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
  [HttpGet]
  public IActionResult GetAll()
  {
    var products = new[]
    {
            new { Id = 1, Name = "Laptop", Price = 999.99 },
            new { Id = 2, Name = "Mouse", Price = 29.99 }
        };
    return Ok(products);
  }

  [HttpGet("{id}")]
  public IActionResult GetById(int id)
  {
    var product = new { Id = id, Name = "Sample Product", Price = 49.99 };
    return Ok(product);
  }

  [HttpPost]
  public IActionResult Create([FromBody] Product product)
  {
    // Add logic to save the product
    return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
  }
}

public record Product(int Id, string Name, decimal Price);