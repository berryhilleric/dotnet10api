using Newtonsoft.Json;

namespace Api.Models
{

  public class Product
  {
    [JsonProperty("id")]
    public required string Id { get; set; }

    [JsonProperty("userId")]
    public required int UserId { get; set; }

    [JsonProperty("name")]
    public required string Name { get; set; }

    [JsonProperty("price")]
    public decimal Price { get; set; }
  }
}