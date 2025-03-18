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
        public DbSet<Indents> Indents { get; set; } 
        public DbSet<CartItem> CartItem { get; set; } 
        public DbSet<Order> Order { get; set; } 
        public DbSet<OrderItem> OrderItem { get; set; } 
        public DbSet<Categories> Categories { get; set; } 
        public DbSet<Location> Location { get; set; } 
        public DbSet<Logs> Logs { get; set; } 
    }
}
