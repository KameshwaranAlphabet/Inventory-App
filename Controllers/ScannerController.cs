using Dapper;
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
    public class ScannerController : Controller
    {
        private readonly DatabaseHelper _dbHelper;
        private readonly string _connectionString;
        private readonly ApplicationContext _context;

        public ScannerController(DatabaseHelper dbHelper,IConfiguration configuration, ApplicationContext context)
        {
            _dbHelper = dbHelper;
            _connectionString = configuration.GetConnectionString("DefaultConnection"); // Get connection string from appsettings.json
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
        /// <summary>
        /// Index page for Scanner
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var user = GetCurrentUser();
            ViewBag.UserName = user.UserName;
            ViewBag.UserImage = user.Image;
            return View("Index");
        }

        /// <summary>
        /// ScanAndReduceStock reduce the stock by 1
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult ScanAndReduceStockaa([FromBody] BarcodeScanRequest request)
        {
            var user = GetCurrentUser();
            ViewBag.UserName = user.UserName;
            ViewBag.UserImage = user.Image;
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
                    var stocks = _context.Stocks.FirstOrDefault(x=>x.SerialNumber == request.Barcode);
                    return Json(new
                    {
                        success = true,
                        product = new
                        {
                            Name = stocks.Name,
                            StockQuantity = stocks.Quantity,
                            SerialNumber = stocks.SerialNumber
                        }
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Stock is empty." });
                }
            }
        }
        [HttpPost]
        public IActionResult ScanAndReduceStock([FromBody] BarcodeScanRequest request)
        {
            var user = GetCurrentUser();
            ViewBag.UserName = user?.UserName;

            if (string.IsNullOrEmpty(request.Barcode))
            {
                return BadRequest(new { success = false, error = "Invalid barcode." });
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                // Fetch current stock details
                string checkStockQuery = "SELECT UnitQuantity, UnitCapacity, Quantity FROM stocks WHERE SerialNumber = @barcode";
                var stock = connection.QueryFirstOrDefault<Stocks>(checkStockQuery, new { barcode = request.Barcode });

                if (stock == null)
                {
                    return Json(new { success = false, message = "Stock not found." });
                }

                int? totalAvailablePieces = (stock.UnitCapacity * stock.UnitQuantity) + stock.Quantity;

                if (1 > totalAvailablePieces)
                {
                    return Json(new { success = false, message = "Not enough stock available." });
                }

                // Calculate new stock levels after deduction
                int? remainingQuantity = totalAvailablePieces - 1;
                int? newPackQuantity = remainingQuantity / stock.UnitCapacity;
                int? newPieceQuantity = remainingQuantity % stock.UnitCapacity;

                // Update database with new stock values
                string updateQuery = @"
            UPDATE stocks 
            SET UnitQuantity = @newPackQuantity, Quantity = @newPieceQuantity 
            WHERE SerialNumber = @barcode";

                connection.Execute(updateQuery, new
                {
                    newPackQuantity,
                    newPieceQuantity,
                    barcode = request.Barcode
                });

                // Fetch updated stock details
                var updatedStock = connection.QueryFirstOrDefault<Stocks>(checkStockQuery, new { barcode = request.Barcode });

                return Json(new
                {
                    success = true,
                    product = new
                    {
                        StockName = updatedStock?.Name,                   
                        StockQuantity = (updatedStock?.UnitQuantity*updatedStock?.UnitCapacity) + updatedStock?.Quantity,
                        SerialNumber = request.Barcode
                    }
                });
            }
        }

    }
}