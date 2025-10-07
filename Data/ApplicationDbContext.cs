using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using start.Models;

namespace start.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }

        public DbSet<Branch> Branches { get; set; }

        public DbSet<Region> Regions { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartDetail> CartDetails { get; set; }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<ProductSize> ProductSizes { get; set; }

    }
}
