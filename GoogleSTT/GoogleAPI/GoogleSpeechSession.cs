using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Speech.V1;
using Google.Protobuf;
using Google.Rpc;
using log4net;

namespace GoogleSTT.GoogleAPI
{
  public class GoogleSpeechSession
  {
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private readonly SpeechClient.StreamingRecognizeStream _streamingCall;
    readonly object _writeLock = new object();
    private CancellationTokenSource _closeTokenSource;
    private bool _responseIsBeingHandled = false;
    private Task _handleResponses;
    private MemoryStream _audioBuffer = new MemoryStream();
    //private string _audioFileName = null;
    ///private FileStream _audioFileStream;

    public GoogleSpeechSession(string socketId, GoogleSessionConfig config, Action<string, string[]> processTranscripts)
    {
      try
      {
        _log.Debug($"Creating google api session: {socketId} - {config}");
        var speech = SpeechClient.Create();
        _streamingCall = speech.StreamingRecognize();
        SockedId = socketId;
        Config = config;
        ProcessTranscripts = processTranscripts;

       // _audioFileName = $"GoogleSpeechSession{DateTime.Now:yyyyMMddHHmmss}.wav";
       // _audioFileStream = File.OpenWrite(Path.Combine($@"c:\temp\Upload", _audioFileName));
      }
      catch (Exception e)
      {
        _log.Error(e);
      }
    }

    public Action<string, string[]> ProcessTranscripts { get; set; }

    private async Task Connect()
    {
      try
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
      }
      catch (Exception connectEx)
      {
        _log.Error(connectEx);
        throw;
      }

      _closeTokenSource = new CancellationTokenSource();
    }

    public async Task Close(bool writeComplete)
    {
      lock (_writeLock)
      {
        IsOpen = false;
        //_closeTokenSource?.Cancel();
      }
      //_audioFileStream.Close();
      if (writeComplete)
      {
        await _streamingCall.WriteCompleteAsync();
        await _handleResponses;
      }
    }

    public bool IsOpen { get; private set; }
    public string SockedId { get; }
    public GoogleSessionConfig Config { get; }

    public Task SendAudio(byte[] buffer)
    {
      lock (_writeLock)
      {
        if (!_responseIsBeingHandled)
        {
          _responseIsBeingHandled = true;
          Connect().Wait();
          IsOpen = true;
          _log.Info($"Start handling the responses: {SockedId}");
          _handleResponses = Task.Run(HandleResponses);
        }

        try
        {
          if (!IsOpen)
          {
            _log.Warn($"Cannot send audio to closed session: {SockedId}");
            return Task.FromResult(-1);
          }

          //_audioFileStream.WriteAsync(buffer, 0, buffer.Length).Wait();
          _audioBuffer.Write(buffer, 0, buffer.Length);

          if (_audioBuffer.Length < 32768)
          {
            _log.Debug($"Buffering: {buffer.Length} | {_audioBuffer.Length}");
            return Task.FromResult(0);
          }

          _audioBuffer.Position = 0;
          _streamingCall.WriteAsync(new StreamingRecognizeRequest()
          {
            AudioContent = ByteString.FromStream(_audioBuffer)
          }).Wait();
          _log.Info($"Sent audio data to Google: {_audioBuffer.Length}");

          _audioBuffer.Dispose();
          _audioBuffer = new MemoryStream();
        }
        catch (Exception sendEx)
        {
          _log.Error(sendEx);
          throw;
        }
        return Task.FromResult(0);
      }
    }

    public async Task WriteComplete()
    {
      await _streamingCall.WriteCompleteAsync();
      await _handleResponses;
    }

    public async Task HandleResponses()
    {
      try
      {
        _log.Info("START handling the response");
        while (await _streamingCall.ResponseStream.MoveNext(default(CancellationToken)))
        {
          _log.Info("New response from Google");
          var responseError = _streamingCall.ResponseStream.Current?.Error;
          if (responseError != null)
          {
            _log.Error($"RESPONSE ERROR:{responseError.Code}: {responseError.Message}: {string.Join("||DetailCount:", responseError.Details?.Count)}");
          }
          var transcripts = new List<string>();

          if (_streamingCall.ResponseStream.Current?.Results != null)
          {
            foreach (var result in _streamingCall.ResponseStream.Current?.Results)
            {
              foreach (var alternative in result.Alternatives)
              {
                _log.Info($"Transcript: {alternative.Transcript}");
                transcripts.Add(alternative.Transcript);
              }
            }
          }

          ProcessTranscripts?.Invoke(SockedId, transcripts.ToArray());
          _log.Info("END handling the response");
        }
      }
      catch (Exception handleResponseEx)
      {
        _log.Error(handleResponseEx);
        throw;
      }
    }

  }
}