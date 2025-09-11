using System;
using Microsoft.EntityFrameworkCore;
using StyleCommerce.Api.Data;
using StyleCommerce.Api.Models;
using StyleCommerce.Api.Utils;

class UpdateUserPasswordsProgram
{
    static void Main(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=stylecommerce_dev;Username=postgres;Password=postgres"
        );

        using var context = new ApplicationDbContext(optionsBuilder.Options);

        var users = context.Users.ToList();
        Console.WriteLine($"Found {users.Count} users to update:");

        foreach (var user in users)
        {
            string defaultPassword = "Password123!";
            var passwordHasher = new PasswordHasher();
            user.PasswordHash = passwordHasher.HashPassword(defaultPassword);
            Console.WriteLine($"Updated user {user.Email} with default password");
        }

        context.SaveChanges();
        Console.WriteLine("All users updated successfully!");
    }
}
