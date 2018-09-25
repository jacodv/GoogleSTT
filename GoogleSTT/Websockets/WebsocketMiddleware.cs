
using System;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.AspNetCore.Http;

namespace GoogleSTT.Websockets
{
  public class WebSocketManagerMiddleware
  {
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private readonly RequestDelegate _next;
    private WebSocketHandler _webSocketHandler { get; set; }

    public WebSocketManagerMiddleware(RequestDelegate next, 
      WebSocketHandler webSocketHandler)
    {
      _next = next;
      _webSocketHandler = webSocketHandler;
    }

    public async Task Invoke(HttpContext context)
    {
      if(!context.WebSockets.IsWebSocketRequest)
        return;
            
      var socket = await context.WebSockets.AcceptWebSocketAsync();
      await _webSocketHandler.OnConnected(socket);
            
      await Receive(socket, async(result, buffer) =>
      {
        _log.Debug($"{socket.State}-{result.MessageType}--{buffer.Length}");
        try
        {
          switch (result.MessageType)
          {
            case WebSocketMessageType.Binary:
              await _webSocketHandler.ReceiveAsyncData(socket, result, buffer);
              break;
            case WebSocketMessageType.Close:
              await _webSocketHandler.OnDisconnected(socket);
              break;
            case WebSocketMessageType.Text:
              await _webSocketHandler.ReceiveAsyncText(socket, result, buffer);
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }
        }
        catch (AggregateException aggEx)
        {
          var innerEx = aggEx.Flatten().InnerException;
          _log.Error($"Failed to receive message: {innerEx?.Message}", innerEx?? aggEx);
        }
        catch (Exception e)
        {
          _log.Error($"Failed to receive message: {e.Message}", e);
        }
      });
            
      //TODO - investigate the Kestrel exception thrown when this is the last middleware
      //await _next.Invoke(context);
    }

    private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
    {
      _log.Debug($"Start receive");
      var buffer = new byte[1024 * 4];

      while(socket.State == WebSocketState.Open)
      {
        var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), cancellationToken: CancellationToken.None);

        handleMessage(result, buffer);                
      }
    }
  }}
