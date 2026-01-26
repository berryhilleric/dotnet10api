using Microsoft.AspNetCore.Mvc;
using Api.Services;
using Api.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize] // Requires valid JWT token
public class ProductsController : ControllerBase
{
  private readonly ICosmosDbService _cosmosDbService;
  private readonly IBlobStorageService _blobStorageService;

  public ProductsController(ICosmosDbService cosmosDbService, IBlobStorageService blobStorageService)
  {
    _cosmosDbService = cosmosDbService;
    _blobStorageService = blobStorageService;
  }

  [HttpGet("me")]
  public IActionResult GetCurrentUser()
  {
    // Extract user information from JWT claims
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("oid")?.Value; // Object ID
    var email = User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("emails")?.Value;
    var displayName = User.FindFirst(ClaimTypes.Name)?.Value
                      ?? User.FindFirst("name")?.Value;

    return Ok(new
    {
      UserId = userId,
      Email = email,
      DisplayName = displayName,
      IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
      Claims = User.Claims.Select(c => new { c.Type, c.Value })
    });
  }

  [HttpGet]
  public async Task<IActionResult> GetAll()
  {
    var products = await _cosmosDbService.GetAllProductsAsync();
    return Ok(products);
  }

  [HttpGet("{userId}/{id}")]
  public async Task<IActionResult> GetById(int userId, string id)
  {
    var product = await _cosmosDbService.GetProductByIdAsync(userId, id);
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

  [HttpPut("{userId}/{id}")]
  public async Task<IActionResult> Update(int userId, string id, [FromBody] Product product)
  {
    product.Id = id;
    product.UserId = userId;
    var updatedProduct = await _cosmosDbService.UpdateProductAsync(id, product);
    return Ok(updatedProduct);
  }

  [HttpDelete("{userId}/{id}")]
  public async Task<IActionResult> Delete(int userId, string id)
  {
    await _cosmosDbService.DeleteProductAsync(userId, id);
    return NoContent();
  }

  [HttpPost("{userId}/{id}/image")]
  public async Task<IActionResult> UploadImage(int userId, string id, IFormFile image)
  {
    if (image == null || image.Length == 0)
    {
      return BadRequest("No image file provided");
    }

    // Validate image file type
    var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
    if (!allowedTypes.Contains(image.ContentType.ToLower()))
    {
      return BadRequest("Invalid image type. Only JPEG, PNG, GIF, and WebP are allowed.");
    }

    // Validate file size (max 5MB)
    if (image.Length > 5 * 1024 * 1024)
    {
      return BadRequest("Image size cannot exceed 5MB");
    }

    // Get the product
    var product = await _cosmosDbService.GetProductByIdAsync(userId, id);
    if (product == null)
    {
      return NotFound("Product not found");
    }

    // Delete old image if exists
    if (!string.IsNullOrEmpty(product.ImageUrl))
    {
      await _blobStorageService.DeleteImageAsync(product.ImageUrl);
    }

    // Upload new image
    using var stream = image.OpenReadStream();
    var imageUrl = await _blobStorageService.UploadImageAsync(stream, image.FileName, image.ContentType);

    // Update product with new image URL
    product.ImageUrl = imageUrl;
    await _cosmosDbService.UpdateProductAsync(id, product);

    return Ok(new { imageUrl });
  }

  [HttpDelete("{userId}/{id}/image")]
  public async Task<IActionResult> DeleteImage(int userId, string id)
  {
    var product = await _cosmosDbService.GetProductByIdAsync(userId, id);
    if (product == null)
    {
      return NotFound("Product not found");
    }

    if (!string.IsNullOrEmpty(product.ImageUrl))
    {
      await _blobStorageService.DeleteImageAsync(product.ImageUrl);
      product.ImageUrl = null;
      await _cosmosDbService.UpdateProductAsync(id, product);
    }

    return NoContent();
  }
}