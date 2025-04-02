namespace Inventree_App.Models
{
    public class DashboardViewModel
    {
        public decimal TotalSales { get; set; }
        public decimal TotalPurchases { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalGrowth { get; set; }
        public List<Order> Orders { get; set; } = new();
        public List<Logs> Logs { get; set; } = new();
    }
    public class DashBoardOrder
    {
        public string? Profile { get; set; }
        public string? Name { get; set; }
        public DateTime Date { get; set; }
        public int ItemCount { get; set; }
        public int? Quantity { get; set; }
        public int OrderId { get; set; }
    }

    public class DashboardLog
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
