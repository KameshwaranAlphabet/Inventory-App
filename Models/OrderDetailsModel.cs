namespace Inventree_App.Models
{
    public class OrderDetailsModel
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string? Status { get; set; }
        public DateTime OrderedDate { get; set; }
        public int ItemsCount { get; set; }
        public string? CustomerName { get; set; }
        public List<OrderItem>? Items { get; set; }
    
    }
}
