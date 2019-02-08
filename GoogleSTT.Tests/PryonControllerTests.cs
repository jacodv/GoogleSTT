using System;
using System.Threading.Tasks;
using FluentAssertions;
using GoogleSTT.Controllers;
using GoogleSTT.Settings;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace GoogleSTT.Tests
{
  [TestFixture]
  public class PryonControllerTests
  {
    private PryonController _controller;
    private PryonSettings _settings;

    public void Setup()
    {
      _settings = new PryonSettings()
      {
        ClientId = "0oaiynlv6uvEahg4n0h7",
        Secret = "8ZQF76GQ4-Wxykg9U6vev0iZVKEJB2VLd9pHULAa",
        Host = "https://dev-355418.oktapreview.com",
        AuthEndPoint = "/oauth2/default/v1/token",
        GrantType = "client_credentials",
        Scope = "infoslips"
      };

      var mockPryonSettings = new Mock<IOptions<PryonSettings>>(MockBehavior.Strict);
      mockPryonSettings.SetupGet(x => x.Value).Returns(_settings);

      _controller = new PryonController(mockPryonSettings.Object);
    }

    [Test]
    public async Task GetAuthToken_GivenValidCredentials_ShouldSucceed()
    {
      //setup
      Setup();

      //action
      var token = await _controller.GetAuthToken();

      //assert
      token.Should().NotBeNull();
      token.AccessToken.Should().NotBeEmpty();
      Console.WriteLine(JsonConvert.SerializeObject(token, Formatting.Indented));
      //Debug.Print(JsonConvert.SerializeObject(token));
    }
  }
}