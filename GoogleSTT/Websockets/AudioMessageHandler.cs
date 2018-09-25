using System;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace GoogleSTT.Websockets
{
  public class AudioMessageHandler: WebSocketHandler
  {
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    public AudioMessageHandler(WebSocketConnectionManager webSocketConnectionManager) : base(webSocketConnectionManager)
    {
    }

    public override async Task OnConnected(WebSocket socket)
    {
      await base.OnConnected(socket);

      var socketId = WebSocketConnectionManager.GetId(socket);
      _log.Debug($"SocketId:{socketId}-Connected");
      await SendMessageToAllAsync($"{socketId} is now connected");
    }

    public override Task ReceiveAsyncText(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
    {
      try
      {
        var socketId = WebSocketConnectionManager.GetId(socket);
        var message = $"{socketId} said: {Encoding.UTF8.GetString(buffer, 0, result.Count)}";

        _log.Debug($"Socket ReceiveAsyncText:{message}");
      }
      catch (Exception e)
      {
        _log.Error($"Socket ReceiveAsyncText:{e.Message}",e);
      }
      return Task.FromResult(0);
    }

    public override Task ReceiveAsyncData(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
    {
      try
      {
        var socketId = WebSocketConnectionManager.GetId(socket);

        _log.Debug($"Socket ReceiveAsyncData:{result.Count}-{buffer.Length}");
        _log.Debug($"Socket ReceiveAsyncData:{Convert.ToBase64String(buffer,0,result.Count)}");

//        _log.Debug($"Socket ReceiveAsyncData:{result.Count}-{buffer.Length}");
      }
      catch (Exception e)
      {
        _log.Error($"Socket ReceiveAsyncData:{e.Message}",e);
      }
      return Task.FromResult(0);
    }

    public override async Task OnDisconnected(WebSocket socket)
    {
      var socketId = WebSocketConnectionManager.GetId(socket);
            
      await base.OnDisconnected(socket);

      _log.Debug($"{socketId}: Disconnected");
      await SendMessageToAllAsync($"{socketId} disconnected");
    }
  }
}
