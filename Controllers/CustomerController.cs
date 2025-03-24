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
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Customer customer)
        {
            if (id != customer.Id)
            {
                return BadRequest();
            }

            var existingCustomer = _context.Customer.Find(id);
            if (existingCustomer == null)
            {
                return NotFound();
            }

            existingCustomer.UserName = customer.UserName;
            existingCustomer.Email = customer.Email;
            existingCustomer.LastName = customer.LastName;
            existingCustomer.FirstName = customer.FirstName;

            _context.Update(existingCustomer);
            _context.SaveChanges();

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