using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;

namespace BiliDownload.Helper
{
    public static class VideoHelper
    {
        public static async Task MakeVideoAsync(StorageFile video, StorageFile audio, string outputPath)
        {
            var info = new ProcessStartInfo()
            {
                CreateNoWindow = true,
                FileName = "FFmpeg/ffmpeg.exe",
                Arguments = $"-i \"{video.Path}\" -i \"{audio.Path}\" -c:v copy -c:a copy -strict experimental \"{outputPath}\" -y",
            };
            var p = new Process()
            {
                StartInfo = info
            };
            p.Start();
            await p.WaitForExitAsync();
            if (p.ExitCode != 0) throw new System.Exception();
        }
    }
}
