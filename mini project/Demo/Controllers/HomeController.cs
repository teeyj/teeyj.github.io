using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers
{
    public class HomeController : Controller
    {
        private readonly DB db;

        public HomeController(DB db)
        {
            this.db = db;
        }

        // GET: Home/Index
        public IActionResult Index()
        {
            var courts = db.Courses.ToList();

            return View(courts);
        }

        public IActionResult AboutWe()
        {
            return View();
        }
    }
}