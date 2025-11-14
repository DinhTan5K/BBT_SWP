using Microsoft.EntityFrameworkCore;
using start.Models;
using start.Models.System;


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

        public DbSet<Discount> Discounts { get; set; }
        public DbSet<DiscountUsage> DiscountUsages { get; set; }
        public DbSet<DiscountRequest> DiscountRequests { get; set; }
        public DbSet<NewsRequest> NewsRequests { get; set; }
        public DbSet<EmployeeBranchRequest> EmployeeBranchRequests { get; set; }
        public DbSet<ProductRequest> ProductRequests { get; set; }
        public DbSet<CategoryRequest> CategoryRequests { get; set; }
        public DbSet<BranchRequest> BranchRequests { get; set; }

        public DbSet<Role> Roles { get; set; }
        public DbSet<Employee> Employees { get; set; }

        public DbSet<Contract> Contracts => Set<Contract>();
        public DbSet<WorkSchedule> WorkSchedules { get; set; }
        public DbSet<Salary> Salaries => Set<Salary>();
        public DbSet<SalaryAdjustment> SalaryAdjustments => Set<SalaryAdjustment>();
        public DbSet<DayOffRequest> DayOffRequests => Set<DayOffRequest>();
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<News> News { get; set; }
        
        public DbSet<Wishlist> Wishlist { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<ChatHistory> ChatHistories { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<MarketingKPI> MarketingKPIs { get; set; }
        public DbSet<AdminSecurity> AdminSecurities { get; set; }
    }
}
