using Inventree_App.Context;
using Inventree_App.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.Entity;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Inventree_App.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationContext _context;
        public HomeController(ApplicationContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Authenticate(string email, string password)
        {
            if (email == "admin@example.com" && password == "password123")
            {
                return RedirectToAction("Index", "Inventory");
            }
            else
            {
                ViewBag.Error = "Invalid email or password.";
                return View("Index");
            }
        }
        //[HttpPost]
        //public async Task<IActionResult> Authenticate(string email, string password)
        //{

        //    // Find user by email
        //    var user = _context.Customer.FirstOrDefault(u => u.Email ==email);
        //    if (user == null || !VerifyPassword(password, user.Password))
        //    {
        //        ViewBag.Error = "Invalid email or password.";
        //        return View(user);
        //    }

        //    // Authentication success - Redirect to Inventory
        //    return RedirectToAction("Index", "Inventory");
        //}

        private bool VerifyPassword(string enteredPassword, string storedHash)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(enteredPassword));
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            return hashString == storedHash;
        }

        //[HttpPost]
        //public IActionResult Register(string firstname,string Lastname, string username, string fullName, string email, string password, string confirmPassword)
        //{
        //    if (password != confirmPassword)
        //    {
        //        ViewBag.Error = "Passwords do not match!";
        //        return View("SignUp");
        //    }

        //    // Save user data (Add database logic here)
        //    ViewBag.SuccessMessage = "Account created successfully! Please log in.";
        //    return RedirectToAction("Index");
        //}
        [HttpPost]
        public IActionResult Register(string firstName, string lastName, string username, string email, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match!");
                return View("SignUp");
            }

            if (_context.Customer.Any(u => u.Email == email))
            {
                ModelState.AddModelError("Email", "Email already in use!");
                return View("SignUp");
            }

            string hashedPassword = HashPassword(password);

            var user = new Customer
            {
                FirstName = firstName,
                LastName = lastName,
                UserName = username,
                Email = email,
                Password = hashedPassword,
                CreatedOn = DateTime.Now
            };

            _context.Customer.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Index", "Customer");
        }
        public IActionResult Register()
        {
            return View("SignUp");
        }
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        // Logout method to clear session
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}
