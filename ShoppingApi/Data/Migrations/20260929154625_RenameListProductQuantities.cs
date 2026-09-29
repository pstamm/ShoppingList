using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameListProductQuantities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [ListProducts] SET [QuantityToOrder] = ROUND([QuantityToOrder], 0), [PendingQuantity] = ROUND([PendingQuantity], 0);");

            migrationBuilder.AlterColumn<int>(
                name: "QuantityToOrder",
                table: "ListProducts",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)");

            migrationBuilder.AlterColumn<int>(
                name: "PendingQuantity",
                table: "ListProducts",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)");

            migrationBuilder.RenameColumn(
                name: "QuantityToOrder",
                table: "ListProducts",
                newName: "TipicalOrder");

            migrationBuilder.RenameColumn(
                name: "PendingQuantity",
                table: "ListProducts",
                newName: "ToOrderNow");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TipicalOrder",
                table: "ListProducts",
                newName: "QuantityToOrder");

            migrationBuilder.RenameColumn(
                name: "ToOrderNow",
                table: "ListProducts",
                newName: "PendingQuantity");

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityToOrder",
                table: "ListProducts",
                type: "decimal(18,3)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "PendingQuantity",
                table: "ListProducts",
                type: "decimal(18,3)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
