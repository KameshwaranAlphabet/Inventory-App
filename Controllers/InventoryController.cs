using Dapper;
using Inventree_App.Context;
using Inventree_App.Models;
using Inventree_App.Service;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
        //public IActionResult Index()
        //{
        //    var inventory = _context.Stocks.ToList();
        //    return View(inventory);
        //}
        public IActionResult Index()
        {
            var stocks = _context.Stocks.ToList();
            return View("Index", stocks);
        }
        //public IActionResult Index()
        //{
        //    //if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
        //    //{
        //    //    TempData["SessionExpired"] = "Your session has expired. Please log in again.";
        //    //    return RedirectToAction("Index", "Auth");
        //    //}

        //    return View();
        //}
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(Stocks stock)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _context.Stocks.Add(stock);
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction("Index"); // Redirect to listing page after adding stock
        //    }
        //    return View(stock);
        //}
        public IActionResult form()
        {
            var stocks = _context.Stocks.ToList();
            return View("Index", stocks);
        }

        public IActionResult ManageColumns()
        {
            List<Stocks> columns = _dbHelper.GetCustomColumns();
            return View(columns);
        }

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
        public IActionResult Create()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                string query = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'stocks' AND TABLE_SCHEMA = DATABASE();";
                var columns = connection.Query<string>(query).ToList();
                ViewBag.ColumnNames = columns; // Pass columns separately
                                               // Get stock data
                var stocks = _context.Stocks.ToList();
                return View("AddStock", stocks); // Matches @model List<Stocks>
            }
        }

        [HttpPost]
        public IActionResult Create(IFormCollection form)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                var columnNames = form.Keys.Where(k => k != "__RequestVerificationToken").ToList();
                var values = columnNames.Select(k => form[k].ToString()).ToList();

                string columns = string.Join(", ", columnNames.Select(c => $"`{c}`"));
                string parameters = string.Join(", ", columnNames.Select(c => $"@{c}"));

                string query = $"INSERT INTO stocks ({columns}) VALUES ({parameters})";

                var parametersDict = columnNames.ToDictionary(c => $"@{c}", c => (object)form[c]);

                connection.Execute(query, parametersDict);
            }

            return RedirectToAction("Index");
        }

    }
}
