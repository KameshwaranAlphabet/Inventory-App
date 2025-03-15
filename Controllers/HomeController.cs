using Inventree_App.Context;
using Inventree_App.Models;
using Inventree_App.Service;
using Microsoft.AspNetCore.Mvc;
using System.Data.Entity;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;


namespace Inventree_App.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly ICustomerService _customerService;
        public HomeController(ApplicationContext context, ICustomerService customerService)
        {
            _context = context;
            _customerService = customerService;
        }
        public IActionResult Index()
        {
            return View();
        }

        //[HttpPost]
        //public IActionResult Authenticate(string email, string password)
        //{
        //    if (email == "admin@example.com" && password == "password123")
        //    {
        //        return RedirectToAction("Index", "Inventory");
        //    }
        //    else
        //    {
        //        ViewBag.Error = "Invalid email or password.";
        //        return View("Index");
        //    }
        //}
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
        public IActionResult Register(string firstName, string lastName, string username, string email, string password, string confirmPassword,string roles)
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
                CreatedOn = DateTime.Now,
                UserRoles = roles
            };

            _context.Customer.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Index", "Customer");
        }
       
        [HttpPost]
        public async Task<IActionResult> Authenticate(string email, string password)
        {
            if (email!=null)
            {
                // Authenticate user and sign in
                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email)
            };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            }
            var user = _context.Customer.FirstOrDefault(u => u.Email == email);

            var token = _customerService.GenerateJwtToken(user);
            Response.Cookies.Append("jwt", token);

            if(user!=null && user.UserRoles == "Faculty")
                return RedirectToAction("OrderList", "Order");

            if (user == null || user.Password != HashPassword(password))
            {
                ViewBag.Error = "Invalid login credentials!";
                return View("Index");
            }

            return RedirectToAction("Index", "Dashboard");
        }

        public IActionResult Register()
        {
            return View("SignUp");
        }
        /// <summary>
        /// HashPassword
        /// </summary>
        /// <param name="password"></param>
        /// <returns>encrypted password</returns>
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
        [HttpGet]
        public IActionResult Logout()
        {
            // Remove JWT cookie
            Response.Cookies.Delete("jwt");

            // Redirect to login page
            return RedirectToAction("Index", "Home");
        }
    }
}
