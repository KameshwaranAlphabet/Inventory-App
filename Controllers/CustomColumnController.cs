using Inventree_App.Context;
using Inventree_App.Models;
using Inventree_App.Service;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Linq;

namespace Inventree_App.Controllers
{
    public class CustomColumnController : Controller
    {
        private readonly DatabaseHelper _dbHelper;

        public CustomColumnController(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public IActionResult ManageColumns()
        {
            List<string> columns = _dbHelper.GetColumns();
            return View(columns);
        }

        [HttpPost]
        public IActionResult AddColumn(string ColumnName, string DataType)
        {
            _dbHelper.AddColumn(ColumnName, DataType);
            return RedirectToAction("Form");
        }

        public IActionResult Form()
        {
            List<string> columns = _dbHelper.GetColumns();
            return View("Index",columns);
        }
    }
}