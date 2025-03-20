namespace Inventree_App.Models
{
    public class Logs
    {
        public int Id { get; set; }
        public int? UserID { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? UserName { get; set; }    
        public string? Type { get; set; }    
    }
}
