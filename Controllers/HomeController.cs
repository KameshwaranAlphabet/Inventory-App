using Microsoft.AspNetCore.Mvc;

namespace Inventree_App.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Authenticate(string email, string password)
        {
            if (email == "admin@example.com" && password == "password123")
            {
                return RedirectToAction("Index", "Dashboard");
            }
            else
            {
                ViewBag.Error = "Invalid email or password.";
                return View("Index");
            }
        }
    }
}
