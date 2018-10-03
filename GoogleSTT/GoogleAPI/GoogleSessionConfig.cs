using Google.Cloud.Speech.V1;

namespace GoogleSTT.GoogleAPI
{
  public class GoogleSessionConfig
  {
    public RecognitionConfig.Types.AudioEncoding AudioEncoding { get; set; } = RecognitionConfig.Types.AudioEncoding.Linear16;
    public int SampleRateHertz { get; set; } = 48000;
    public string LanguageCode { get; set; } = "en";
    public bool InterimResults { get; set; } = true;

    public override string ToString()
    {
      return $"{AudioEncoding}|{SampleRateHertz}|{LanguageCode}|{InterimResults}";
    }
  }
}