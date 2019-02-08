using Newtonsoft.Json;

namespace GoogleSTT.Models
{
  public class PryonToken
  {
    [JsonProperty("access_token")] 
    public string AccessToken { get; set; }
    [JsonProperty("token_type")] 
    public string TokenType { get; set; }
    [JsonProperty("expires_in")] 
    public int ExpiresIn { get; set; }

    public string WebSocketUrl { get; set; }
  }
}
