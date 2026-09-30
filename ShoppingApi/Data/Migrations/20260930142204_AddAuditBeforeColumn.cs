using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditBeforeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Before",
                table: "Audit",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("UPDATE [Audit] SET [Before] = N'{}' WHERE [Before] IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "Before",
                table: "Audit",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Before",
                table: "Audit");
        }
    }
}
