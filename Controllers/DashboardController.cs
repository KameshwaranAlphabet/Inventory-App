using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Inventree_App.Models;
using Inventree_App.Context;
using System.Threading.Tasks;
using System.Data.Entity;
using Inventree_App.Enum;
using System.IdentityModel.Tokens.Jwt;

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
    //public async Task<IActionResult> Index()
    //{
    //    var stocks = _context.Stocks.AsQueryable();
    //    var lowstocks = stocks.Where(s => (s.Quantity / (float)s.MaxQuantity) * 100 < 30);

    //    ViewBag.StocksCount = stocks.Count();
    //    ViewBag.LowStocksCount = lowstocks.Count();
    //    ViewBag.AvailableStocks = stocks.Where(s => s.Quantity > 0).Count();
    //    return View();
    //}
    public IActionResult Index(string filterType = "All", string statusFilter = "All", int page = 1, int pageSize = 10)
    {
        var user = GetCurrentUser();
        ViewBag.UserName = user.UserName;

        var stocks = _context.Stocks.AsQueryable();
        var lowstocks = stocks.Where(s => (s.Quantity / (float)s.MaxQuantity) * 100 < 30);
        var order = _context.Order.Where(x => x.Status == OrderStatus.Approved.ToString()).ToList();
        var orderPending = _context.Order.Where(x => x.Status == OrderStatus.Pending.ToString()).ToList();

        DateTime today = DateTime.Today;
        DateTime lastWeek = today.AddDays(-7);
        DateTime lastMonth = today.AddMonths(-1);

        // Start with all logs by default
        IQueryable<Logs> logsQuery = _context.Logs.OrderByDescending(x => x.CreatedDate);

        // Apply Date Filter only if not "All"
        if (filterType != "All")
        {
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
        }

        // Apply Status Filter only if not "All"
        if (statusFilter != "All")
        {
            logsQuery = logsQuery.Where(log => log.Type == statusFilter);
        }

        // Pagination
        int totalLogs = logsQuery.Count();
        List<Logs> logs = logsQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        // ViewBag Data
        ViewBag.TotalLogs = totalLogs;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.FilterType = filterType;
        ViewBag.StatusFilter = statusFilter;

        ViewBag.StocksCount = stocks.Count();
        ViewBag.ApprovedCount = order.Count();
        ViewBag.LowStocksCount = lowstocks.Count();
        ViewBag.AvailableStocks = stocks.Where(s => s.Quantity > 0).Count();
        ViewBag.Pending = orderPending.Count();

        return View(logs);
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
