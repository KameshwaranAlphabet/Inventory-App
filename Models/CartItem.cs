namespace Inventree_App.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int StockId { get; set; }
        public string? StockName { get; set; }
        public int Quantity { get; set; }
        public int UserId { get; set; }
    }
    public class CartRequest
    {
        public string StockId { get; set; }
        public int Quantity { get; set; }
    }
    public class CartRemoveRequest
    {
        public int StockId { get; set; }
    }
}
