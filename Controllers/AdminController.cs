using Inventree_App.Context;
using Inventree_App.Models;
using Inventree_App.Service;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using static iTextSharp.text.pdf.AcroFields;

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
        public IActionResult Index(int orderId,int page = 1, int pageSize = 10, string search = "", string orderDate = "", string filter ="")
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

            if(orderId > 0)
                query = query.Where(o => o.Id == orderId);

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
        public IActionResult StationeryCategories(int pageNumber = 1, int pageSize = 5)
        {
            var user = GetCurrentUser();
            ViewBag.UserName = user.UserName;

            var categoriesQuery = _context.Categories.Select(category => new CategoryViewModel
            {
                Id = category.Id,
                Name = category.CategoryName,
                CreatedOn = category.CreatedOn,
                ParentId = category.ParentCategoryId,
                Count = _context.Stocks.Count(x => x.CategoryId == category.Id)
            });

            // Get total count for pagination
            int totalRecords = categoriesQuery.Count();

            // Apply pagination
            var categories = categoriesQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalRecords = totalRecords;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;

            return View("StationeryCategories", categories);
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
                _context.Categories.Remove(category);
                _context.SaveChanges();
            }
            return RedirectToAction("StationeryCategories");
        }

        //public IActionResult StationeryLocation()
        //{
        //    var user = GetCurrentUser();
        //    ViewBag.UserName = user.UserName;
        //    var locations = _context.Location.ToList();

        //    var model = new List<LocationViewModel>();
        //    foreach (var location in locations)
        //    {
        //        var count = _context.Stocks.Count(x => x.LocationId == location.Id);

        //        model.Add(new LocationViewModel
        //        {
        //            Id = location.Id,
        //            Name = location.LocationName,
        //            CreatedOn = DateTime.Now,
        //            Count = count
        //        });
        //    }
        //    return View("Location", model);
        //}

        public IActionResult StationeryLocation(int pageNumber = 1, int pageSize = 5)
        {
            var user = GetCurrentUser();
            ViewBag.UserName = user.UserName;

            var categoriesQuery = _context.Location.Select(category => new LocationViewModel
            {
                Id = category.Id,
                Name = category.LocationName,
                Count = _context.Stocks.Count(x => x.LocationId == category.Id)
            });

            // Get total count for pagination
            int totalRecords = categoriesQuery.Count();

            // Apply pagination
            var categories = categoriesQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalRecords = totalRecords;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;

            return View("Location", categories);
        }

        [HttpPost]
        public IActionResult CreateOrUpdateLocation(Location model)
        {
            if (ModelState.IsValid)
            {
                if (model.Id == 0) // Create new category
                {
                    _context.Location.Add(model);
                }
                else // Update existing category
                {
                    var existingCategory = _context.Location.Find(model.Id);
                    if (existingCategory != null)
                    {
                        existingCategory.LocationName = model.LocationName;
                        _context.Location.Update(existingCategory);
                    }
                }
                _context.SaveChanges();
                return RedirectToAction("StationeryLocation");
            }

            return RedirectToAction("StationeryLocation");
        }
        [HttpGet]
        public IActionResult DeleteLocation(int id)
        {
            var category = _context.Location.FirstOrDefault(c => c.Id == id);
            if (category != null)
            {
                _context.Location.Remove(category);
                _context.SaveChanges();
            }
            return RedirectToAction("StationeryLocation");
        }

        // API to search customers
        [HttpGet]
        public JsonResult SearchCustomers(string term)
       {
            //var result = _context.Customer.Where(c => c.UserName.Contains(term.ToLower())).ToList();
            var customers = _context.Customer
                            .Where(c => c.UserName.Contains(term))
                            .Select(c => new { name = c.UserName })
                            .ToList();
            return Json(customers);
        }

        // API to search stocks
        [HttpGet]
        public JsonResult SearchStocks(string term)
        {
            //var result = _context.Stocks.Where(s => s.Name.Contains(term.ToLower())).ToList();
            var stocks = _context.Stocks
                           .Where(s => s.Name.Contains(term))
                           .Select(s => new { stockName = s.Name })
                           .ToList();

            return Json(stocks);
        }

        [HttpPost]
        public IActionResult CreateManual(ManualStockPage model)
        {
            var user = GetCurrentUser();

            if (ModelState.IsValid)
            {
                // Find the stock entry for the item
                var stock1 = _context.Stocks.FirstOrDefault(s => s.Name == model.StockName);

                if (stock1 != null)
                {
                    var totalStockQuantity = (stock1.UnitCapacity * stock1.UnitQuantity) + stock1.Quantity; // Total available in pieces

                    if (model.Quantity > totalStockQuantity)
                    {
                        TempData["ErrorMessage"] = "Insufficient stock available!";
                        return RedirectToAction("CreateManual");
                    }

                    int? requestedQty = model.Quantity;
                    int? remainingQty = requestedQty;

                    // First use available loose pieces
                    if (stock1.Quantity >= remainingQty)
                    {
                        stock1.Quantity -= remainingQty;
                        remainingQty = 0;
                    }
                    else
                    {
                        remainingQty -= stock1.Quantity;
                        stock1.Quantity = 0;

                        // Use full packs (converted to pieces)
                        int? totalPiecesFromPacks = stock1.UnitQuantity * stock1.UnitCapacity;

                        if (remainingQty <= totalPiecesFromPacks)
                        {
                            int? fullPacksNeeded = remainingQty / stock1.UnitCapacity;
                            int? leftoverPieces = remainingQty % stock1.UnitCapacity;

                            stock1.UnitQuantity -= fullPacksNeeded;
                            stock1.Quantity -= leftoverPieces;

                            // If not enough loose pieces, convert one more pack
                            if (stock1.Quantity < 0)
                            {
                                if (stock1.UnitQuantity > 0)
                                {
                                    stock1.UnitQuantity -= 1;
                                    stock1.Quantity += stock1.UnitCapacity;
                                }
                                else
                                {
                                    stock1.Quantity = 0;
                                }
                            }
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Insufficient stock available!";
                            return RedirectToAction("CreateManual");
                        }
                    }

                    // Final safety checks
                    if (stock1.UnitQuantity < 0) stock1.UnitQuantity = 0;
                    if (stock1.Quantity < 0) stock1.Quantity = 0;

                    _context.Stocks.Update(stock1);
                    _context.SaveChanges();

                    // Log the transaction
                    var newPurchase = new Logs
                    {
                        UserID = user.Id,
                        UserName = model.CustomerName,
                        CreatedDate = DateTime.Now,
                        Description = $"{model.StockName} qty {model.Quantity} as Picked by {model.CustomerName} Given by {user.UserName}",
                        Type = "Completed",
                    };

                    _context.Logs.Add(newPurchase);
                    _context.SaveChanges();

                    TempData["SuccessMessage"] = "Stock successfully deducted!";
                    return RedirectToAction("CreateManual");
                }

                TempData["ErrorMessage"] = "Stock item not found!";
                return RedirectToAction("CreateManual");
            }

            TempData["ErrorMessage"] = "Invalid request!";
            return View(model);
        }

        public IActionResult CreateManual()
        {
            var user = GetCurrentUser();
            ViewBag.UserName = user.UserName;   
            return View("ManualEntry");
        }
        public IActionResult UnitTypes(int pageNumber = 1, int pageSize = 5, string search = "")
        {
            var user = GetCurrentUser();
            ViewBag.UserName = user.UserName;

            // Start with base query
            var query = _context.UnitTypes.AsQueryable();

            // Apply search filter if searchTerm is provided
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.UnitName.Contains(search));
            }

            // Get total count after filtering
            int totalRecords = query.Count();

            // Apply pagination
            var categories = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Pass data to View
            ViewBag.TotalRecords = totalRecords;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchTerm = search; // To retain search value in input field

            return View("AddUnits", categories);
        }

        [HttpPost]
        public IActionResult CreateOrUpdateUnitTypes(UnitTypes model)
        {

            if (model.Id == 0) // Create new category
            {
                _context.UnitTypes.Add(model);
            }
            else // Update existing category
            {
                var existingCategory = _context.UnitTypes.Find(model.Id);
                if (existingCategory != null)
                {
                    existingCategory.UnitName = model.UnitName;
                    _context.UnitTypes.Update(existingCategory);
                }
            }
            _context.SaveChanges();
            return RedirectToAction("UnitTypes");
        }
        [HttpGet]
        public IActionResult DeleteUnits(int id)
        {
            var category = _context.UnitTypes.FirstOrDefault(c => c.Id == id);
            if (category != null)
            {
                _context.UnitTypes.Remove(category);
                _context.SaveChanges();
            }
            return RedirectToAction("UnitTypes");
        }
        public IActionResult SubUnitTypes(int pageNumber = 1, int pageSize = 5, string search = "")
        {
            var user = GetCurrentUser();
            ViewBag.UserName = user.UserName;

            // Start with base query
            var query = _context.SubUnitTypes.AsQueryable();

            // Apply search filter if searchTerm is provided
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.SubUnitName.Contains(search));
            }

            // Get total count after filtering
            int totalRecords = query.Count();

            // Apply pagination
            var categories = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Pass data to View
            ViewBag.TotalRecords = totalRecords;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchTerm = search; // To retain search value in input field

            return View("AddSubUnits", categories);
        }


        [HttpPost]
        public IActionResult CreateOrUpdateSubUnitTypes(SubUnitTypes model)
        {

            if (model.Id == 0) // Create new category
            {
                _context.SubUnitTypes.Add(model);
            }
            else // Update existing category
            {
                var existingCategory = _context.SubUnitTypes.Find(model.Id);
                if (existingCategory != null)
                {
                    existingCategory.SubUnitName = model.SubUnitName;
                    _context.SubUnitTypes.Update(existingCategory);
                }
            }
            _context.SaveChanges();
            return RedirectToAction("SubUnitTypes");
        }
        [HttpGet]
        public IActionResult DeleteSubUnits(int id)
        {
            var category = _context.SubUnitTypes.FirstOrDefault(c => c.Id == id);
            if (category != null)
            {
                _context.SubUnitTypes.Remove(category);
                _context.SaveChanges();
            }
            return RedirectToAction("SubUnitTypes");
        }
    }
}
