using Dapper;
using Inventree_App.Context;
using Inventree_App.Models;
using Inventree_App.Service;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Linq;

namespace Inventree_App.Controllers
{
    public class ScannerController : Controller
    {
        private readonly DatabaseHelper _dbHelper;
        private readonly string _connectionString;

        public ScannerController(DatabaseHelper dbHelper,IConfiguration configuration)
        {
            _dbHelper = dbHelper;
            _connectionString = configuration.GetConnectionString("DefaultConnection"); // Get connection string from appsettings.json
        }

        /// <summary>
        /// Index page for Scanner
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View("Index");
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

                    return Ok(new { success = true, remainingStock = currentStock - 1 });
                }
                else
                {
                    return BadRequest(new { success = false, error = "Stock not available or already empty." });
                }
            }
        }
    }
}