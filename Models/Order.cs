namespace Inventree_App.Models
{
    public class Order
    {
        public int Id { get; set; } // Primary Key
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; } 
        public int ItemsCount { get; set; } 
        public string? Status { get; set; } // pending , success, approved
    }
    public class OrderAndItemsUpdateRequest
    {
        public List<int> OrderIds { get; set; }
        public List<int> OrderItemIds { get; set; }
        public string Status { get; set; }
    }


}
