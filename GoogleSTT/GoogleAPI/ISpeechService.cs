using System;

namespace GoogleSTT.GoogleAPI
{
  public interface ISpeechService
  {
    GoogleSpeechSession CreateSession(string socketId, GoogleSessionConfig config, Action<string, string[]> processTranscripts);
    void SendAudio(string socketId, byte[] data, bool writeComplete);
    void CloseSession(string socketId, bool writeComplete);
  }
}