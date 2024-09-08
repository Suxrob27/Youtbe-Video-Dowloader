using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebProject.Models;

namespace WebProject.Controllers
{
    public class VideoController : Controller
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly string _ytDlpPath;

        public VideoController(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            _ytDlpPath = Path.Combine(_hostingEnvironment.WebRootPath, "yt-dlp.exe");
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
                string filePath = await DownloadVideoAsync(videoUrl,model.Quality);

                if (System.IO.File.Exists(filePath))
                {
                    return File(filePath, "video/mp4", Path.GetFileName(filePath));
                }
                else
                {
                    ModelState.AddModelError("", "Failed to download video.");
                }
            }
            return View(model);
        }

        private async Task<string> DownloadVideoAsync(string videoUrl,string quality)
        {
            string fileName = "downloaded_video.mp4";
            string filePath = Path.Combine(_hostingEnvironment.WebRootPath, fileName);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                Arguments = $"-f {quality} -o \"{filePath}\" {videoUrl}",
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

                if (process.ExitCode != 0)
                {
                    // Log or handle error
                    System.Diagnostics.Trace.WriteLine($"Error: {error}");
                    return null;
                }

                return filePath;
            }

        }
    }




}  