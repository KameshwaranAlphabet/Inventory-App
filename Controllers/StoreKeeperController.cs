using Dapper;
using Google.Protobuf.WellKnownTypes;
using Inventree_App.Context;
using Inventree_App.Enum;
using Inventree_App.Models;
using Inventree_App.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using ZXing.Common;
using ZXing;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Inventree_App.Controllers
{
    public class StoreKeeperController : Controller
    {
        private readonly DatabaseHelper _dbHelper;
        private readonly ApplicationContext _context;
        private readonly ICustomerService _customerService;
        private readonly string _connectionString;


        public StoreKeeperController(DatabaseHelper dbHelper, ApplicationContext context,IConfiguration configuration, ICustomerService customerService)
        {
            _dbHelper = dbHelper;
            _context = context;
            _customerService = customerService;
            _connectionString = configuration.GetConnectionString("DefaultConnection"); // Get connection string from appsettings.json

        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        //public ActionResult Index()
        //{
        //    var user = GetCurrentUser();
        //    ViewBag.UserName = user.UserName;
        //    var stocks = _context.Stocks.ToList();
        //    return View(stocks);
        //}
        public IActionResult Index(string filterType, int page = 1, int pageSize = 10)
        {
            var user = GetCurrentUser();
            ViewBag.UserName = user.UserName;

            var stocks = _context.Stocks.AsQueryable();
            var lowstocks = stocks.Where(s => (s.Quantity / (float)s.MaxQuantity) * 100 < 30);
            var order = _context.Order.Where(x => x.Status == OrderStatus.Approved.ToString()).ToList();
            var orderPending = _context.Order.Where(x => x.Status == OrderStatus.Pending.ToString()).Count();
            var orderApproved = _context.Order.Where(x => x.Status == OrderStatus.Approved.ToString()).Count();
            var today = DateTime.Now.Date; // Get today's date in UTC
            var orderApprovedCount = _context.Order.Where(x => x.OrderDate.Date == today).Count();

            DateTime lastWeek = today.AddDays(-7);
            DateTime lastMonth = today.AddMonths(-1);

            IQueryable<Logs> logsQuery = _context.Logs.OrderByDescending(x=>x.CreatedDate);

            switch (filterType)
            {
                case "Today":
                    logsQuery = logsQuery.Where(log => log.CreatedDate.Value.Date == today);
                    break;
                case "Week":
                    logsQuery = logsQuery.Where(log => log.CreatedDate.Value.Date >= lastWeek);
                    break;
                case "Month":
                    logsQuery = logsQuery.Where(log => log.CreatedDate.Value.Date >= lastMonth);
                    break;
            }
            int totalLogs = logsQuery.Count();
            List<Logs> logs = logsQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.TotalLogs = totalLogs;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.FilterType = filterType;

            ViewBag.StocksCount = stocks.Count();
            ViewBag.ApprovedCount = order.Count();
            ViewBag.LowStocksCount = lowstocks.Count();
            ViewBag.AvailableStocks = stocks.Where(s => s.Quantity > 0).Count();
            ViewBag.Pending = orderPending;
            ViewBag.Approved = orderApproved;
            ViewBag.TodayCount = orderApprovedCount;

            return View(logs.ToList());
        }

        public IActionResult ApproveList(int page = 1, int pageSize = 10, string search = "", string orderDate = "", DateTime? fromDate = null, DateTime? toDate = null, string filter = "")
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

            // Order Date Sorting
            if (orderDate == "asc")
            {
                query = query.OrderBy(o => o.OrderDate);
            }
            else if (orderDate == "desc")
            {
                query = query.OrderByDescending(o => o.OrderDate);
            }

            if (filter == "Pending")
                query = query.Where(o => o.Status == "Pending");

            if (filter == "Approved")
                query = query.Where(o => o.Status == "Approved");

            if (filter == "Today")
                query = query.Where(o => o.OrderDate.Date == DateTime.Now.Date); // Always filter today's orders

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.OrderDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(a => a.OrderDate <= toDate.Value);
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


        public async Task<IActionResult> Inventory(int page = 1, int pageSize = 10, string filter = "all", string search = "")
        {
            var userName = GetCurrentUser();
            if (userName == null)
                return RedirectToAction("Index", "Home");

            ViewBag.UserName = userName.UserName;
            ViewBag.CurrentFilter = filter;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            var stocksQuery = _context.Stocks.AsQueryable();

            // Apply stock level filtering
            if (filter == "red")
                stocksQuery = stocksQuery.Where(s => (s.Quantity / (float)s.MaxQuantity) * 100 < 30);
            else if (filter == "orange")
                stocksQuery = stocksQuery.Where(s => (s.Quantity / (float)s.MaxQuantity) * 100 >= 30 && (s.Quantity / (float)s.MaxQuantity) * 100 < 70);
            else if (filter == "green")
                stocksQuery = stocksQuery.Where(s => (s.Quantity / (float)s.MaxQuantity) * 100 >= 70);
            else if (filter == "Available")
                stocksQuery = stocksQuery.Where(s => s.Quantity != 0);

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
                stocksQuery = stocksQuery.Where(s => s.Name.Contains(search));

            int totalItems = stocksQuery.Count();

            var paginatedStocks =  stocksQuery
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.Quantity,
                    a.MaxQuantity,
                    a.UnitCapacity,
                    a.UnitQuantity,
                    a.SerialNumber,
                    UnitType = _context.UnitTypes.Where(x => x.Id.ToString() == a.UnitType).Select(x => x.UnitName).FirstOrDefault(),
                    SubUnitType = _context.SubUnitTypes.Where(x => x.Id.ToString() == a.SubUnitType).Select(x => x.SubUnitName).FirstOrDefault()
                })
                .ToList();

            // Convert anonymous type to a proper model (if needed)
            var stockList = paginatedStocks.Select(a => new Stocks
            {
                Id = a.Id,
                Name = a.Name,
                Quantity = a.Quantity,
                MaxQuantity = a.MaxQuantity,
                UnitCapacity = a.UnitCapacity,
                UnitQuantity = a.UnitQuantity,
                SerialNumber = a.SerialNumber,
                UnitType = a.UnitType ?? "",  // Default value if null
                SubUnitType = a.SubUnitType ?? ""
            }).ToList();

            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View("Inventory", stockList);
        }

        /// <summary>
        /// ScanAndReduceStock reduce the stock by 1
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult ScanAndReduceStock([FromBody] BarcodeScanRequest request)
        {
            if (string.IsNullOrEmpty(request.Barcode))
            {
                return BadRequest(new { success = false, error = "Invalid barcode." });
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                string checkStockQuery = "SELECT quantity FROM stocks WHERE SerialNumber = @barcode";
                int currentStock = connection.ExecuteScalar<int>(checkStockQuery, new { barcode = request.Barcode });

                if (currentStock > 0)
                {
                    string updateQuery = "UPDATE stocks SET Quantity = Quantity - 1 WHERE SerialNumber = @barcode";
                    connection.Execute(updateQuery, new { barcode = request.Barcode });
                    var stocks = _context.Stocks.FirstOrDefault(x => x.SerialNumber == request.Barcode);
                    return Json(new
                    {
                        success = true,
                        product = new
                        {
                            Name = stocks.Name,
                            StockQuantity = stocks.Quantity,
                        }
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Stock is empty." });
                }
            }
        }
        /// <summary>
        /// Index page for Scanner
        /// </summary>
        /// <returns></returns>
        public IActionResult ScannerIndex()
        {
            var userName = GetCurrentUser();
            if (userName == null)
                return RedirectToAction("Index", "Home");

            ViewBag.UserName = userName.UserName;
            return View("Scanning");
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
        public IActionResult Create()
        {
            ViewData["Locations"] = new SelectList(_context.Location, "Id", "LocationName");

            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "CategoryName");

            ViewData["UnitTypes"] = new SelectList(_context.UnitTypes, "Id", "UnitName");

            ViewData["SubUnitTypes"] = new SelectList(_context.SubUnitTypes, "Id", "SubUnitName");

            return View("AddStock"); // Matches @model List<Stocks>           
        }

        [HttpPost]
        public IActionResult Create(Stocks stock)
        {
            var userName = GetCurrentUser();

            if (ModelState.IsValid)
            {
                stock.CreatedOn = DateTime.Now;
                stock.Email = userName.Email;
                stock.MaxQuantity = (stock.UnitCapacity * stock.UnitQuantity) + stock.Quantity;

                // Add stock to database
                _context.Stocks.Add(stock);
                _context.SaveChanges();

                // Generate Barcode based on ID
                stock.SerialNumber = $"STK{stock.Id:D6}";  // Example: STK000123
                stock.Barcode = GenerateBarcodeImage(stock.SerialNumber);

                // Update stock with barcode details
                _context.Stocks.Update(stock);
                _context.SaveChanges();

                // Log Stock Creation
                var log = new Logs
                {
                    UserID = userName.Id,
                    Type = "CreateStock",
                    Description = $"Stock {stock.SerialNumber} created by {userName.Email}",
                    CreatedDate = DateTime.Now,
                    UserName = userName.Email,
                };

                _context.Logs.Add(log);
                _context.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(stock);
        }

        private string GenerateBarcodeImage(string barcodeValue)
        {
            var writer = new BarcodeWriter<Image<Rgba32>>
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Height = 100,
                    Width = 300,
                    Margin = 10
                },
                Renderer = new ZXing.ImageSharp.Rendering.ImageSharpRenderer<Rgba32>() // Fix for the error
            };

            // Generate barcode image
            using (Image<Rgba32> image = writer.Write(barcodeValue))
            {
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "barcodes");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string barcodePath = Path.Combine(folderPath, $"{barcodeValue}.png");

                // Save barcode as PNG
                image.Save(barcodePath, new PngEncoder()); //  Cross-platform saving
                return $"/barcodes/{barcodeValue}.png"; // Path to store in DB
            }
        }
        // Delete method
        //The Delete method removes a record based on the provided id.
        // the method used to delete the stock permanetly from the stock table 
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                string query = "DELETE FROM stocks WHERE id = @id";
                connection.Execute(query, new { id });
            }

            return RedirectToAction("Inventory");
        }

        /// <summary>
        /// Get the stock by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // Get the stock by id  
        [HttpGet]
        public IActionResult GetStockById(int? id)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                List<string> columnNames = connection.Query<string>("SHOW COLUMNS FROM stocks").ToList();
                ViewBag.ColumnNames = columnNames;
                ViewData["Locations"] = new SelectList(_context.Location, "Id", "LocationName");

                ViewData["Categories"] = new SelectList(_context.Categories, "Id", "CategoryName");

                ViewData["UnitTypes"] = new SelectList(_context.UnitTypes, "Id", "UnitName");

                ViewData["SubUnitTypes"] = new SelectList(_context.SubUnitTypes, "Id", "SubUnitName");

                if (id.HasValue)
                {
                    string query = "SELECT * FROM stocks WHERE Id = @id";
                    //var stock = connection.QueryFirstOrDefault(query, new { id });
                    var stock = _context.Stocks.FirstOrDefault(x => x.Id == id);

                    if (stock == null)
                    {
                        return NotFound();
                    }

                    return View("AddStock", stock);
                }

                return View("Index");
            }
        }
        [HttpPost]
        public IActionResult Edit(int id, Stocks updatedStock)
        {
            if (ModelState.IsValid)
            {
                var stock = _context.Stocks.Find(id);
                if (stock == null)
                {
                    return NotFound();
                }

                var user = GetCurrentUser(); // Get current user details

                // Capture old values before updating
                var oldStockData = new
                {
                    stock.Name,
                    stock.LocationId,
                    stock.CategoryId,
                    stock.Quantity,
                    stock.MaxQuantity,
                    stock.UnitType,
                    stock.UnitQuantity,
                    stock.UnitCapacity,
                    stock.SubUnitType,
                };

                // Update stock properties dynamically
                stock.Name = updatedStock.Name;
                stock.LocationId = updatedStock.LocationId;
                stock.CategoryId = updatedStock.CategoryId;
                stock.Quantity = updatedStock.Quantity;
                stock.MaxQuantity = updatedStock.MaxQuantity;
                stock.UnitType = updatedStock.UnitType;
                stock.UnitQuantity = updatedStock.UnitQuantity;
                stock.UnitCapacity = updatedStock.UnitCapacity;
                stock.SubUnitType = updatedStock.SubUnitType;
                stock.MaxQuantity = updatedStock.MaxQuantity;

                _context.Stocks.Update(stock);
                _context.SaveChanges();

                // Log changes
                var logDetails = $"Stock {stock.SerialNumber} updated by {user.Email}. Changes: ";

                if (oldStockData.Name != stock.Name) logDetails += $"Name: {oldStockData.Name} ? {stock.Name}, ";
                if (oldStockData.LocationId != stock.LocationId) logDetails += $"LocationId: {oldStockData.LocationId} ? {stock.LocationId}, ";
                if (oldStockData.CategoryId != stock.CategoryId) logDetails += $"CategoryId: {oldStockData.CategoryId} ? {stock.CategoryId}, ";
                if (oldStockData.Quantity != stock.Quantity) logDetails += $"Quantity: {oldStockData.Quantity} ? {stock.Quantity}, ";
                if (oldStockData.MaxQuantity != stock.MaxQuantity) logDetails += $"MaxQuantity: {oldStockData.MaxQuantity} ? {stock.MaxQuantity}, ";

                logDetails = logDetails.TrimEnd(',', ' '); // Remove trailing comma

                var log = new Logs
                {
                    UserID = user.Id,
                    Type = "Updated",
                    Description = logDetails,
                    UserName = user.UserName,
                    CreatedDate = DateTime.Now
                };

                _context.Logs.Add(log);
                _context.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(updatedStock);
        }
        public JsonResult GetUnitTypes()
        {
            var unitTypes = _context.UnitTypes.Select(u => new { u.Id, u.UnitName }).ToList();
            return Json(unitTypes);
        }

        public JsonResult GetSubUnitTypes()
        {
            var subUnitTypes = _context.SubUnitTypes.Select(s => new { s.Id, s.SubUnitName }).ToList();
            return Json(subUnitTypes);
        }
        public IActionResult StationeryCategories(int pageNumber = 1, int pageSize = 5, string search = "")
        {
            var user = GetCurrentUser();
            ViewBag.UserName = user.UserName;

            // Base query to fetch categories and count stocks
            var categoriesQuery = _context.Categories.Select(category => new CategoryViewModel
            {
                Id = category.Id,
                Name = category.CategoryName,
                CreatedOn = category.CreatedOn,
                ParentId = category.ParentCategoryId,
                Count = _context.Stocks.Count(x => x.CategoryId == category.Id)
            });

            // Apply search filter if search term is provided
            if (!string.IsNullOrEmpty(search))
            {
                categoriesQuery = categoriesQuery.Where(c => c.Name.Contains(search)); // Search by category name
            }

            // Get total count for pagination
            int totalRecords = categoriesQuery.Count();

            // Apply pagination
            var categories = categoriesQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Pass pagination info and data to the view
            ViewBag.TotalRecords = totalRecords;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchTerm = search;  // Keep the search term to retain in the input field

            // Return the view with filtered and paginated categories
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
        public IActionResult DeleteCategory(int id)
        {
            var category = _context.Categories.FirstOrDefault(c => c.Id == id);
            if (category != null)
            {
                _context.Remove(category);
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

        public IActionResult StationeryLocation(int pageNumber = 1, int pageSize = 5, string search = "")
        {
            var user = GetCurrentUser();
            ViewBag.UserName = user.UserName;

            // Base query to get categories and stock counts
            var categoriesQuery = _context.Location.Select(category => new LocationViewModel
            {
                Id = category.Id,
                Name = category.LocationName,
                Count = _context.Stocks.Count(x => x.LocationId == category.Id)
            });

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(search))
            {
                categoriesQuery = categoriesQuery.Where(s => s.Name.Contains(search));
            }

            // Get total count for pagination
            int totalRecords = categoriesQuery.Count();

            // Apply pagination
            var categories = categoriesQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Pass pagination info and data to the view
            ViewBag.TotalRecords = totalRecords;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;

            // Return the view with paginated categories
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
                _context.Remove(category);
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
                    var totalStockQuantity = (stock1.UnitCapacity * stock1.UnitQuantity) + stock1.Quantity; // Total available stock

                    if (model.Quantity > totalStockQuantity)
                    {
                        TempData["ErrorMessage"] = "Insufficient stock available!";
                        return RedirectToAction("CreateManual");
                    }

                    if (model.Quantity <= totalStockQuantity)
                    {
                        // Calculate how many full packs to deduct
                        int? fullPacksToDeduct = model.Quantity / stock1.UnitCapacity; // Full packs
                        int? remainingPieces = model.Quantity % stock1.UnitCapacity;   // Leftover pieces

                        // Deduct from packs
                        stock1.UnitQuantity -= fullPacksToDeduct;

                        // Deduct from loose pieces
                        stock1.Quantity -= remainingPieces;

                        // Ensure we don't have negative pack counts
                        if (stock1.UnitQuantity < 0) stock1.UnitQuantity = 0;

                        // Ensure loose pieces are correctly adjusted
                        if (stock1.Quantity < 0)
                        {
                            stock1.UnitQuantity--; // Deduct 1 more pack
                            stock1.Quantity += stock1.UnitCapacity; // Convert a pack into pieces
                        }

                        // Ensure stock is not negative
                        if (stock1.UnitQuantity < 0) stock1.UnitQuantity = 0;
                        if (stock1.Quantity < 0) stock1.Quantity = 0;
                    }
                }
                var newPurchase = new Logs
                {
                    UserID = user.Id,
                    UserName = model.CustomerName,
                    CreatedDate = DateTime.Now,
                    Description = $"{model.StockName} qty {model.Quantity} as Picked by {model.CustomerName} Given by {user.UserName} ",
                    Type = "Completed",
                };
                TempData["SuccessMessage"] = "Stock successfully deducted!";
                _context.Logs.Add(newPurchase);
                _context.SaveChanges();
                return RedirectToAction("CreateManual");
            }
            TempData["ErrorMessage"] = "Invalid request!";
            return View(model);
        }
        public IActionResult CreateManual()
        {
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
        public IActionResult SubUnitTypes(int pageNumber = 1, int pageSize = 5, string search ="")
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