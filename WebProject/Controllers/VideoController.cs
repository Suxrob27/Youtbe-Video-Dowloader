using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebProject.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebProject.Controllers
{
    public class VideoController : Controller
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly string _ytDlpPath;

        public VideoController(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            _ytDlpPath = Path.Combine(_hostingEnvironment.WebRootPath, @"yt-dlp\yt-dlp.exe");
        }

        [HttpGet]
        public IActionResult Download()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Download(VideoModel model)
        {
            if (ModelState.IsValid)
            {
                string videoUrl = model.VideoUrl;
                string filePath = await DownloadVideoAsync(videoUrl, model.Quality);

                if (System.IO.File.Exists(filePath))
                {
                    // Return the file to the user for download
                    var memory = new MemoryStream();
                    using (var stream = new FileStream(filePath, FileMode.Open))
                    {
                        await stream.CopyToAsync(memory);
                    }
                    memory.Position = 0;

                    // Provide the file for download with a prompt
                    return File(memory, "video/mp4", Path.GetFileName(filePath));
                }
                else
                {
                    ModelState.AddModelError("", "Failed to download video.");
                }
            }
            if (ModelState.ContainsKey("Sign in to confirm you're not a bot"))
            {
                ModelState.AddModelError("", "The video requires a login. Please ensure you are providing valid authentication.");
            }
            return View(model);

        }

        private async Task<string> DownloadVideoAsync(string videoUrl, string quality)
        {
            string downloadFolder = Path.Combine(_hostingEnvironment.WebRootPath, "downloads");

            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
            }

            CheckFfmpegInstallation();

            string videoFileTemplate = Path.Combine(downloadFolder, "%(title)s_video.%(ext)s");
            string audioFileTemplate = Path.Combine(downloadFolder, "%(title)s_audio.%(ext)s");
            string outputFileTemplate = Path.Combine(downloadFolder, "%(title)s_final.mp4");

            string formatCode = quality switch
            {
                "720" => "bestvideo[height<=720]",
                "480" => "bestvideo[height<=480]",
                _ => "bestvideo"
            };

            string audioFormatCode = "bestaudio";

            var videoDownloadTask = StartYtDlpProcessAsync(formatCode, videoFileTemplate, videoUrl);
            var audioDownloadTask = StartYtDlpProcessAsync(audioFormatCode, audioFileTemplate, videoUrl);

            await Task.WhenAll(videoDownloadTask, audioDownloadTask);

            if (videoDownloadTask.Result != 0 || audioDownloadTask.Result != 0)
            {
                Trace.WriteLine("Video or audio download failed.");
                return null;
            }

            return await MergeVideoAndAudioAsync(videoFileTemplate, audioFileTemplate, outputFileTemplate);
        }
        // Helper method to start yt-dlp process for video/audio download
        private int StartYtDlpProcess(string formatCode, string outputFileTemplate, string videoUrl)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                Arguments = $"-f {formatCode}  --cookies \"{Path.Combine(_hostingEnvironment.WebRootPath, "cookies.txt")}\" --user-agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36\" --referer \"https://www.youtube.com/\"-o \"{outputFileTemplate}\" \"{videoUrl}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Trace.WriteLine($"Download failed: {error}");
                }

                return process.ExitCode;
            }
        }

        // Merging video and audio using ffmpeg
        private async Task<string> MergeVideoAndAudioAsync(string videoFilePath, string audioFilePath, string outputFilePath)
        {
            string ffmpegPath = Path.Combine(_hostingEnvironment.WebRootPath, @"yt-dlp\ffmpeg.exe");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{videoFilePath}\" -i \"{audioFilePath}\" -c:v copy -c:a aac -strict experimental \"{outputFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                Trace.WriteLine($"ffmpeg Output: {output}");
                Trace.WriteLine($"ffmpeg Error: {error}");

                if (process.ExitCode != 0)
                {
                    Trace.WriteLine($"Merge failed with exit code {process.ExitCode}. Error: {error}");
                    return null;
                }

                return outputFilePath;
            }
        }
        // Ensure that ffmpeg is installed
        private void CheckFfmpegInstallation()
        {
            string ffmpegPath = Path.Combine(_hostingEnvironment.WebRootPath, @"yt-dlp\ffmpeg.exe");
            if (!System.IO.File.Exists(ffmpegPath))
            {
                throw new FileNotFoundException("ffmpeg.exe not found. Ensure ffmpeg is properly installed.");
            }
        }
        private async Task<int> StartYtDlpProcessAsync(string formatCode, string outputFileTemplate, string videoUrl)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                Arguments = $"-f {formatCode} --cookies \"{Path.Combine(_hostingEnvironment.WebRootPath, "cookies.txt")}\" --user-agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36\" --referer \"https://www.youtube.com/\" -o \"{outputFileTemplate}\" \"{videoUrl}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                Trace.WriteLine($"yt-dlp Output: {output}");
                Trace.WriteLine($"yt-dlp Error: {error}");

                if (process.ExitCode != 0)
                {
                    Trace.WriteLine($"Download failed with exit code {process.ExitCode}. Error: {error}");
                }

                return process.ExitCode;
            }
        }
    }
}