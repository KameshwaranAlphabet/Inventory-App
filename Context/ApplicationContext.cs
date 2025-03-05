using Inventree_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventree_App.Context
{
    public class ApplicationContext : DbContext
    {
        public DbSet<InventoryItem> InventoryItems { get; set; }

    }
}
