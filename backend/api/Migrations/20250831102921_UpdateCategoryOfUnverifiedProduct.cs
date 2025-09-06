using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleCommerce.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCategoryOfUnverifiedProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "CategoryId",
                value: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "CategoryId",
                value: 1);
        }
    }
}
