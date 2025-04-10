using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Inventree_App.Models;
using Inventree_App.Context;
using System.Threading.Tasks;
using System.Data.Entity;
using Inventree_App.Enum;
using System.IdentityModel.Tokens.Jwt;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
        var lowstocks = stocks.Where(s => ((s.UnitCapacity * s.UnitQuantity + s.Quantity) / (float)s.MaxQuantity) * 100 < 30);
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
        ViewBag.AvailableStocks = stocks.Where(s => (s.UnitCapacity * s.UnitQuantity + s.Quantity) > 0).Count();
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

    [HttpGet]
    public IActionResult GetOrders(string month, string searchUser = "", int page = 1, int pageSize = 5)
    {
        var ordersQuery = from o in _context.Order
                          join oi in _context.OrderItem on o.Id equals oi.OrderId
                          group oi by new { o.Id, o.UserId, o.OrderDate } into g
                          select new
                          {
                              OrderId = g.Key.Id,
                              UserId = g.Key.UserId,
                              OrderDate = g.Key.OrderDate,
                              TotalQuantity = g.Sum(x => x.Quantity),
                              ItemCount = g.Count(x => x.Quantity > 0)
                          };

        // Filter by month if selected
        if (month != "all")
        {
            ordersQuery = ordersQuery.Where(o => o.OrderDate.Month.ToString() == month);
        }

        var ordersList = ordersQuery.OrderByDescending(x => x.TotalQuantity).AsEnumerable().ToList();
        var result = new List<DashBoardOrder>();

        foreach (var order in ordersList)
        {
            var user = _context.Customer.FirstOrDefault(x => x.Id == order.UserId);
            if (user != null) // Ensure user is not null
            {
                string firstInitial = !string.IsNullOrEmpty(user.FirstName) ? user.FirstName[0].ToString() : "";
                string lastInitial = !string.IsNullOrEmpty(user.LastName) ? user.LastName[^1].ToString() : ""; // ^1 gets the last character
                string fullName = user.UserName.ToLower(); // Convert to lowercase for case-insensitive search

                result.Add(new DashBoardOrder
                {
                    Profile = user.UserName[0].ToString().ToUpper(),
                    Name = user.UserName,
                    Date = order.OrderDate,
                    ItemCount = order.ItemCount,
                    Quantity = order.TotalQuantity,
                    OrderId = order.OrderId
                });
            }
        }

        // Apply user name filter (case-insensitive)
        if (!string.IsNullOrEmpty(searchUser))
        {
            result = result.Where(x => x.Name.ToLower().Contains(searchUser.ToLower())).ToList();
        }

        // Apply pagination on filtered data
        int totalRecords = result.Count;
        var paginatedOrders = result.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Json(new { data = paginatedOrders, totalRecords, pageSize });
    }

    [HttpGet]
    public IActionResult GetLogs(string month = "all", string searchUser = "", int page = 1, int pageSize = 5)
    {
        var query = _context.Logs
                    .AsNoTracking() // Improves read performance by disabling tracking
            .OrderByDescending(x => x.CreatedDate)
            .AsQueryable();

        // Filter by month if selected
        if (!string.IsNullOrEmpty(month) && month != "all")
        {
            if (int.TryParse(month, out int monthValue) && monthValue >= 1 && monthValue <= 12)
            {
                query = query.Where(o => o.CreatedDate.HasValue && o.CreatedDate.Value.Month == monthValue);
            }
        }

        // Filter by username if provided
        if (!string.IsNullOrEmpty(searchUser))
        {
            query = query.Where(o => o.UserName.Contains(searchUser));
        }

        // Get total record count before pagination
        int totalRecords = query.Count();

        // Apply pagination
        var paginatedOrders = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Json(new { data = paginatedOrders, totalRecords, pageSize });
    }

}
