using Dapper;
using Inventree_App.Context;
using Inventree_App.Models;
using Inventree_App.Service;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using System.Drawing;
using System.Drawing.Imaging;
using System.IdentityModel.Tokens.Jwt;
using ZXing;
using ZXing.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inventree_App.Controllers
{
    public class InventoryController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly DatabaseHelper _dbHelper;
        private readonly string _connectionString;


        public InventoryController(DatabaseHelper helper, ApplicationContext context, IConfiguration connectionString)
        {
            _dbHelper = helper;
            _context = context;
            _connectionString = connectionString.GetConnectionString("DefaultConnection"); // Get connection string from appsettings.json
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
        /// <summary>
        /// 
        /// </summary>
        /// <returns>return the list of sctocks to inventory overview</returns>
        //public IActionResult Index()
        //{
        //    var userName = GetCurrentUserName();
        //    if(userName== "Guest")
        //        return RedirectToAction("Index", "Home");

        //    ViewBag.UserName = userName;
        //    var stocks = _context.Stocks.ToList();
        //    return View("Index", stocks);
        //}

        public async Task<IActionResult> Index(int page = 1, int pageSize = 7, string filter = "all", string search = "")
        {
            var userName = GetCurrentUser();
            if (userName == null)
                return RedirectToAction("Index", "Home");

            ViewBag.UserName = userName.UserName;
            ViewBag.CurrentFilter = filter;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            var stocks = _context.Stocks.AsQueryable();

            // Apply stock level filtering
            if (filter == "red")
                stocks = stocks.Where(s => (s.Quantity / (float)s.MaxQuantity) * 100 < 30);
            else if (filter == "orange")
                stocks = stocks.Where(s => (s.Quantity / (float)s.MaxQuantity) * 100 >= 30 && (s.Quantity / (float)s.MaxQuantity) * 100 < 70);
            else if (filter == "green")
                stocks = stocks.Where(s => (s.Quantity / (float)s.MaxQuantity) * 100 >= 70);

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
                stocks = stocks.Where(s => s.Name.Contains(search));

            int totalItems =  stocks.Count();

            var paginatedStocks = stocks
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(paginatedStocks);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IActionResult ManageColumns()
        {
            List<Stocks> columns = _dbHelper.GetCustomColumns();
            return View(columns);
        }

        /// <summary>
        /// Add the column to the stock table
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="DataType"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult AddColumn(string ColumnName, string DataType)
        {
            var message =   _dbHelper.AddColumn(ColumnName, DataType);
            if (message.Contains("successfully"))
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }
            return RedirectToAction("Create");
        }

        public IActionResult Forms()
        {
            List<Stocks> columns = _dbHelper.GetCustomColumns();
            return View("Index", columns);
        }

        /// <summary>
        /// Get all the columns from stock table
        /// </summary>
        /// <returns></returns>
        // Get All the columns from stock table
        public IActionResult Create()
        {
            ViewData["Locations"] = new SelectList(_context.Location, "Id", "LocationName");

            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "CategoryName");
            return View("AddStock"); // Matches @model List<Stocks>           
        }

        /// <summary>
        /// Create the stock Dynamically
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        // The Create method dynamically inserts all form fields except the hidden __RequestVerificationToken.

        //[HttpPost]
        //public IActionResult Create(IFormCollection form)
        //{
        //    using (var connection = new MySqlConnection(_connectionString))
        //    {
        //        var columnNames = form.Keys.Where(k => k != "__RequestVerificationToken").ToList();
        //        var values = columnNames.Select(k => form[k].ToString()).ToList();

        //        string columns = string.Join(", ", columnNames.Select(c => $"`{c}`"));
        //        string parameters = string.Join(", ", columnNames.Select(c => $"@{c}"));

        //        string query = $"INSERT INTO stocks ({columns}) VALUES ({parameters})";

        //        var parametersDict = columnNames.ToDictionary(c => $"@{c}", c => (object)form[c]);

        //        connection.Execute(query, parametersDict);
        //    }

        //    return RedirectToAction("Index");
        //}
        //[HttpPost]
        //public IActionResult Create(IFormCollection form)
        //{
        //    using (var connection = new MySqlConnection(_connectionString))
        //    {
        //        var columnNames = form.Keys.Where(k => k != "__RequestVerificationToken").ToList();
        //        var values = columnNames.Select(k => form[k].ToString()).ToList();

        //        string columns = string.Join(", ", columnNames.Select(c => $"`{c}`"));
        //        string parameters = string.Join(", ", columnNames.Select(c => $"@{c}"));

        //        string insertQuery = $"INSERT INTO stocks ({columns}) VALUES ({parameters}); SELECT LAST_INSERT_ID();";

        //        var parametersDict = columnNames.ToDictionary(c => $"@{c}", c => (object)values[columnNames.IndexOf(c)]);

        //        int stockId = connection.ExecuteScalar<int>(insertQuery, parametersDict);

        //        if (stockId > 0 && columnNames.Contains("barcode")) // Check if "barcode" column exists
        //        {
        //            // Generate barcode based on Stock ID
        //            string barcodeValue = $"STK{stockId:D6}"; // Example: STK000123
        //            string barcodeImagePath = GenerateBarcodeImage(barcodeValue);

        //            // Update stock with barcode and image path
        //            string updateQuery = "UPDATE stocks SET SerialNumber = @barcode, barcode = @barcodeImage WHERE id = @id";
        //            connection.Execute(updateQuery, new { barcode = barcodeValue, barcodeImage = barcodeImagePath, id = stockId });
        //        }
        //    }

        //    return RedirectToAction("Index");
        //}

        [HttpPost]
        public IActionResult Create(Stocks stock)
        {
            var userName = GetCurrentUser();

            if (ModelState.IsValid)
            {
                stock.CreatedOn = DateTime.Now;
                stock.Email = userName.Email;
                // Add stock to database
                _context.Stocks.Add(stock);
                _context.SaveChanges();

                // Generate Barcode based on ID
                stock.SerialNumber = $"STK{stock.Id:D6}";  // Example: STK000123
                stock.Barcode = GenerateBarcodeImage(stock.SerialNumber);

                // Update stock with barcode details
                _context.Stocks.Update(stock);
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
        /// <summary>
        /// Scan the barcode
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult ScanBarcode([FromBody] string request)
        {
            using (var connection = new MySqlConnection(_connectionString)) 
            {
                string query = "UPDATE stocks SET quantity = quantity - 1 WHERE barcode = @barcode AND quantity > 0";

                int affectedRows = connection.Execute(query, new { barcode = request });

                if (affectedRows > 0)
                {
                    return Json(new { success = true, message = "Stock updated successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Stock not available or barcode not found." });
                }
            }
        }
        /// <summary>
        /// Edit the stock by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="form"></param>
        /// <returns></returns>
        // The Edit method dynamically updates all form fields except the hidden __RequestVerificationToken.
        //// this function will edit the stocks 
        //[HttpPost]
        //public IActionResult Edit(int id, IFormCollection form)
        //{

        //    ViewData["Locations"] = new SelectList(_context.Location, "Id", "LocationName");

        //    ViewData["Categories"] = new SelectList(_context.Categories, "Id", "CategoryName");

        //    using (var connection = new MySqlConnection(_connectionString))
        //    {
        //        var columnNames = form.Keys.Where(k => k != "__RequestVerificationToken").ToList();
        //        var updateSet = string.Join(", ", columnNames.Select(c => $"`{c}` = @{c}"));

        //        string query = $"UPDATE stocks SET {updateSet} WHERE id = @id";

        //        var parametersDict = columnNames.ToDictionary(c => $"@{c}", c => (object)form[c]);
        //        parametersDict["@id"] = id;

        //        connection.Execute(query, parametersDict);
        //    }

        //    return RedirectToAction("Index");
        //}

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
                var location = _context.Location.First(x => x.Id == stock.LocationId);
                ViewData["Locations"] = new SelectList(_context.Location, "Id", "LocationName");
                ViewData["Categories"] = new SelectList(_context.Categories, "Id", "CategoryName");

                // Update stock properties dynamically
                stock.Name = updatedStock.Name;
                stock.LocationId = updatedStock.LocationId;
                stock.CategoryId = updatedStock.CategoryId;
                stock.Quantity = updatedStock.Quantity;
                stock.MaxQuantity = updatedStock.MaxQuantity;
                stock.LocationId = updatedStock.LocationId;
                stock.CategoryId = updatedStock.CategoryId;

                _context.Stocks.Update(stock);
                _context.SaveChanges();

                return RedirectToAction("Index");
            }

         
            return View(updatedStock);
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

            return RedirectToAction("Index");
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
    }
}
