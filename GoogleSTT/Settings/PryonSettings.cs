using System;

namespace GoogleSTT.Settings
{
  public class PryonSettings
  {
    public string ClientId { get; set; }
    public string Secret { get; set; }
    public string GrantType { get; set; }
    public string Scope { get; set; }
    public string Host { get; set; }
    public string AuthEndPoint { get; set; }
    public string WebSocketEndPoint { get; set; }
  }
}
