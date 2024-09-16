using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebProject.Models;

namespace WebProject.Controllers
{
    public class DownloadController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetVideoQualities(string videoUrl)
        {
            if (string.IsNullOrWhiteSpace(videoUrl))
            {
                ViewBag.Message = "Please enter a valid video URL.";
                return View("Index");
            }

            string ytDlpPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "yt-dlp", "yt-dlp.exe");

            if (!System.IO.File.Exists(ytDlpPath))
            {
                ViewBag.Message = "yt-dlp executable not found.";
                return View("Index");
            }

            string arguments = $"--cookies \"{SD.cookieFile}\" --dump-json \"{videoUrl}\"";

            var processInfo = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (var process = Process.Start(processInfo))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        try
                        {
                            JObject videoInfo = JObject.Parse(output);
                            var formats = videoInfo["formats"];
                            var title = videoInfo["title"]?.ToString();  // Extract video title

                            if (formats == null)
                            {
                                ViewBag.Message = "No formats found for the provided URL.";
                                return View("Index");
                            }

                            List<string> availableQualities = new List<string>();
                            foreach (var format in formats)
                            {
                                string formatId = format["format_id"]?.ToString();
                                string formatNote = format["format_note"]?.ToString();
                                string ext = format["ext"]?.ToString();

                                if (formatId != null && formatNote != null && ext != null)
                                {
                                    string videoFormat = $"{formatId} - {formatNote} - {ext}";
                                    availableQualities.Add(videoFormat);
                                }
                            }

                            ViewBag.Qualities = availableQualities;
                            ViewBag.VideoUrl = videoUrl;
                            ViewBag.Title = title;  // Pass the video title to the view
                        }
                        catch (Exception ex)
                        {
                            ViewBag.Message = "Error parsing video formats: " + ex.Message;
                        }
                    }
                    else
                    {
                        string errorOutput = await process.StandardError.ReadToEndAsync();
                        ViewBag.Message = "Error retrieving video formats: " + errorOutput;
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An error occurred: " + ex.Message;
            }

            return View("Index");
        }
        [HttpPost]
        public async Task<IActionResult> DownloadVideo(string videoUrl, string formatCode)
        {
            if (string.IsNullOrWhiteSpace(videoUrl) || string.IsNullOrWhiteSpace(formatCode))
            {
                ViewBag.Message = "Invalid video URL or format.";
                return View("Index");
            }

            string ytDlpPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "yt-dlp", "yt-dlp.exe");

            // Step 1: Retrieve video metadata to get the title
            string metadataArguments = $"--cookies \"{SD.cookieFile}\" --dump-json \"{videoUrl}\"";
            string videoTitle = "downloaded_video";  // Default title if retrieval fails

            try
            {
                var metadataProcessInfo = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = metadataArguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var metadataProcess = Process.Start(metadataProcessInfo))
                {
                    string metadataOutput = await metadataProcess.StandardOutput.ReadToEndAsync();
                    await metadataProcess.WaitForExitAsync();

                    if (metadataProcess.ExitCode == 0)
                    {
                        // Step 2: Parse the video metadata to extract the title
                        JObject videoInfo = JObject.Parse(metadataOutput);
                        videoTitle = videoInfo["title"]?.ToString() ?? videoTitle;
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error retrieving video title: " + ex.Message;
                return View("Index");
            }

            // Clean up the title for safe file naming (remove invalid characters)
            string safeFileName = string.Concat(videoTitle.Split(Path.GetInvalidFileNameChars())) + ".mp4";

            // Step 3: Download the video with format that includes both video and audio
            string downloadArguments = $"--cookies \"{SD.cookieFile}\" -f \"{formatCode}+bestaudio\" -o - \"{videoUrl}\"";

            var downloadProcessInfo = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = downloadArguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (var process = Process.Start(downloadProcessInfo))
                {
                    Response.ContentType = "video/mp4";
                    Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{safeFileName}\"");

                    await process.StandardOutput.BaseStream.CopyToAsync(Response.Body);
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        string errorOutput = await process.StandardError.ReadToEndAsync();
                        ViewBag.Message = "Error during download: " + errorOutput;
                        return View("Index");
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An error occurred: " + ex.Message;
                return View("Index");
            }

            return new EmptyResult();
        }
    }
}