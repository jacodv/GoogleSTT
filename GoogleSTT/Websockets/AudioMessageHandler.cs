using System;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GoogleSTT.GoogleAPI;
using log4net;

namespace GoogleSTT.Websockets
{
  public class AudioMessageHandler: WebSocketHandler
  {
    private readonly ISpeechService _speechService;
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    public AudioMessageHandler(WebSocketConnectionManager webSocketConnectionManager, ISpeechService speechService) : base(webSocketConnectionManager)
    {
      _speechService = speechService;
    }

    public override async Task OnConnected(WebSocket socket)
    {
      await base.OnConnected(socket);

      var socketId = WebSocketConnectionManager.GetId(socket);
      _log.Debug($"SocketId:{socketId}-Connected");
      // ReSharper disable once StringLiteralTypo
      await SendMessageAsync(socket,$"SOCKETID:{socketId}");
    }
    public override async Task OnDisconnected(WebSocket socket)
    {
      var socketId = WebSocketConnectionManager.GetId(socket);
            
      await base.OnDisconnected(socket);

      await Task.Run(()=> _speechService.CloseSession(socketId, true));

      _log.Debug($"{socketId}: Disconnected");
      await SendMessageToAllAsync($"{socketId} disconnected");
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
    public override async Task ReceiveAsyncData(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
    {
      try
      {
        var socketId = WebSocketConnectionManager.GetId(socket);
        await Task.Run(()=> _speechService.SendAudio(socketId, new ArraySegment<byte>(buffer, 0, result.Count).Array, false));
      }
      catch (Exception e)
      {
        _log.Error($"Socket ReceiveAsyncData:{e.Message}",e);
        throw;
      }
    }
   
  }
}
