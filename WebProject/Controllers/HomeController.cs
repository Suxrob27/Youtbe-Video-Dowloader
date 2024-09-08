using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebProject.Models;

namespace WebProject.Controllers
{
    public class HomeController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }


   
    }
}
