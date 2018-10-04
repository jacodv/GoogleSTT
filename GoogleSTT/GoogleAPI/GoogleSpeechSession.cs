using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Speech.V1;
using Google.Protobuf;
using log4net;

namespace GoogleSTT.GoogleAPI
{
  public class GoogleSpeechSession : IGoogleSpeechSession, IDisposable
  {
    private readonly string _sessionId = Guid.NewGuid().ToString("N");
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private SpeechClient.StreamingRecognizeStream _streamingCall;
    private Task _handleResponses;
    private Task _processQueueItems;
    private ConcurrentQueue<AudioQueueItem> _audioQueue = new ConcurrentQueue<AudioQueueItem>();
    private int _queueProcessingDelay = 50;

    public GoogleSpeechSession(string socketId, GoogleSessionConfig config, Action<string, string[]> processTranscripts)
    {
      try
      {
        _log.Debug($"New google api session: SocketId={socketId} - SessionId={_sessionId} - {config}");
        SockedId = socketId;
        Config = config;
        ProcessTranscripts = processTranscripts;
        _connect().Wait();
        IsOpen = true;
        _processQueueItems = Task.Run(ProcessQueue);
      }
      catch (Exception e)
      {
        _log.Error(e);
      }
    }

    public Action<string, string[]> ProcessTranscripts { get; set; }
    public bool IsOpen { get; private set; }
    public string SockedId { get; }
    public GoogleSessionConfig Config { get; }

    public void SendAudio(byte[] buffer)
    {
      _audioQueue.Enqueue(new AudioQueueItem() { Buffer = buffer, WriteComplete = false });
    }
    public void WriteComplete()
    {
      _audioQueue.Enqueue(new AudioQueueItem() { WriteComplete = true });
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
    public async Task ProcessQueue()
    {
      _log.Debug($"Start processing the audio queue: SocketId:{SockedId} | SessionId:{_sessionId}");
      while (true)
      {
        if (!IsOpen)
        {
          _log.Warn($"Cannot process queue if session is closed: SocketId:{SockedId} | SessionId:{_sessionId}");
          return;
        }

        while (_audioQueue.TryDequeue(out var queueItem))
        {
          if (queueItem == null)
            break;

          _log.Debug($"Dequeuing: SocketId:{SockedId} | SessionId:{_sessionId}");
          if (queueItem.Buffer?.Length > 0)
            await _submitToGoogle(queueItem.Buffer);

          if (!queueItem.WriteComplete)
            continue;

          await _writeComplete();
          return;
        }
        await Task.Delay(_queueProcessingDelay);
      }
    }
    public void Close(bool writeComplete)
    {
      if (writeComplete)
        WriteComplete();

      _processQueueItems.Wait(_queueProcessingDelay * 4);
      _handleResponses.Wait();
      IsOpen = false;
    }

    #region Private
    private async Task _connect()
    {
      _log.Debug($"Connecting: SocketId={SockedId} - SessionId={_sessionId}");
      try
      {
        if (_streamingCall == null)
        {
          var speech = SpeechClient.Create();
          _streamingCall = speech.StreamingRecognize();
          _log.Debug(@"Received the streaming context for: SocketId={SockedId} - SessionId={_sessionId}");
        }

        await _openStreamingContext();
        _handleResponses = Task.Run(HandleResponses);
      }
      catch (Exception connectEx)
      {
        _log.Error(connectEx);
        throw;
      }
    }
    private async Task _openStreamingContext()
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
    private async Task _writeBufferToStreamingContext(byte[] buffer)
    {
      using (var stream = new MemoryStream(buffer))
      {
        var streamingRecognizeRequest = new StreamingRecognizeRequest() { AudioContent = ByteString.FromStream(stream) };
        _log.Debug($"Submitting to Google: SocketId={SockedId} - SessionId={_sessionId} | {buffer.Length}");
        await _streamingCall.WriteAsync(streamingRecognizeRequest);
        stream.Close();
      }
    }
    private async Task _submitToGoogle(byte[] buffer)
    {
      try
      {
        if (!IsOpen)
        {
          _log.Warn($"Cannot send audio to closed session: {SockedId}");
          return;
        }

        await _writeBufferToStreamingContext(buffer);
      }
      catch (Exception sendEx)
      {
        _log.Error(sendEx);
        throw;
      }
    }
    private async Task _writeComplete()
    {
      _log.Debug($"_writeComplete: SocketId={SockedId} - SessionId={_sessionId}");
      await _streamingCall.WriteCompleteAsync();
      await _handleResponses;
    }
    #endregion

    public void Dispose()
    {
      _log.Debug($"Disposing the GoogleSpeechSession:{SockedId}");
      _processQueueItems?.Dispose();
      _handleResponses?.Dispose();
    }
  }

  public class AudioQueueItem
  {
    public byte[] Buffer { get; set; }
    public bool WriteComplete { get; set; }
  }
}