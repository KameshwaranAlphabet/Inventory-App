using Dapper;
using Inventree_App.Context;
using Inventree_App.Models;
using Inventree_App.Service;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.IdentityModel.Tokens.Jwt;
using ZXing;
using ZXing.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Drawing.Printing;
using System.Reflection.Metadata;
using System.Xml.Linq;
using iTextSharp.text.pdf;
using iTextSharp.text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using Image = iTextSharp.text.Image;
using Document = iTextSharp.text.Document;
using OfficeOpenXml;
using static iTextSharp.text.pdf.AcroFields;


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

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string filter = "all", string search = "")
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
                stocksQuery = stocksQuery.Where(s => ((s.UnitCapacity * s.UnitQuantity + s.Quantity) / (float)s.MaxQuantity) * 100 < 30);
            else if (filter == "orange")
                stocksQuery = stocksQuery.Where(s => ((s.UnitCapacity * s.UnitQuantity + s.Quantity) / (float)s.MaxQuantity) * 100 >= 30 && ((s.UnitCapacity * s.UnitQuantity + s.Quantity) / (float)s.MaxQuantity) * 100 < 70);
            else if (filter == "green")
                stocksQuery = stocksQuery.Where(s => ((s.UnitCapacity * s.UnitQuantity + s.Quantity) / (float)s.MaxQuantity) * 100 >= 70);
            else if (filter == "Available")
                stocksQuery = stocksQuery.Where(s => (s.UnitCapacity * s.UnitQuantity + s.Quantity) != 0);

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
                stocksQuery = stocksQuery.Where(s => s.Name.Contains(search));

            int totalItems = await stocksQuery.CountAsync();

            var paginatedStocks = await stocksQuery
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
                .ToListAsync();

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

            return View(stockList);
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
            var message = _dbHelper.AddColumn(ColumnName, DataType);
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

            ViewData["UnitTypes"] = new SelectList(_context.UnitTypes, "Id", "UnitName");

            ViewData["SubUnitTypes"] = new SelectList(_context.SubUnitTypes, "Id", "SubUnitName");


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
            stock.MaxQuantity = (stock.UnitCapacity * stock.UnitQuantity) + stock.Quantity;

            if (ModelState.IsValid)
            {
                stock.CreatedOn = DateTime.Now;
                stock.Email = userName.Email;
                //stock.UnitType = _context.UnitTypes.Where(x => x.Id == int.Parse(stock.UnitType)).Select(x => x.UnitName).First();
                //stock.SubUnitType = _context.SubUnitTypes.Where(x => x.Id == int.Parse(stock.SubUnitType)).Select(x => x.SubUnitName).First();
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

                bool changesMade = false;
                var logDetails = $"Stock {stock.SerialNumber} updated by {user.Email}. Changes: ";

                if (oldStockData.Name != updatedStock.Name)
                {
                    logDetails += $"Name: {oldStockData.Name} → {updatedStock.Name}, ";
                    stock.Name = updatedStock.Name;
                    changesMade = true;
                }

                if (oldStockData.LocationId != updatedStock.LocationId)
                {
                    logDetails += $"LocationId: {oldStockData.LocationId} → {updatedStock.LocationId}, ";
                    stock.LocationId = updatedStock.LocationId;
                    changesMade = true;
                }

                if (oldStockData.CategoryId != updatedStock.CategoryId)
                {
                    logDetails += $"CategoryId: {oldStockData.CategoryId} → {updatedStock.CategoryId}, ";
                    stock.CategoryId = updatedStock.CategoryId;
                    changesMade = true;
                }

                if (oldStockData.UnitQuantity != updatedStock.UnitQuantity ||
                    oldStockData.UnitCapacity != updatedStock.UnitCapacity ||
                    oldStockData.Quantity != updatedStock.Quantity)
                {

                    var newMax = ((updatedStock.UnitCapacity * updatedStock.UnitQuantity) + updatedStock.Quantity);
                    
                    //var newMax = (updatedStock.UnitCapacity * updatedStock.UnitQuantity) + updatedStock.Quantity;
                    if (oldStockData.MaxQuantity != newMax)
                    {

                        logDetails += $"MaxQuantity: {oldStockData.MaxQuantity} → {newMax}, ";
                        stock.MaxQuantity = updatedStock.MaxQuantity = (newMax == null || newMax <= 0) ? updatedStock.UnitQuantity : (updatedStock.UnitCapacity * updatedStock.UnitQuantity) + updatedStock.Quantity ;
                        changesMade = true;
                    }
                }

                if (oldStockData.UnitQuantity != updatedStock.UnitQuantity)
                {
                    logDetails += $"UnitQuantity: {oldStockData.UnitQuantity} → {updatedStock.UnitQuantity}, ";
                    stock.UnitQuantity = updatedStock.UnitQuantity;
                    changesMade = true;
                }

                if (oldStockData.UnitCapacity != updatedStock.UnitCapacity)
                {
                    logDetails += $"UnitCapacity: {oldStockData.UnitCapacity} → {updatedStock.UnitCapacity}, ";
                    stock.UnitCapacity = updatedStock.UnitCapacity;
                    changesMade = true;
                }

                if (oldStockData.SubUnitType != updatedStock.SubUnitType)
                {
                    logDetails += $"SubUnitType: {oldStockData.SubUnitType} → {updatedStock.SubUnitType}, ";
                    stock.SubUnitType = updatedStock.SubUnitType;
                    changesMade = true;
                }

                if (oldStockData.Quantity != updatedStock.Quantity)
                {
                    logDetails += $"Quantity: {oldStockData.Quantity} → {updatedStock.Quantity}, ";
                    stock.Quantity = updatedStock.Quantity;
                    changesMade = true;
                }

                if (oldStockData.UnitType != updatedStock.UnitType)
                {
                    logDetails += $"UnitType: {oldStockData.UnitType} → {updatedStock.UnitType}, ";
                    stock.UnitType = updatedStock.UnitType;
                    changesMade = true;
                }

                if (changesMade)
                {
                    logDetails = logDetails.TrimEnd(',', ' '); // Clean up trailing comma

                    _context.Stocks.Update(stock);
                    _context.SaveChanges();

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
                }

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
            var userName = GetCurrentUser();
            ViewBag.UserName = userName.UserName;

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
        public IActionResult DownloadPdf()
        {
            var stocks = _context.Stocks.ToList();

            //if (filter == "low")
            //{
            //    stocks = stocks.Where(s => (s.Quantity / (float)s.MaxQuantity) * 100 < 30).ToList();
            //}

            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 20f, 20f, 20f, 20f);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();

                // Title
                Paragraph title = new Paragraph("Stock Barcode List", new Font(Font.FontFamily.HELVETICA, 18f, Font.BOLD))
                {
                    Alignment = Element.ALIGN_CENTER
                };
                document.Add(title);
                document.Add(new Paragraph("\n"));

                foreach (var stock in stocks)
                {
                    // Add Company Label
                    Paragraph companyLabel = new Paragraph("aLphabet school", new Font(Font.FontFamily.HELVETICA, 12f, Font.BOLD))
                    {
                        Alignment = Element.ALIGN_CENTER
                    };
                    document.Add(companyLabel);

                    // Add Barcode Image from Path
                    if (!string.IsNullOrEmpty(stock.Barcode))
                    {
                        //string barcodeImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Barcodes", stock.Barcode);
                        string baseUrl = $"{Request.Scheme}://{Request.Host}";
                        string barcodeImagePath = baseUrl + stock.Barcode;
                        if ((barcodeImagePath!=""))
                        {
                            Image barcodeImage = Image.GetInstance(barcodeImagePath);
                            barcodeImage.ScaleAbsolute(180f, 60f); // Adjust size
                            barcodeImage.Alignment = Element.ALIGN_CENTER;
                            document.Add(barcodeImage);
                        }
                    }

                    // Add Serial Number Below Barcode
                    Paragraph serialNumber = new Paragraph(stock.SerialNumber, new Font(Font.FontFamily.HELVETICA, 14f, Font.BOLD))
                    {
                        Alignment = Element.ALIGN_CENTER
                    };
                    document.Add(serialNumber);

                    document.Add(new Paragraph("\n")); // Spacing
                }

                document.Close();
                return File(ms.ToArray(), "application/pdf", "StockBarcodes.pdf");
            }
        }
        private int? ParseNullableInt(string? input)
        {
            if (int.TryParse(input?.Trim(), out int result))
            {
                return result;
            }
            return null; // Leave it null if parsing fails
        }

        [HttpPost]
        public async Task<IActionResult> UploadCsv(IFormFile file)
        {
            var userName = GetCurrentUser();

            if (file == null || file.Length == 0)
            {
                TempData["Message"] = "Please select a CSV file.";
                return RedirectToAction("Index");
            }

            // Validate file extension
            var fileExtension = Path.GetExtension(file.FileName);
            if (fileExtension.ToLower() != ".csv")
            {
                TempData["Message"] = "Invalid file format. Please upload a CSV file.";
                return RedirectToAction("Index");
            }

            try
            {
                using (var stream = new StreamReader(file.OpenReadStream()))
                {
                    var stationeryList = new List<Stocks>();
                    bool isFirstRow = true;

                    while (!stream.EndOfStream)
                    {
                        var line = await stream.ReadLineAsync();
                        if (isFirstRow) // Skip header row
                        {
                            isFirstRow = false;
                            continue;
                        }

                        var values = line.Split(',');

                        if (values.Length == 15) // Ensure correct columns count
                        {
                            var stationery = new Stocks
                            {
                                Name = values[1].Trim(),
                                SerialNumber = values[2].Trim(),
                                Quantity = ParseNullableInt(values[3]),
                                MaxQuantity = ParseNullableInt(values[4]),
                                Email = userName.Email,
                                Barcode = values[7].Trim(),
                                CategoryId = ParseNullableInt(values[10]),
                                CreatedOn = DateTime.Now,
                                LocationId = ParseNullableInt(values[9]),
                                UpdatedOn = DateTime.Now,
                                SubUnitType = values[14].Trim(),
                                UnitCapacity = ParseNullableInt(values[13]),
                                UnitQuantity = ParseNullableInt(values[12]),
                                UnitType = values[11].Trim()
                            };

                            stationeryList.Add(stationery);
                        }
                    }

                    if (stationeryList.Count > 0)
                    {
                        _context.Stocks.AddRange(stationeryList);
                        await _context.SaveChangesAsync(); // Save initially to generate IDs

                        // Update Serial Numbers and Barcodes in bulk
                        stationeryList.ForEach(stock =>
                        {
                            stock.SerialNumber = $"STK{stock.Id:D6}"; // Format: STK000123
                            stock.Barcode = GenerateBarcodeImage(stock.SerialNumber);
                            var test = ((stock.UnitCapacity * stock.UnitQuantity) + stock.Quantity);
                            stock.MaxQuantity = (test == null || test <= 0) ? stock.UnitQuantity : stock.Quantity;
                        });

                        _context.Stocks.UpdateRange(stationeryList); // Bulk update
                        await _context.SaveChangesAsync(); // Save changes again
                    }
                }

                TempData["Message"] = "CSV uploaded successfully!";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "An error occurred: " + ex.Message;
            }

            return RedirectToAction("Index");
        }



        //public async Task<IActionResult> DownloadBarcodePdf()
        //{
        //    var stocks = await _context.Stocks.ToListAsync();

        //    using (var memoryStream = new MemoryStream())
        //    {
        //        using (var writer = new PdfWriter(memoryStream))
        //        {
        //            var pdf = new PdfDocument(writer);
        //            var document = new Document(pdf);

        //            document.Add(new Paragraph("Stock Barcode List").SetBold().SetFontSize(16));

        //            foreach (var stock in stocks)
        //            {
        //                var barcodeImage = stock.Barcode;

        //                if (barcodeImage != null)
        //                {
        //                    Image image = new Image(ImageDataFactory.Create(barcodeImage));
        //                    document.Add(new Paragraph($"Property of ABC Company").SetFontSize(10));
        //                    document.Add(image.SetWidth(150)); // Adjust barcode size
        //                    document.Add(new Paragraph(stock.SerialNumber).SetFontSize(12).SetBold());
        //                    document.Add(new Paragraph("\n")); // Add spacing
        //                }
        //            }

        //            document.Close();
        //        }

        //        return File(memoryStream.ToArray(), "application/pdf", "StockBarcodes.pdf");
        //    }
        //}

        public IActionResult ExportToExcel()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var inventoryData = GetRecentInventoryData(); // Fetch recent data

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Inventory Backup");
                worksheet.Cells.LoadFromCollection(inventoryData, true);

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string fileName = "InventoryBackup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
        public IActionResult BackupList()
        {
            string backupFolder = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
            Directory.CreateDirectory(backupFolder);
            var files = Directory.GetFiles(backupFolder, "*.xlsx")
                                 .Select(f => new BackupFile
                                 {
                                     FileName = Path.GetFileName(f),
                                     FilePath = "/Backups/" + Path.GetFileName(f),
                                     CreatedDate = System.IO.File.GetCreationTime(f)
                                 })
                                 .OrderByDescending(f => f.CreatedDate)
                                 .ToList();
            return View(files);
        }

        [HttpPost]
        public IActionResult TriggerExport()
        {
            return RedirectToAction("ExportToExcel");
        }
        private List<Stocks> GetRecentInventoryData()
        {
            // Replace with actual database fetching logic
            return _context.Stocks.ToList();
        
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

    }

}
