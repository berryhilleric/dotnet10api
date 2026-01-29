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
  private readonly IProductService _productService;
  private readonly IBlobStorageService _blobStorageService;

  public ProductsController(
    IProductService productService,
    IBlobStorageService blobStorageService)
  {
    _productService = productService;
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
  public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
  {
    var productDtos = await _productService.GetAllAsync();
    return Ok(productDtos);
  }

  [HttpGet("{userId}")]
  public async Task<ActionResult<IEnumerable<ProductDto>>> GetByUserId(int userId)
  {
    // Optionally, implement a GetByUserIdAsync in the service if needed
    var allProducts = await _productService.GetAllAsync();
    var userProducts = allProducts.Where(p => p.UserId == userId);
    return Ok(userProducts);
  }

  [HttpGet("{userId}/{id}")]
  public async Task<ActionResult<ProductDto>> GetById(int userId, string id)
  {
    var productDto = await _productService.GetByIdAsync(id);
    if (productDto == null || productDto.UserId != userId)
    {
      return NotFound();
    }
    return Ok(productDto);
  }

  [HttpPost]
  public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto createDto)
  {
    if (!ModelState.IsValid)
    {
      return BadRequest(ModelState);
    }

    var productDto = await _productService.CreateAsync(createDto);
    return CreatedAtAction(nameof(GetById), new { userId = productDto.UserId, id = productDto.Id }, productDto);
  }

  [HttpPut("{userId}/{id}")]
  public async Task<ActionResult<ProductDto>> Update(int userId, string id, [FromBody] UpdateProductDto updateDto)
  {
    if (!ModelState.IsValid)
    {
      return BadRequest(ModelState);
    }

    var productDto = await _productService.UpdateAsync(id, updateDto);
    if (productDto == null || productDto.UserId != userId)
    {
      return NotFound();
    }
    return Ok(productDto);
  }

  [HttpDelete("{userId}/{id}")]
  public async Task<IActionResult> Delete(int userId, string id)
  {
    var productDto = await _productService.GetByIdAsync(id);
    if (productDto == null || productDto.UserId != userId)
    {
      return NotFound();
    }
    await _productService.DeleteAsync(id);
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

    // Get the product via service
    var productDto = await _productService.GetByIdAsync(id);
    if (productDto == null || productDto.UserId != userId)
    {
      return NotFound("Product not found");
    }

    // Delete old image if exists
    if (!string.IsNullOrEmpty(productDto.ImageUrl))
    {
      await _blobStorageService.DeleteImageAsync(productDto.ImageUrl);
    }

    // Upload new image
    using var stream = image.OpenReadStream();
    var imageUrl = await _blobStorageService.UploadImageAsync(stream, image.FileName, image.ContentType);

    // Update product with new image URL using service
    var updateDto = new UpdateProductDto
    {
      Name = productDto.Name,
      Price = productDto.Price,
      ImageUrl = imageUrl
    };
    await _productService.UpdateAsync(id, updateDto);

    return Ok(new { imageUrl });
  }

  [HttpDelete("{userId}/{id}/image")]
  public async Task<IActionResult> DeleteImage(int userId, string id)
  {
    var productDto = await _productService.GetByIdAsync(id);
    if (productDto == null || productDto.UserId != userId)
    {
      return NotFound("Product not found");
    }

    if (!string.IsNullOrEmpty(productDto.ImageUrl))
    {
      await _blobStorageService.DeleteImageAsync(productDto.ImageUrl);
      // Remove image URL from product
      var updateDto = new UpdateProductDto
      {
        Name = productDto.Name,
        Price = productDto.Price,
        ImageUrl = null
      };
      await _productService.UpdateAsync(id, updateDto);
    }

    return NoContent();
  }
}
