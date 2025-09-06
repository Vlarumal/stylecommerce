using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StyleCommerce.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUnverifiedProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[]
                {
                    "Id",
                    "Brand",
                    "CategoryId",
                    "Color",
                    "CreatedAt",
                    "Description",
                    "EcoScore",
                    "ImageUrl",
                    "IsVerified",
                    "Model3DUrl",
                    "Name",
                    "Price",
                    "Size",
                    "StockQuantity",
                    "UpdatedAt",
                    "VerificationScore",
                },
                values: new object[]
                {
                    4,
                    "EcoUnknown",
                    3,
                    "Natural",
                    new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    "Sustainable tote bag made from hemp fibers",
                    0,
                    "/images/hemp-tote-bag.png",
                    false,
                    "",
                    "Hemp Fabric Tote Bag",
                    24.99m,
                    "M",
                    30,
                    new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    0,
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: 4);
        }
    }
}
