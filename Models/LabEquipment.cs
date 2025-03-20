namespace Inventree_App.Models
{
    public class LabEquipment
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? Quantity { get; set; }
        public string? Type { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CategoryId { get; set; }
        public string? Status { get; set; }
        public int BrokenReportID { get; set; }
        public int MaxQuantity { get; set; }
    }
}