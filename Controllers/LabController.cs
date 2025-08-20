using Dapper;
using Inventree_App.Context;
using Inventree_App.Models;
using Inventree_App.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Inventree_App.Controllers
{
    public class LabController : Controller
    {
        private readonly DatabaseHelper _dbHelper;
        private readonly ApplicationContext _context;

        public LabController(DatabaseHelper dbHelper, ApplicationContext context)
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
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string filter = "all", string search = "", string labType = "all", string status = "all")
        {
            var userName = GetCurrentUser();
            if (userName == null)
                return RedirectToAction("Index", "Lab");

            ViewBag.UserName = userName.UserName;
            ViewBag.UserImage = userName.Image;
            ViewBag.CurrentFilter = filter;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentLabType = labType;
            ViewBag.CurrentStatus = status;

            var stocks = _context.LabEquipment.AsQueryable();

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

            // Apply lab type filter
            if (labType != "all")
                stocks = stocks.Where(s => s.Type == labType);

            // Apply status filter (e.g., available, in-use, maintenance, etc.)
            if (status != "all")
                stocks = stocks.Where(s => s.Status == status);

            int totalItems = stocks.Count();

            var paginatedStocks = stocks
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(paginatedStocks);
        }

        public IActionResult Create()
        {
            var userName = GetCurrentUser();
            if (userName == null)
                return RedirectToAction("Index", "Lab");

            ViewBag.UserName = userName.UserName;
            ViewBag.UserImage = userName.Image;
            ViewData["Locations"] = new SelectList(_context.Location, "Id", "LocationName");

            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "CategoryName");
            return View("AddLabEquipment"); // Matches @model List<Stocks>           
        }
        [HttpPost]
        public IActionResult Create(LabEquipment lab)
        {
            var userName = GetCurrentUser();

            if (ModelState.IsValid)
            {
                lab.CreatedOn = DateTime.Now;
                // Add stock to database
                _context.LabEquipment.Add(lab);
                _context.SaveChanges();

                // Generate Barcode based on ID
                // lab.SeialNumber = $"STK{lab.Id:D6}";  // Example: STK000123
                //lab.Barcode = GenerateBarcodeImage(stock.SerialNumber);

                // Update stock with barcode details
                //_context.Stocks.Update(stock);
                //_context.SaveChanges();

                return RedirectToAction("Index");
            }
            return View("AddLabEquipment", lab);
        }
        [HttpPost]
        public IActionResult Edit(int id, LabEquipment updatedStock)
        {
            if (ModelState.IsValid)
            {
                var stock = _context.LabEquipment.Find(id);
                if (stock == null)
                {
                    return NotFound();
                }
                //var location = _context.Location.First(x => x.Id == stock.LocationId);
                //ViewData["Locations"] = new SelectList(_context.Location, "Id", "LocationName");
                //ViewData["Categories"] = new SelectList(_context.Categories, "Id", "CategoryName");

                // Update stock properties dynamically
                stock.Name = updatedStock.Name;
                stock.CategoryId = updatedStock.CategoryId;
                stock.Quantity = updatedStock.Quantity;
                stock.MaxQuantity = updatedStock.MaxQuantity;
                stock.CategoryId = updatedStock.CategoryId;

                _context.LabEquipment.Update(stock);
                _context.SaveChanges();

                return RedirectToAction("Index");
            }
            return RedirectToAction("Create");

        }
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var lab = _context.LabEquipment.First(x => x.Id == id);

            if (lab == null)
            {
                return NotFound();
            }
            var location = _context.LabEquipment.Remove(lab);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult GetStockById(int? id)
        {
            var lab = _context.LabEquipment.FirstOrDefault(x => x.Id == id);

            if (lab != null)
            {
                return View("AddLabEquipment", lab);
            }

            return View("Index");
        }



        //Chemaicals

        public async Task<IActionResult> Chemicals(int page = 1, int pageSize = 10, string filter = "all", string search = "")
        {
            var userName = GetCurrentUser();
            if (userName == null)
                return RedirectToAction("Index", "Home");

            ViewBag.UserName = userName.UserName;
            ViewBag.UserImage = userName.Image;
            ViewBag.CurrentFilter = filter;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            var stocks = _context.Chemicals.AsQueryable();

            // Apply stock level filtering
            //if (filter == "red")
            //    stocks = stocks.Where(s => (s.Quantity / (float)s.MaxQuantity) * 100 < 30);
            //else if (filter == "orange")
            //    stocks = stocks.Where(s => (s.Quantity / (float)s.MaxQuantity) * 100 >= 30 && (s.Quantity / (float)s.MaxQuantity) * 100 < 70);
            //else if (filter == "green")
            //    stocks = stocks.Where(s => (s.Quantity / (float)s.MaxQuantity) * 100 >= 70);

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
                stocks = stocks.Where(s => s.Name.Contains(search));

            if (filter != "all")
            {
                stocks = stocks.Where(c => c.ExpiryDate < DateTime.Now);
            }
            // Apply lab type filter
            //if (labType != "all")
            //    stocks = stocks.Where(s => s.Type == labType);

            // Apply status filter (e.g., available, in-use, maintenance, etc.)
            //if (status != "all")
            //    stocks = stocks.Where(s => s.Status == status);

            int totalItems = stocks.Count();

            var paginatedStocks = stocks
                .OrderBy(c => c.ExpiryDate) // Sorting by expiry date
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(paginatedStocks);
        }

        public IActionResult CreateChemicals()
        {
            var userName = GetCurrentUser();
            if (userName == null)
                return RedirectToAction("Index", "Home");

            ViewBag.UserName = userName.UserName;
            ViewBag.UserImage = userName.Image;
            //ViewData["Locations"] = new SelectList(_context.Location, "Id", "LocationName");

            //ViewData["Categories"] = new SelectList(_context.Categories, "Id", "CategoryName");
            return View("AddChemicals"); // Matches @model List<Stocks>           
        }
        [HttpPost]
        public IActionResult CreateChemicals(Chemicals chemicals)
        {
            var userName = GetCurrentUser();

            if (ModelState.IsValid)
            {
                // Add stock to database
                _context.Chemicals.Add(chemicals);
                _context.SaveChanges();

                // Generate Barcode based on ID
                // lab.SeialNumber = $"STK{lab.Id:D6}";  // Example: STK000123
                //lab.Barcode = GenerateBarcodeImage(stock.SerialNumber);

                // Update stock with barcode details
                //_context.Stocks.Update(stock);
                //_context.SaveChanges();

                return RedirectToAction("Chemicals");
            }
            return View("Chemicals", chemicals);
        }
        [HttpPost]
        public IActionResult EditChemicals(int id, Chemicals updatedStock)
        {
            if (ModelState.IsValid)
            {
                var stock = _context.Chemicals.Find(id);
                if (stock == null)
                {
                    return NotFound();
                }
                //var location = _context.Location.First(x => x.Id == stock.LocationId);
                //ViewData["Locations"] = new SelectList(_context.Location, "Id", "LocationName");
                //ViewData["Categories"] = new SelectList(_context.Categories, "Id", "CategoryName");

                // Update stock properties dynamically
                stock.Name = updatedStock.Name;
                stock.Quantity = updatedStock.Quantity;
                stock.GradeUsage = updatedStock.GradeUsage;
                stock.ExpiryDate = updatedStock.ExpiryDate;

                _context.Chemicals.Update(stock);
                _context.SaveChanges();

                return RedirectToAction("CreateChemicals");
            }
            return RedirectToAction("CreateChemicals");

        }
        [HttpDelete]
        public IActionResult DeleteChemicals(int id)
        {
            var lab = _context.Chemicals.First(x => x.Id == id);

            if (lab == null)
            {
                return NotFound();
            }
            var location = _context.Chemicals.Remove(lab);
            _context.SaveChanges();

            return RedirectToAction("Chemicals");
        }
        [HttpGet]
        public IActionResult GetChemicalsById(int? id)
        {
            var lab = _context.Chemicals.FirstOrDefault(x => x.Id == id);

            if (lab != null)
            {
                return View("AddChemicals", lab);
            }

            return View("Chemicals");
        }

    }
}
