using System;
using System.Threading.Tasks;

namespace GoogleSTT.GoogleAPI
{
  public interface IGoogleSpeechSession
  {
    Action<string, string[]> ProcessTranscripts { get; set; }
    bool IsOpen { get; }
    string SockedId { get; }
    GoogleSessionConfig Config { get; }
    void SendAudio(byte[] buffer);
    void WriteComplete();
    Task HandleResponses();
    void Close(bool writeComplete);
    Task SendFile(byte[] data);
  }
}