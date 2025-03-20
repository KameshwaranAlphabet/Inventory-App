namespace Inventree_App.Models
{
    public class Chemicals
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? GradeUsage { get; set; }
        public int Quantity { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
