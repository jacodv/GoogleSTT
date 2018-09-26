using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using log4net;

namespace GoogleSTT.GoogleAPI
{
  public static class GoogleSpeechFactory
  {
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private static ConcurrentDictionary<string, GoogleSpeechSession> _sessions;

    static GoogleSpeechFactory()
    {
      _sessions = new ConcurrentDictionary<string, GoogleSpeechSession>();
    }

    public static GoogleSpeechSession CreateSession(string socketId, GoogleSessionConfig config, Action<string,string[]> processTranscripts)
    {
      _log.Debug("Creating new GOOGLE SPEECH SESSION");
      var session = new GoogleSpeechSession(socketId, config,processTranscripts);
      _sessions.TryAdd(socketId, session);
      return session;
    }

    public static void CloseSession(string socketId)
    {
      _log.Debug("Closing new GOOGLE SPEECH SESSION");
      if (_sessions.ContainsKey(socketId))
        _sessions[socketId].Close().Wait();
    }

    public static async Task SendAudio(string socketId, byte[] data)
    {
      //_log.Debug("Sending to GOOGLE SPEECH SESSION");
      if(string.IsNullOrEmpty(socketId))
        throw new ArgumentNullException(nameof(socketId));
      if(!_sessions.ContainsKey(socketId))
        throw new InvalidOperationException($"SocketId: {socketId} not registered");

      if (data.Length == 0)
      {
        _log.Warn("NO DATA FOR GOOGLE SPEECH SESSION");
        return;
      }

      await _sessions[socketId].SendAudio(data);
    }
  }
}
