using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Inventree_App.Models;
using Inventree_App.Context;
using System.Threading.Tasks;
using System.Data.Entity;

namespace Inventree_App.Controllers;

public class DashboardController : Controller
{
    private readonly ILogger<DashboardController> _logger;
    private readonly ApplicationContext _context;

    public DashboardController(ILogger<DashboardController> logger , ApplicationContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var stocks =  _context.Stocks.AsQueryable();
        var lowstocks = stocks.Where(s => (s.Quantity / (float)s.MaxQuantity) * 100 < 30);

        ViewBag.StocksCount = stocks.Count();
        ViewBag.LowStocksCount = lowstocks.Count();
        ViewBag.AvailableStocks = stocks.Where(s => s.Quantity > 0).Count();
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
