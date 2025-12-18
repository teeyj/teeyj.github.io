using Microsoft.AspNetCore.Mvc;

namespace Demo.Models
{
    public class RecaptchaService : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
