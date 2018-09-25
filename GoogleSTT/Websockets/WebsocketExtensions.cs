using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GoogleSTT.Websockets
{
  public static class WebsocketExtensions
  {
    public static IApplicationBuilder MapWebSocketManager(this IApplicationBuilder app, PathString path, WebSocketHandler handler)
    {
      return app.Map(path, (_app) => _app.UseMiddleware<WebSocketManagerMiddleware>(handler));
    }

    public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
    {
      services.AddTransient<WebSocketConnectionManager>();

      foreach(var type in Assembly.GetEntryAssembly().ExportedTypes)
      {
        if(type.GetTypeInfo().BaseType == typeof(WebSocketHandler))
        {
          services.AddSingleton(type);
        }
      }

      return services;
    }
  }
}
