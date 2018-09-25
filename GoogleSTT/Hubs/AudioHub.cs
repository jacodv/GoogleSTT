using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace GoogleSTT.Hubs
{
  public class AudioHub : Hub
  {
    public async Task SendAudio(byte[] audioData)
    {
      var base64 = Convert.ToBase64String(audioData);
      Console.WriteLine(base64);
    }  
    
  }

  public class ChatHub : Hub  
  {  
    public async Task SendMessage(string user, string message)  
    {  
      await Clients.All.SendAsync("ReceiveMessage", user, message);  
    }  
  }  
}
