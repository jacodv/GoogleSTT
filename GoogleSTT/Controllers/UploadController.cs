using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GoogleSTT.GoogleAPI;
using log4net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace GoogleSTT.Controllers
{
  [Produces("application/json")]
  [Route("api/[controller]")]
  public class UploadController : Controller
  {
    private readonly ISpeechService _speechService;
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    public UploadController(ISpeechService speechService)
    {
      _speechService = speechService;
    }


    [HttpPost, DisableRequestSizeLimit]
    [Route("UploadFile/{id}")]
    public async Task<IActionResult> UploadFile(string id)
    {
      var debugFileName = $"uploadedFile{DateTime.Now:yyyyMMddHHmmss}.wav";
      var fileData = await _getUploadedBuffer();
      await _speechService.SendFile(id, fileData);
      return Json("Upload File Successful.");
    }

    [HttpPost, DisableRequestSizeLimit]
    [Route("UploadStream/{id}")]
    public async Task<IActionResult> UploadStream(string id)
    {
      return await _submitStream(id, false, $"uploadedStream{DateTime.Now:yyyyMMddHHmmss.fff}.wav");
    }

    [HttpPost]
    [Route("StopStream/{id}")]
    public IActionResult StopStream(string id)
    {
      try
      {
        if (id == null)
          throw new ArgumentNullException(nameof(id));

        _log.Info($"Stopping audio session:{id}");

        _speechService.CloseSession(id, true);

        return Json("Closed Successfully");
      }
      catch (Exception ex)
      {
        _log.Error(ex);
        return Json("Close Failed: " + ex.Message);
      }
    }

    #region Private
    private static void _writeBufferToFile(byte[] fileData, string fileName)
    {
      const string folderName = "Upload";
      var newPath = Path.Combine(@"c:\temp", folderName);
      if (!Directory.Exists(newPath))
      {
        Directory.CreateDirectory(newPath);
      }

      var fullPath = Path.Combine(newPath, fileName);
      System.IO.File.WriteAllBytes(fullPath, fileData);
    }
    private async Task<byte[]> _getUploadedBuffer()
    {
      byte[] fileData;
      var file = Request.Form.Files.LastOrDefault(f => f.Length > 0);
      if (file == null || file.Length <= 0)
        return default(byte[]);

      using (var stream = new MemoryStream())
      {
        await file.CopyToAsync(stream, CancellationToken.None);
        fileData = stream.ToArray();
      }

      return fileData;
    }
    private async Task<IActionResult> _submitStream(string id, bool writeComplete, string debugFileName)
    {
      if (id == null)
        throw new ArgumentNullException(nameof(id));

      try
      {
        var fileData = await _getUploadedBuffer();
        if (fileData.Length == 0)
          return Json("No data found!");

        _writeBufferToFile(fileData, debugFileName);

        try
        {
          _speechService.SendAudio(id, fileData, writeComplete);
          _log.Debug($"Sent audio:{fileData.Length} to session:{id}");
        }
        catch (Exception e)
        {
          _log.Error("Failed to send audio to GOOGLE", e);
          throw;
        }

        return Json("Upload Successful.");
      }
      catch (Exception ex)
      {
        _log.Error(ex);
        return Json("Upload Failed: " + ex.Message);
      }
    }
    #endregion

  }
}
