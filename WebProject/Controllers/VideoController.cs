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
            return View(model);
        }

        private async Task<string> DownloadVideoAsync(string videoUrl, string quality)
        {
            // Path to save the downloaded video
            string downloadFolder = Path.Combine(_hostingEnvironment.WebRootPath, "downloads");

            // Ensure the directory exists
            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
            }

            // Use yt-dlp's dynamic title as filename template
            string fileTemplate = Path.Combine(downloadFolder, "%(title)s.%(ext)s");

            // Map user-selected quality to yt-dlp format codes
            string formatCode = quality switch
            {
                "720" => "136", // 720p video code
                "480" => "135", // 480p video code
                _ => "137"      // Default to 1080p if not specified
            };

            var processStartInfo = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                Arguments = $"-f{formatCode} --merge-output-format mp4  --cookies \"{Path.Combine(_hostingEnvironment.WebRootPath, "cookies.txt")}\" --user-agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36\" --referer \"https://www.youtube.com/\" -o \"{fileTemplate}\" {videoUrl}",
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
                    Trace.WriteLine($"Output: {output}");
                    Trace.WriteLine($"Error: {error}");
                    return null;
                }

                // Parse the output to get the generated file name
                string downloadedFilePath = GetDownloadedFilePath(output, downloadFolder);
                return downloadedFilePath;
            }
        }
        // This method retrieves the file path from the yt-dlp output or download folder
        private string GetDownloadedFilePath(string ytDlpOutput, string downloadFolder)
        {
            // Find the actual file downloaded (it should be in the download folder)
            // You can check the output for file information or just get the latest file in the folder
            var directory = new DirectoryInfo(downloadFolder);
            var latestFile = directory.GetFiles().OrderByDescending(f => f.LastWriteTime).FirstOrDefault();

            return latestFile?.FullName;
        }
    }
}