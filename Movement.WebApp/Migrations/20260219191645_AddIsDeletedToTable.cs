using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Movement.WebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeletedToTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DataEntries",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DataEntries");
        }
    }
}
