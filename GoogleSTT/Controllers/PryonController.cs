using System;
using System.Threading.Tasks;
using GoogleSTT.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using GoogleSTT.Settings;
using RestSharp;


namespace GoogleSTT.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class PryonController : ControllerBase
  {
    private readonly PryonSettings _settings;
    private static PryonToken _token;

    public PryonController(IOptions<PryonSettings> settings)
    {
      _settings = settings.Value;
    }

    [HttpGet]
    public async Task<PryonToken> GetAuthToken()
    {
      var token = await _getPryonAuthtoken(_settings);
      token.WebSocketUrl = $"{_settings.Host}{_settings.WebSocketEndPoint}";
      return token;
    }

    private async Task<PryonToken> _getPryonAuthtoken(PryonSettings settings)
    {
      var client = new RestClient(settings.Host);
      var req = new RestRequest(settings.AuthEndPoint,Method.POST);

      // Content type is not required when adding parameters this way
      // This will also automatically UrlEncode the values
      req.AddParameter("client_id",settings.ClientId, ParameterType.GetOrPost);
      req.AddParameter("grant_type",settings.GrantType);
      req.AddParameter("client_secret",settings.Secret, ParameterType.GetOrPost);
      req.AddParameter("scope",settings.Scope, ParameterType.GetOrPost);

      var response = await client.ExecuteTaskAsync<PryonToken>(req);
      
      if(!response.IsSuccessful)
        throw new InvalidOperationException("Failed to logon to Pryon: " + response.ErrorException);

      return response.Data;
    }
  }
}