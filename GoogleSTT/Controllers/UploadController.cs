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
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private IHostingEnvironment _hostingEnvironment;

    public UploadController(IHostingEnvironment hostingEnvironment)
    {
      _hostingEnvironment = hostingEnvironment;
    }

    [HttpPost, DisableRequestSizeLimit]
    [Route("UploadFile/{id}")]
    public async Task<IActionResult> UploadFile(string id)
    {
      try
      {
        if (id == null) throw new ArgumentNullException(nameof(id));

        _log.Info($"Sending audio to session:{id}");

        var file = Request.Form.Files.LastOrDefault(f=>f.Length>0);
        const string folderName = "Upload";
        //var webRootPath = _hostingEnvironment.WebRootPath;
        //var newPath = Path.Combine(webRootPath, folderName);
        var newPath = Path.Combine(@"c:\temp", folderName);
        if (!Directory.Exists(newPath))
        {
          Directory.CreateDirectory(newPath);
        }

        if (file==null || file.Length <= 0) 
          return Json("No data found!");
        
        var fileName = $"uploadedFile{DateTime.Now:yyyyMMddHHmmss}.wav";//ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
        var fullPath = Path.Combine(newPath, fileName);

        var fileData = new byte[file.Length];

        using (var stream = new MemoryStream())
        {
          await file.CopyToAsync(stream, CancellationToken.None);
          fileData = stream.ToArray();
        }

        System.IO.File.WriteAllBytes(fullPath, fileData);

        try
        {
          await GoogleSpeechFactory.SendAudio(id, fileData, true);
          _log.Info($"Sent audio:{fileData.Length} to session:{id}");
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
  }
}
