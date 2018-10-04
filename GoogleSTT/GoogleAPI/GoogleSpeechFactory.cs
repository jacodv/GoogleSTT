using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Google.Api.Gax;
using log4net;

namespace GoogleSTT.GoogleAPI
{
  public static class GoogleSpeechFactory
  {
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private static readonly ConcurrentDictionary<string, GoogleSpeechSession> _sessions;

    static GoogleSpeechFactory()
    {
      _sessions = new ConcurrentDictionary<string, GoogleSpeechSession>();
    }

    public static GoogleSpeechSession CreateSession(string socketId, GoogleSessionConfig config, Action<string, string[]> processTranscripts)
    {
      _log.Debug("Creating new GOOGLE SPEECH SESSION");
      var session = new GoogleSpeechSession(socketId, config, processTranscripts);
      _sessions.TryAdd(socketId, session);
      return session;
    }

    public static void CloseSession(string socketId, bool writeComplete)
    {
      _log.Debug("Closing new GOOGLE SPEECH SESSION");
      if (_sessions.ContainsKey(socketId))
        _sessions[socketId].Close(writeComplete);
    }

    public static void SendAudio(string socketId, byte[] data, bool writeComplete)
    {
      if (string.IsNullOrEmpty(socketId))
        throw new ArgumentNullException(nameof(socketId));
      if (!_sessions.ContainsKey(socketId))
        throw new InvalidOperationException($"SocketId: {socketId} not registered");
      _log.Debug($"Received audio on for: {socketId} | {data.Length}");

      if (data.Length == 0)
      {
        _log.Warn("NO DATA FOR GOOGLE SPEECH SESSION");
        return;
      }

      _sessions[socketId].SendAudio(data);

      if (writeComplete)
        _sessions[socketId].WriteComplete();

    }
  }
}
