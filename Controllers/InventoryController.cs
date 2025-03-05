using Inventree_App.Context;
using Inventree_App.Models;
using Microsoft.AspNetCore.Mvc;

namespace Inventree_App.Controllers
{
    public class InventoryController : Controller
    {
        //private readonly ApplicationContext _context;

        //public InventoryController(ApplicationContext context)
        //{
        //    _context = context;
        //}
        //public IActionResult Index()
        //{
        //    var inventory = _context.InventoryItems.ToList();
        //    return View(inventory);
        //}
        public IActionResult Index()
        {
            var inventory = new List<InventoryItem>
            {
                new InventoryItem { Id = 1, Name = "Pencil", Quantity = 4, MaxQuantity = 10 },
                new InventoryItem { Id = 2, Name = "Notebook", Quantity = 7, MaxQuantity = 15 },
                new InventoryItem { Id = 3, Name = "Eraser", Quantity = 9, MaxQuantity = 12 }
            };

            return View(inventory);
        }


    }
}
