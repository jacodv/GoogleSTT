using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Speech.V1;
using log4net;
using Microsoft.AspNetCore.Identity.UI.Pages.Internal.Account;

namespace GoogleSTT.GoogleAPI
{
  public static class GoogleSpeechFactory
  {
    private static ConcurrentDictionary<string, GoogleSpeechSession> _sessions;

    static GoogleSpeechFactory()
    {
      _sessions = new ConcurrentDictionary<string, GoogleSpeechSession>();
    }

    public static GoogleSpeechSession CreateSession(string socketId, GoogleSessionConfig config)
    {
      var session = new GoogleSpeechSession(socketId, config);
      _sessions.GetOrAdd(socketId, session);
      return session;
    }

  }

  public class GoogleSessionConfig
  {
    public RecognitionConfig.Types.AudioEncoding AudioEncoding { get; set; } = RecognitionConfig.Types.AudioEncoding.Linear16;
    public int SampleRateHertz { get; set; } = 16000;
    public string LanguageCode { get; set; } = "en";
    public bool InterimResults { get; set; } = true;
  }

  public class GoogleSpeechSession
  {
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private SpeechClient _speech;
    private SpeechClient.StreamingRecognizeStream _streamingCall;
    readonly object writeLock = new Object();
    private CancellationTokenSource _closeTokenSource;

    public GoogleSpeechSession(string socketId, GoogleSessionConfig config)
    {
      _speech = SpeechClient.Create();
      _streamingCall = _speech.StreamingRecognize();
      SockedId = socketId;
      Config = config;
    }

    public async Task Connect()
    {
      await _streamingCall.WriteAsync(new StreamingRecognizeRequest()
        {
          StreamingConfig = new StreamingRecognitionConfig()
          {
            Config = new RecognitionConfig()
            {
              Encoding = Config.AudioEncoding,
              SampleRateHertz = Config.SampleRateHertz,
              LanguageCode = Config.LanguageCode,
            },
            InterimResults = Config.InterimResults,
          }
        });

      _closeTokenSource = new CancellationTokenSource();

      HandleResponses = Task.Run(async () =>
      {
        while (await _streamingCall.ResponseStream.MoveNext(_closeTokenSource.Token))
        {
          var transcripts = new List<string>();
          foreach (var result in _streamingCall.ResponseStream.Current.Results)
          {
            foreach (var alternative in result.Alternatives)
            {
              _log.Debug($"Transcript: {alternative.Transcript}");
              transcripts.Add(alternative.Transcript);
            }
          }

          ProcessTranscripts?.Invoke(transcripts.ToArray());
        }
      });
    }

    public async Task Close()
    {
      lock (writeLock)
      {
        IsOpen = false;
        _closeTokenSource.Cancel();
      }
      await _streamingCall.WriteCompleteAsync();
    }

    public bool IsOpen { get; private set; }
    public string SockedId { get; }
    public GoogleSessionConfig Config { get; }

    public Task SendAudio(byte[] buffer)
    {
      lock (writeLock)
      {
        if (!IsOpen) 
          return Task.FromResult(-1);

        _streamingCall.WriteAsync(new StreamingRecognizeRequest()
        {
          AudioContent = Google.Protobuf.ByteString.CopyFrom(buffer, 0, buffer.Length)
        }).Wait();
      }

      return Task.FromResult(0);
    }

    public Task HandleResponses { get; private set; }
    public Action<string[]> ProcessTranscripts { get; set; }
  }
}
