using Microsoft.AspNetCore.Mvc;
using Api.Services;
using Api.Models;
using Api.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize] // Requires valid JWT token
public class ProductsController : ControllerBase
{
  private readonly ApiDbContext _context;
  private readonly IBlobStorageService _blobStorageService;

  public ProductsController(ApiDbContext context, IBlobStorageService blobStorageService)
  {
    _context = context;
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
    var products = await _context.Products.ToListAsync();
    return Ok(products);
  }

  [HttpGet("{userId}")]
  public async Task<IActionResult> GetByUserId(int userId)
  {
    var products = await _context.Products
      .Where(p => p.UserId == userId)
      .ToListAsync();
    return Ok(products);
  }

  [HttpGet("{userId}/{id}")]
  public async Task<IActionResult> GetById(int userId, string id)
  {
    var product = await _context.Products
      .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

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

    _context.Products.Add(product);
    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(GetById), new { userId = product.UserId, id = product.Id }, product);
  }

  [HttpPut("{userId}/{id}")]
  public async Task<IActionResult> Update(int userId, string id, [FromBody] Product product)
  {
    var existingProduct = await _context.Products
      .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

    if (existingProduct == null)
    {
      return NotFound();
    }

    // Update properties
    existingProduct.Name = product.Name;
    existingProduct.Price = product.Price;
    existingProduct.ImageUrl = product.ImageUrl;

    await _context.SaveChangesAsync();

    return Ok(existingProduct);
  }

  [HttpDelete("{userId}/{id}")]
  public async Task<IActionResult> Delete(int userId, string id)
  {
    var product = await _context.Products
      .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

    if (product == null)
    {
      return NotFound();
    }

    _context.Products.Remove(product);
    await _context.SaveChangesAsync();

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
    var product = await _context.Products
      .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

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
    await _context.SaveChangesAsync();

    return Ok(new { imageUrl });
  }

  [HttpDelete("{userId}/{id}/image")]
  public async Task<IActionResult> DeleteImage(int userId, string id)
  {
    var product = await _context.Products
      .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

    if (product == null)
    {
      return NotFound("Product not found");
    }

    if (!string.IsNullOrEmpty(product.ImageUrl))
    {
      await _blobStorageService.DeleteImageAsync(product.ImageUrl);
      product.ImageUrl = null;
      await _context.SaveChangesAsync();
    }

    return NoContent();
  }
}
