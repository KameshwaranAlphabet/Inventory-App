﻿namespace Inventree_App.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? UserName { get; set; }

        public string? Email { get; set; }

        public string? Password { get; set; }

        public string ? Image { get; set; }

        public DateTime? CreatedOn { get; set; }
        public string? UserRoles { get; set; }
    }
}
