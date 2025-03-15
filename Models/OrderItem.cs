using System.ComponentModel.DataAnnotations.Schema;

namespace Inventree_App.Models
{
    public class OrderItem
    {
        public int Id { get; set; } // Primary Key
        public int OrderId { get; set; } // Foreign Key
        public int StockId { get; set; }
        public int Quantity { get; set; }
        public string? StockName { get; set; }
        public string? Status { get; set; }
        // Navigation Property to Order
        [ForeignKey("OrderId")]
        public Order Order { get; set; }
    }
}
