using Inventree_App.Context;
using Inventree_App.Enum;
using Inventree_App.Models;
using Inventree_App.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Utilities;
using System.Collections.Generic;
using System.Data.Entity;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Inventree_App.Controllers
{
    public class OrderController : Controller
    {
        private readonly DatabaseHelper _dbHelper;
        private readonly ApplicationContext _context;
        private readonly ICustomerService _customerService;

        public OrderController(DatabaseHelper dbHelper, ApplicationContext context, ICustomerService customerService)
        {
            _dbHelper = dbHelper;
            _context = context;
            _customerService = customerService;
        }

        public List<StockViewModel> GetFilteredProducts(string search, int? locationId, int page, int pageSize)
        {
            var query = _context.Stocks
                .Join(_context.Location, p => p.LocationId, l => l.Id,
                      (p, l) => new StockViewModel
                      {
                          ID = p.Id,
                          Name = p.Name,
                          StockQuantity = p.Quantity,
                          LocationId = p.LocationId,
                          StockLocation = l.LocationName
                      }).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search));
            }

            if (locationId.HasValue)
            {
                query = query.Where(p => p.LocationId == locationId.Value);
            }

            return query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        //public ActionResult Index()
        //{
        //    var user = GetCurrentUser();
        //    ViewBag.UserName = user.UserName;
        //    var stocks = _context.Stocks.ToList();
        //    return View(stocks);
        //}
        public IActionResult Index(string search, int? locationId, int page = 1, int pageSize = 10)
        {
            var user = GetCurrentUser();
            ViewBag.UserName = user.UserName;
            ViewData["Locations"] = new SelectList(_context.Categories, "Id", "CategoryName");
            var products = GetFilteredProducts(search, locationId, page, pageSize);
            return View(products);
        }
        /// <summary>
        /// get the current user cart item 
        /// </summary>
        /// <returns></returns>
        public IActionResult CartItem()
        {
            var user = GetCurrentUser();
            ViewBag.UserName = user.UserName;
            var stocks = _context.CartItem.Where(x=>x.UserId == user.Id && x.Quantity > 0).ToList();
            return View("Cart",stocks);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stockId"></param>
        /// <returns></returns>
        [HttpDelete]
        public IActionResult RemoveFromCart([FromBody] CartRemoveRequest request)
        {
            var user = GetCurrentUser();
            ViewBag.UserName = user.UserName;
            var cart = _context.CartItem.FirstOrDefault(x => x.UserId == user.Id && x.StockId == request.StockId && x.Quantity > 0);
            if (cart!=null)
            {
                    _context.CartItem.Remove(cart);
                _context.SaveChanges();
                    return Json(new { success = true, message = "Item removed from cart" });
            }
            return Json(new { success = false, message = "Item not found in cart" });
        }
        [HttpPost]
        public IActionResult PlaceOrder()
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Get all cart items for the user
            var cartItems = _context.CartItem.Where(c => c.UserId == user.Id).ToList();

            if (!cartItems.Any())
            {
                return Json(new { success = false, message = "Cart is empty" });
            }

            // Create and save Order first
            var newOrder = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.Now,
                ItemsCount = cartItems.Count(),
                Status = OrderStatus.Pending.ToString()
            };

            _context.Order.Add(newOrder);
            _context.SaveChanges(); // Save first to generate OrderId

            // Create OrderItems with correct OrderId
            var orderItems = cartItems.Select(item => new OrderItem
            {
                OrderId = newOrder.Id, // Correctly map OrderId
                StockId = item.StockId,
                Quantity = item.Quantity,
                StockName = item.StockName
            }).ToList();

            _context.OrderItem.AddRange(orderItems);

            // Remove items from cart after placing order
            _context.CartItem.RemoveRange(cartItems);
            _context.SaveChanges();

            return Json(new { success = true, message = "Order placed successfully", orderId = newOrder.Id });
        }

        /// <summary>
        /// Add the item to cart
        /// </summary>
        /// <param name="stockId"></param>
        /// <param name="quantity"></param>
        /// <returns>return the message of the of cart item added</returns>
        [HttpPost]
        public IActionResult AddToCart([FromBody] CartRequest request)
        {
            var user = GetCurrentUser();
            var test =   int.TryParse(request.StockId, out int stockId);
            //custom stock
            if (request != null && !test && request.Quantity > 0)
            {
                var existingItem = _context.CartItem.FirstOrDefault(c => c.StockName == request.StockId && c.UserId == user.Id);
                if (existingItem != null)
                {
                    existingItem.Quantity += request.Quantity;
                    _context.CartItem.Update(existingItem);
                }
                else
                {
                    var cart = new CartItem()
                    {
                        UserId = user.Id,
                        Quantity = request.Quantity,
                        StockId = 0,
                        StockName = request.StockId
                    };
                    _context.CartItem.Add(cart);
                }
                _context.SaveChanges();
                return Json(new { success = true, message = "Item Added to Cart" });
            }

            // Validate and convert StockId from string to int
            if (request == null || !int.TryParse(request.StockId, out int stockIdInt) || request.Quantity <= 0)
            {
                return Json(new { success = false, message = "Invalid stock ID or quantity" });
            }

            var stock = _context.Stocks.FirstOrDefault(s => s.Id == stockIdInt);
            if (stock != null && stock.Quantity >= request.Quantity)
            {
                var existingItem = _context.CartItem.FirstOrDefault(c => c.StockId == stockIdInt && c.UserId == user.Id);
                if (existingItem != null)
                {
                    existingItem.Quantity += request.Quantity;
                    _context.CartItem.Update(existingItem);
                }
                else
                {
                    var cart = new CartItem()
                    {
                        UserId = user.Id,
                        Quantity = request.Quantity,
                        StockId = stockIdInt,
                        StockName = stock.Name
                    };
                    _context.CartItem.Add(cart);
                }

                _context.SaveChanges();
                return Json(new { success = true, message = "Item Added to Cart" });
            }

            return Json(new { success = false, message = "Insufficient stock" });
        }
        /// <summary>
        /// Get OrderList of current user
        /// </summary>
        /// <param name="status"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns>list of order</returns>
        public IActionResult OrderList(string status, DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 5)
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            ViewBag.UserName = user.UserName;
            // Fetch orders for the current user
            var ordersQuery = _context.Order
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == status);
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate <= endDate.Value);
            }

            // Pagination
            int totalOrders = ordersQuery.Count();
            // Pagination
            var orders = ordersQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var orderList = ordersQuery.Select(order => new OrderDetailsModel
            {
                OrderId = order.Id,
                OrderedDate = order.OrderDate,
                CustomerName = order!=null ? _context.Customer.FirstOrDefault(c => c.Id == order.UserId).UserName : "",
                ItemsCount = order.ItemsCount,
                Status = order.Status,
                Items = _context.OrderItem.Where(x => x.OrderId == order.Id).ToList()

            }).ToList();

            ViewBag.StatusFilter = status;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalOrders / pageSize);

            return View("OrderList", orderList);
        }
        /// <summary>
        /// Get OrderDetails by orderIs
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public async Task<IActionResult> OrderDetail(int orderId)
        {
            var user = GetCurrentUser();
            ViewBag.UserName = user.UserName;
            var orderItem =  _context.OrderItem.Where(x=>x.OrderId == orderId).ToList();

            if (orderItem == null)
            {
                return NotFound();
            }

            //var viewModel = new Order
            //{
            //    Id = order.Id,
            //    OrderDate = order.OrderDate,
            //    ItemsCount = order,
            //    Status = order.Status,
            //    OrderItems = order.OrderItems.Select(oi => new OrderItem
            //    {
            //        ProductName = oi.Product.Name,
            //        Quantity = oi.Quantity,
            //        UnitPrice = oi.Price
            //    }).ToList()
            //};

            return View("OrderDetails", orderItem);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult UpdateOrderAndItems([FromBody] OrderAndItemsUpdateRequest request)
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            ViewBag.UserName = user.UserName;

            if ((request.OrderIds == null || request.OrderIds.Count == 0) &&
                (request.OrderItemIds == null || request.OrderItemIds.Count == 0))
            {
                return Json(new { success = false, message = "No orders or items selected." });
            }

            // Update Orders
            if (request.OrderIds != null && request.OrderIds.Count > 0)
            {
                var ordersToUpdate = _context.Order.Where(o => request.OrderIds.Contains(o.Id)).ToList();
                foreach (var order in ordersToUpdate)
                {
                    order.Status = request.Status;
                }
                _context.Order.UpdateRange(ordersToUpdate);
            }
            var logs = new List<Logs>(); // List to store log entries

            // Update Order Items
            if (request.OrderItemIds != null && request.OrderItemIds.Count > 0)
            {
                var itemsToUpdate = _context.OrderItem.Where(i => request.OrderItemIds.Contains(i.Id)).ToList();
                foreach (var item in itemsToUpdate)
                {
                    item.Status = request.Status;
                    var order = _context.Order.FirstOrDefault(x => x.Id == item.OrderId);
                    var userName = _context.Customer.FirstOrDefault(x => x.Id == order.UserId);
                    // If status is "Completed", log it
                    if (request.Status == OrderStatus.Approved.ToString())
                    {
                        logs.Add(new Logs
                        {
                            UserID = user.Id,
                            UserName = user.UserName,
                            Description = $"{item.StockName} qty {item.Quantity} was Approved by {user.UserName} for {userName.FirstName} ",
                            CreatedDate = DateTime.UtcNow
                        });
                    }
                }
                _context.OrderItem.UpdateRange(itemsToUpdate);
            }
            _context.AddRange(logs);
            _context.SaveChanges();

            return Json(new { success = true, message = $"Selected orders and items marked as {request.Status}." });
        }

        [HttpPost]
        public IActionResult UpdateOrderAndItemsStorekeeper([FromBody] OrderAndItemsUpdateRequest request)
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            ViewBag.UserName = user.UserName;
            if ((request.OrderIds == null || request.OrderIds.Count == 0) &&
                (request.OrderItemIds == null || request.OrderItemIds.Count == 0))
            {
                return Json(new { success = false, message = "No orders or items selected." });
            }

            //// Update Orders
            //if (request.OrderIds != null && request.OrderIds.Count > 0)
            //{
            //    var ordersToUpdate = _context.Order.Where(o => request.OrderIds.Contains(o.Id)).ToList();
            //    foreach (var order in ordersToUpdate)
            //    {
            //        order.Status = request.Status;
            //    }
            //    _context.Order.UpdateRange(ordersToUpdate);
            //}
            var logs = new List<Logs>(); // List to store log entries

            // Update Order Items
            if (request.OrderItemIds != null && request.OrderItemIds.Count > 0)
            {
                var itemsToUpdate = _context.OrderItem.Where(i => request.OrderItemIds.Contains(i.Id)).ToList();
                foreach (var item in itemsToUpdate)
                {
                    item.State = request.Status;

                  var order = _context.Order.FirstOrDefault(x => x.Id == item.OrderId);
                    var userName = _context.Customer.FirstOrDefault(x => x.Id == order.UserId); 
                    // If status is "Completed", log it
                    if (request.Status == "Completed")
                    {
                        logs.Add(new Logs
                        {
                            UserID = user.Id,
                            UserName = user.UserName,
                            Description = $"{item.StockName} qty {item.Quantity} as Pickuped by {userName.UserName} Given by {user.UserName} ",
                            CreatedDate = DateTime.UtcNow
                        });
                    }
                }
                _context.OrderItem.UpdateRange(itemsToUpdate);
            }
            _context.AddRange(logs);
            _context.SaveChanges();

            return Json(new { success = true, message = $"Selected orders and items marked as {request.Status}." });
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
    }
}