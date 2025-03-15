namespace Inventree_App.Models
{
    public class Indents
    {
        public int Id { get; set; }

        public int FacultyId { get; set; }

        public int ItemId { get; set; }

        public int Quantity { get; set; }  
        
        public string? Status { get; set; }
    }
}
