using System;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GoogleSTT.Websockets;
using log4net;

namespace GoogleSTT.Websockets
{
  public abstract class WebSocketHandler
  {
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    protected WebSocketConnectionManager WebSocketConnectionManager { get; set; }

    public WebSocketHandler(WebSocketConnectionManager webSocketConnectionManager)
    {
      WebSocketConnectionManager = webSocketConnectionManager;
    }

    public virtual async Task OnConnected(WebSocket socket)
    {
      _log.Debug($"Adding new web socket: {socket.State}");
      WebSocketConnectionManager.AddSocket(socket);
    }

    public virtual async Task OnDisconnected(WebSocket socket)
    {
      _log.Debug($"Disconnecting socket: {socket.CloseStatusDescription}");
      await WebSocketConnectionManager.RemoveSocket(WebSocketConnectionManager.GetId(socket));
    }

    public async Task SendMessageAsync(WebSocket socket, string message)
    {
      if(socket.State != WebSocketState.Open)
      {
        _log.Warn($"Invalid socket state({socket.State}), for message: {message}");
        return;
      }
      await socket.SendAsync(buffer: new ArraySegment<byte>(array: Encoding.ASCII.GetBytes(message),
          offset: 0, 
          count: message.Length),
        messageType: WebSocketMessageType.Text,
        endOfMessage: true,
        cancellationToken: CancellationToken.None);          
    }

    public async Task SendMessageAsync(string socketId, string message)
    {
      await SendMessageAsync(WebSocketConnectionManager.GetSocketById(socketId), message);
    }

    public async Task SendMessageToAllAsync(string message)
    {
      foreach(var pair in WebSocketConnectionManager.GetAll())
      {
        if(pair.Value.State == WebSocketState.Open)
          await SendMessageAsync(pair.Value, message);
      }
    }

    public abstract Task ReceiveAsyncText(WebSocket socket, WebSocketReceiveResult result, byte[] buffer);
    public abstract Task ReceiveAsyncData(WebSocket socket, WebSocketReceiveResult result, byte[] buffer);
  }}
