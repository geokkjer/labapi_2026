using LabApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LabApi.Data;

public static class DbInitializer
{
    public static async Task Seed(AppDbContext db)
    {
        if (await db.Products.AnyAsync())
            return;

        var products = new List<Product>
        {
            new Product { Name = "Laptop", Price = 12999.99m },
            new Product { Name = "Wireless Mouse", Price = 499.00m },
            new Product { Name = "Mechanical Keyboard", Price = 1499.00m },
            new Product { Name = "USB-C Hub", Price = 799.00m },
            new Product { Name = "27\" 4K Monitor", Price = 5499.00m },
            new Product { Name = "Noise Cancelling Headphones", Price = 2499.00m },
            new Product { Name = "Webcam 1080p", Price = 899.00m },
            new Product { Name = "External SSD 1TB", Price = 1299.00m },
            new Product { Name = "Wireless Charger", Price = 349.00m },
            new Product { Name = "Standing Desk", Price = 4999.00m },
            new Product { Name = "Ergonomic Chair", Price = 3999.00m },
            new Product { Name = "Smartphone", Price = 7999.00m },
            new Product { Name = "Tablet", Price = 5999.00m },
            new Product { Name = "Smartwatch", Price = 2999.00m },
            new Product { Name = "Bluetooth Speaker", Price = 999.00m },
            new Product { Name = "Gaming Console", Price = 4999.00m },
        };

        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }
}
