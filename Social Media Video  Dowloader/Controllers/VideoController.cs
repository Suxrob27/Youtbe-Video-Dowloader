using Grpc.Core;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Web.Mvc;

namespace Social_Media_Video__Dowloader.Controllers
{
    public class VideoController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DownLoadYoutubeVideo(string url)
        {
            string videoUrl = GetVideoMP4Url(url);

            try
            {
                WebClient wc = new WebClient();
                string fileName = @"E:\video\" + Guid.NewGuid().ToString() + ".mp4";
                wc.DownloadFile(videoUrl, fileName);

                Response.ContentType = "application/octet-stream";
                Response.AddHeader("Content-Disposition", "attachment; filename="
                    + new FileInfo(fileName).Name);
                Response.WriteFile(fileName);
                Response.End();
            }
            catch (Exception ex)
            {
            }

            return View();
        }

        private string GetVideoMP4Url(string url)
        {
            try
            {
                WebRequest req = WebRequest.Create(url);
                StreamReader reader = new StreamReader(
                    req.GetResponse().GetResponseStream());
                string content = reader.ReadToEnd();

                int start = content.IndexOf("vorbi");
                int end = content.IndexOf("mp4", start) + 3;
                content = content.Substring(start, end - start);

                int start1 = content.LastIndexOf("http");
                int end1 = content.LastIndexOf("mp4") + 3;
                content = content.Substring(start1, end1 - start1);
                content = Server.UrlDecode(content);
                content = Server.UrlDecode(content);

                return new Uri(content).AbsoluteUri;
            }
            catch (Exception ex)
            {
            }

            return null;
        }

    
}
}
