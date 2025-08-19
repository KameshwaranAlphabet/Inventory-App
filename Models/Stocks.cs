using Inventree_App.Enum;

namespace Inventree_App.Models
{
    /// <summary>
    /// Stocks Model Class to store the Stocks Information
    /// </summary>
    public class Stocks
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? SerialNumber { get; set; }
        public int? Quantity { get; set; }
        public int? MaxQuantity { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? Email { get; set; }
        public string? Barcode { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? LocationId { get; set; }
        public int? CategoryId { get; set; }
        // Unit Type (Pack, Box, etc.)
        public string? UnitType { get; set; }
        public int? UnitQuantity { get; set; }
        public int? UnitCapacity { get; set; }
        public string? SubUnitType { get; set; }
        public string? ImageUrl { get; set; }
    }
}
