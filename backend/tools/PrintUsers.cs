using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using StyleCommerce.Api.Data;
using StyleCommerce.Api.Models;

class Program
{
    static void Main(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=stylecommerce_dev;Username=postgres;Password=postgres"
        );

        using var context = new ApplicationDbContext(optionsBuilder.Options);

        var users = context.Users.ToList();
        Console.WriteLine($"Found {users.Count} users:");
        foreach (var user in users)
        {
            Console.WriteLine(
                $"ID: {user.Id}, Email: {user.Email}, PasswordHash: {user.PasswordHash}"
            );
        }
    }
}
