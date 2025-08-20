using Inventree_App.Context;
using Inventree_App.Models;
using Inventree_App.Service;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace Inventree_App.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationContext _context;

        public CustomerController(ApplicationContext context)
        {
            _context = context;
        }
        private Customer GetCurrentUser()
        {
            var token = Request.Cookies["jwt"]; // Get JWT token from cookies

            if (string.IsNullOrEmpty(token))
                return null; // No token means user is not logged in

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var userName = jwtToken.Claims.First(c => c.Type == "sub")?.Value;
            var user = _context.Customer.FirstOrDefault(x => x.Email == userName);
            return user; // Return username from token
        }
        // List Customers
        public IActionResult Index()
        {
            var user = GetCurrentUser();
            ViewBag.UserName = user.UserName;
            ViewBag.UserImage = user.Image;

            var customers = _context.Customer.ToList();
            return View("Index",customers);
        }

        // GET: Edit Customer
        public IActionResult Edit(int id)
        {
            var customer = _context.Customer.Find(id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // POST: Edit Customer
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Customer customer, IFormFile? imageFile)
        {
            if (id != customer.Id)
            {
                return BadRequest();
            }

            var existingCustomer = await _context.Customer.FindAsync(id);
            if (existingCustomer == null)
            {
                return NotFound();
            }

            // Update text fields
            existingCustomer.UserName = customer.UserName;
            existingCustomer.LastName = customer.LastName;
            existingCustomer.FirstName = customer.FirstName;

            // Handle image upload
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Save the relative path
                existingCustomer.Image = "/Uploads/" + fileName;
            }

            _context.Update(existingCustomer);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }


        // GET: Delete Customer Confirmation
        public IActionResult Delete(int id)
        {
            var customer = _context.Customer.Find(id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }


        public IActionResult DeleteConfirmed(int id)
        {
            var customer = _context.Customer.Find(id);
            if (customer == null)
            {
                return NotFound();
            }

            _context.Customer.Remove(customer);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

    }
}