namespace Inventree_App.Models
{
    public class ManualStockPage
    {
        public int Id { get; set; }

        public string? StockName { get; set; }

        public int? Quantity { get; set; }

        public string? CustomerName { get; set; }

        public DateTime? Created { get; set; }
    }
}
