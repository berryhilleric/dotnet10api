using Microsoft.AspNetCore.Mvc;
using Products.Services;
using Products.Models;
using Products.Models.DTOs;
using Products.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Products.Mappers;

namespace Products.Controllers;

[ApiController]
[Route("api/products")]
[Authorize] // Requires valid JWT token
public class ProductsController : ControllerBase
{
  private readonly ApiDbContext _context;
  private readonly IBlobStorageService _blobStorageService;
  private readonly IProductMapper _mapper;

  public ProductsController(
    ApiDbContext context,
    IBlobStorageService blobStorageService,
    IProductMapper mapper)
  {
    _context = context;
    _blobStorageService = blobStorageService;
    _mapper = mapper;
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
  public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
  {
    var products = await _context.Products.ToListAsync();
    var productDtos = _mapper.ToDto(products);
    return Ok(productDtos);
  }

  [HttpGet("{userId}")]
  public async Task<ActionResult<IEnumerable<ProductDto>>> GetByUserId(int userId)
  {
    var products = await _context.Products
      .Where(p => p.UserId == userId)
      .ToListAsync();
    var productDtos = _mapper.ToDto(products);
    return Ok(productDtos);
  }

  [HttpGet("{userId}/{id}")]
  public async Task<ActionResult<ProductDto>> GetById(int userId, string id)
  {
    var product = await _context.Products
      .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

    if (product == null)
    {
      return NotFound();
    }

    var productDto = _mapper.ToDto(product);
    return Ok(productDto);
  }

  [HttpPost]
  public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto createDto)
  {
    if (!ModelState.IsValid)
    {
      return BadRequest(ModelState);
    }

    var product = _mapper.ToEntity(createDto);
    product.Id = Guid.NewGuid().ToString();

    _context.Products.Add(product);
    await _context.SaveChangesAsync();

    var productDto = _mapper.ToDto(product);
    return CreatedAtAction(nameof(GetById), new { userId = product.UserId, id = product.Id }, productDto);
  }

  [HttpPut("{userId}/{id}")]
  public async Task<ActionResult<ProductDto>> Update(int userId, string id, [FromBody] UpdateProductDto updateDto)
  {
    if (!ModelState.IsValid)
    {
      return BadRequest(ModelState);
    }

    var existingProduct = await _context.Products
      .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

    if (existingProduct == null)
    {
      return NotFound();
    }

    // Use mapper to update the entity
    _mapper.UpdateEntity(existingProduct, updateDto);

    await _context.SaveChangesAsync();

    var productDto = _mapper.ToDto(existingProduct);
    return Ok(productDto);
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
