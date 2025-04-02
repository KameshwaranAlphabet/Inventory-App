namespace Inventree_App.Dto
{
    public class CartItemDto
    {
        public int Id { get; set; }
        public int StockId { get; set; }
        public string? StockName { get; set; }
        public int Quantity { get; set; }
        public int UserId { get; set; }
        public string Units { get; set; }
    }
}
