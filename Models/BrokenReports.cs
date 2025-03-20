namespace Inventree_App.Models
{
    public class BrokenReports
    {
        public int Id { get; set; }
        public int? EquipmentID { get; set; }
        public int? ReporterID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? Status { get; set; }
    }
}
