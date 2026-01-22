using System.Security.Claims;

public interface ICurrentUserService
{
  string? UserId { get; }
  string? Email { get; }
  string? DisplayName { get; }
  string? TenantId { get; }
}

public class CurrentUserService : ICurrentUserService
{
  private readonly IHttpContextAccessor _httpContextAccessor;

  public CurrentUserService(IHttpContextAccessor httpContextAccessor)
  {
    _httpContextAccessor = httpContextAccessor;
  }

  public string? UserId =>
      _httpContextAccessor.HttpContext?.User.FindFirst("oid")?.Value
      ?? _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

  public string? Email =>
      _httpContextAccessor.HttpContext?.User.FindFirst("emails")?.Value
      ?? _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;

  public string? DisplayName =>
      _httpContextAccessor.HttpContext?.User.FindFirst("name")?.Value
      ?? _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;

  public string? TenantId =>
      _httpContextAccessor.HttpContext?.User.FindFirst("tid")?.Value;
}