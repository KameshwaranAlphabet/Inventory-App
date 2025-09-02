namespace Inventree_App.Models
{
    public class StockViewModel
    {
        public int ID { get; set; }
        public string? Description { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public int? StockQuantity { get; set; }
        public int? CartQuantity { get; set; }
        public int? LocationId { get; set; }
        public string? StockLocation { get; set; }
        public string? ImageUrl { get; set; }
    }
}
