namespace Inventree_App.Models
{
    public class Stocks
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? SerialNumber { get; set; }
        public int Quantity { get; set; }
        public int MaxQuantity { get; set; } // Define max stock level for progress bar
        public string? LocationId { get; set; }
        public string? CategoryId { get; set; }
    }
}
