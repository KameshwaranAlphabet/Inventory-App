using Inventree_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventree_App.Context
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
        }

        public DbSet<Stocks> Stocks { get; set; } // Replace 'Stocks' with your model
        public DbSet<Customer> Customer { get; set; } 
    }
}
