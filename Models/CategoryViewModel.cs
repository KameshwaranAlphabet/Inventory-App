namespace Inventree_App.Models
{
    public class CategoryViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int? ParentId { get; set; }
        public int Count { get; set; }
        public DateTime? CreatedOn { get; set; }
    }

    public class LocationViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ImageUrl { get; set; }
        public int Count { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}
