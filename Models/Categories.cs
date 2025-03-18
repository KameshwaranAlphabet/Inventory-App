namespace Inventree_App.Models
{
    public class Categories
    {
        public int Id { get; set; }
        public string? CategoryName { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int ParentCategoryId { get; set; }
    }
}
