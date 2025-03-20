namespace Inventree_App.Models
{
    public class AccidentReports
    {
        public int Id {  get; set; }
        public string? SubmittedBy { get; set; }
        public string? LabType { get; set;}
        public string? Description { get; set; }
        public DateTime? CreatedOn { get; set; }    
    }
}
