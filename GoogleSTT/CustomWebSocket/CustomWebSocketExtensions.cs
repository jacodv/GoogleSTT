using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace GoogleSTT.CustomWebSocket
{
  public static class WebSocketExtensions
  {
    public static IApplicationBuilder UseCustomWebSocketManager(this IApplicationBuilder app)
    {
      return app.UseMiddleware<CustomWebSocketManager>();
    }
  }

  public class CustomWebSocketManager
  {
    private readonly RequestDelegate _next;

    public CustomWebSocketManager(RequestDelegate next)
    {
      _next = next;
    }

    public async Task Invoke(HttpContext context, ICustomWebSocketFactory wsFactory, ICustomWebSocketMessageHandler wsmHandler)
    {
      if (context.Request.Path == "/ws")
      {
        if (context.WebSockets.IsWebSocketRequest)
        {
          string username = context.Request.Query["u"];
          if (!string.IsNullOrEmpty(username))
          {
            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
            CustomWebSocket userWebSocket = new CustomWebSocket()
            {
              WebSocket = webSocket,
              Username = username
            };
            wsFactory.Add(userWebSocket);
            await wsmHandler.SendInitialMessages(userWebSocket);
            await Listen(context, userWebSocket, wsFactory, wsmHandler);
          }
        }
        else
        {
          context.Response.StatusCode = 400;
        }
      }
      await _next(context);
    }

    private async Task Listen(HttpContext context, CustomWebSocket userWebSocket, ICustomWebSocketFactory wsFactory, ICustomWebSocketMessageHandler wsmHandler)
    {
      WebSocket webSocket = userWebSocket.WebSocket;
      var buffer = new byte[1024 * 4];
      WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
      while (!result.CloseStatus.HasValue)
      {
        await wsmHandler.HandleMessage(result, buffer, userWebSocket, wsFactory);
        buffer = new byte[1024 * 4];
        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
      }
      wsFactory.Remove(userWebSocket.Username);
      await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }
  }
  public class CustomWebSocket
  {
    public WebSocket WebSocket { get; set; }
    public string Username { get; set; }
  }

  class CustomWebSocketMessage
  {
    public string Text { get; set; }
    public DateTime MessagDateTime { get; set; }
    public string Username { get; set; }
  }

  public interface ICustomWebSocketFactory
  {
    void Add(CustomWebSocket uws);
    void Remove(string username);
    List<CustomWebSocket> All();
    List<CustomWebSocket> Others(CustomWebSocket client);
    CustomWebSocket Client(string username);
  }

  public class CustomWebSocketFactory : ICustomWebSocketFactory
  {
    List<CustomWebSocket> List;

    public CustomWebSocketFactory()
    {
      List = new List<CustomWebSocket>();
    }

    public void Add(CustomWebSocket uws)
    {
      List.Add(uws);
    }

    //when disconnect
    public void Remove(string username) 
    {
      List.Remove(Client(username));
    }

    public List<CustomWebSocket> All()
    {
      return List;
    }
   
    public List<CustomWebSocket> Others(CustomWebSocket client)
    {
      return List.Where(c => c.Username != client.Username).ToList();
    }
 
    public CustomWebSocket Client(string username)
    {
      return List.First(c=>c.Username == username);
    }
  }

  public interface ICustomWebSocketMessageHandler
{
   Task SendInitialMessages(CustomWebSocket userWebSocket);
   Task HandleMessage(WebSocketReceiveResult result, byte[] buffer, CustomWebSocket userWebSocket, ICustomWebSocketFactory wsFactory);
   Task BroadcastOthers(byte[] buffer, CustomWebSocket userWebSocket, ICustomWebSocketFactory wsFactory);
   Task BroadcastAll(byte[] buffer, CustomWebSocket userWebSocket, ICustomWebSocketFactory wsFactory);
}

public class CustomWebSocketMessageHandler : ICustomWebSocketMessageHandler
{
   public async Task SendInitialMessages(CustomWebSocket userWebSocket)
   {
      WebSocket webSocket = userWebSocket.WebSocket;
      var msg = new CustomWebSocketMessage
      {
         MessagDateTime = DateTime.Now,
         Text = "Blah blah",
         Username = "system"
      };

      string serialisedMessage = JsonConvert.SerializeObject(msg);
      byte[] bytes = Encoding.ASCII.GetBytes(serialisedMessage);
      await webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
   }

   public async Task HandleMessage(WebSocketReceiveResult result, byte[] buffer, CustomWebSocket userWebSocket, ICustomWebSocketFactory wsFactory)
   {
      string msg = Encoding.ASCII.GetString(buffer);
      try
      {
         var message = JsonConvert.DeserializeObject<CustomWebSocketMessage>(msg);
         //if (message.Type == WSMessageType.anyType)
         //{
         //   await BroadcastOthers(buffer, userWebSocket, wsFactory);
         //}
        Console.WriteLine(message);
      }
      catch (Exception e)
      {
         await userWebSocket.WebSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
      }
   }

   public async Task BroadcastOthers(byte[] buffer, CustomWebSocket userWebSocket, ICustomWebSocketFactory wsFactory)
   {
      var others = wsFactory.Others(userWebSocket);
      foreach (var uws in others)
      {
         await uws.WebSocket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
      }
   }

   public async Task BroadcastAll(byte[] buffer, CustomWebSocket userWebSocket, ICustomWebSocketFactory wsFactory)
   {
      var all = wsFactory.All();
      foreach (var uws in all)
      {
         await uws.WebSocket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
      }
   }
}
}

