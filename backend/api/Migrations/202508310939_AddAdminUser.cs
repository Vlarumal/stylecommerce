using Microsoft.EntityFrameworkCore.Migrations;
using StyleCommerce.Api.Utils;

#nullable disable

namespace StyleCommerce.Api.Migrations
{
    public partial class AddAdminUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var hasher = new PasswordHasher();
            var adminPassword = hasher.HashPassword("SecureAdminPass123!");

            migrationBuilder.Sql(
                $@"
                INSERT INTO Users (Username, Email, FirstName, LastName, PasswordHash, IsActive, CreatedAt, UpdatedAt, Role)
                VALUES (
                    'admin@stylecommerce.com',
                    'admin@stylecommerce.com',
                    'Admin',
                    'User',
                    '{adminPassword}',
                    1,
                    GETUTCDATE(),
                    GETUTCDATE(),
                    'Admin'
                )"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Users WHERE Username = 'admin@stylecommerce.com'");
        }
    }
}
