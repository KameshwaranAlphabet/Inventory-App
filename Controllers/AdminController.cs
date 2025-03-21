using Inventree_App.Context;
using Inventree_App.Models;
using Inventree_App.Service;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;

namespace Inventree_App.Controllers
{
    public class AdminController : Controller
    {
        private readonly DatabaseHelper _dbHelper;
        private readonly ApplicationContext _context;

        public AdminController(DatabaseHelper dbHelper, ApplicationContext context)
        {
            _dbHelper = dbHelper;
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
        //public IActionResult Index()
        //{
        //    var stocks = _context.Order.ToList();
        //    return View(stocks);
        //}
        public IActionResult Index(int page = 1, int pageSize = 10, string search = "", string orderDate = "", string filter ="")
        {
            var user = GetCurrentUser();
            ViewBag.UserName = user.UserName;

            var query = _context.Order.AsQueryable();

            // Search Filter (By Customer Name or Order ID)
            if (!string.IsNullOrEmpty(search))
            {
                var customerIds = _context.Customer
                    .Where(x => x.UserName.Contains(search))
                    .Select(x => x.Id)
                    .ToList();

                query = query.Where(x => customerIds.Contains(x.UserId));
            }

            if(filter == "Pending")
                query = query.Where(o => o.Status == "Pending");

            // Order Date Sorting
            if (orderDate == "asc")
            {
                query = query.OrderBy(o => o.OrderDate);
            }
            else if (orderDate == "desc")
            {
                query = query.OrderByDescending(o => o.OrderDate);
            }

            var totalOrders = query.Count();

            // Pagination
            var orders = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var orderList = orders.Select(order => new OrderDetailsModel
            {
                OrderId = order.Id,
                OrderedDate = order.OrderDate,
                CustomerName = _context.Customer.FirstOrDefault(c => c.Id == order.UserId)?.UserName,
                ItemsCount = order.ItemsCount,
                Status = order.Status,
                Items = _context.OrderItem.Where(x => x.OrderId == order.Id).ToList()
            }).ToList();

            // Storing Pagination Data in ViewBag
            ViewBag.TotalItems = totalOrders;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalOrders / pageSize);
            ViewBag.Search = search;
            ViewBag.OrderDate = orderDate;

            return View(orderList);
        }
        public IActionResult StationeryCategories()
        {
            var user = GetCurrentUser();
            ViewBag.UserName = user.UserName;
            var categories = _context.Categories.ToList();

            var model = new List<CategoryViewModel>();
            foreach (var category in categories)
            {
                var count = _context.Stocks.Count(x => x.CategoryId == category.Id);
                 
                model.Add(new CategoryViewModel
                {
                    Id = category.Id,
                    Name = category.CategoryName,
                    CreatedOn = category.CreatedOn,
                    ParentId = category.ParentCategoryId,
                    Count = count
                });
            }
            return View("StationeryCategories", model);
        }
        [HttpPost]
        public IActionResult CreateOrUpdate(Categories model)
        {
            if (ModelState.IsValid)
            {
                if (model.Id == 0) // Create new category
                {
                    model.CreatedOn = DateTime.Now;
                    _context.Categories.Add(model);
                }
                else // Update existing category
                {
                    var existingCategory = _context.Categories.Find(model.Id);
                    if (existingCategory != null)
                    {
                        existingCategory.CategoryName = model.CategoryName;
                        _context.Categories.Update(existingCategory);
                    }
                }
                _context.SaveChanges();
                return RedirectToAction("StationeryCategories");
            }

            return RedirectToAction("StationeryCategories");
        }


        [HttpPost]
        public IActionResult Edit(Categories model)
        {
            var category = _context.Categories.FirstOrDefault(c => c.Id == model.Id);
            if (category == null) return NotFound();
            category.CategoryName = model.CategoryName;

            _context.Update(category);
            _context.SaveChanges();
            return RedirectToAction("StationeryCategories");
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var category = _context.Categories.FirstOrDefault(c => c.Id == id);
            if (category != null)
            {
                _context.Remove(category);
                _context.SaveChanges();
            }
            return RedirectToAction("StationeryCategories");
        }
    }
}
