using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RehabAI.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailLogMetadataJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "EmailLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "EmailLogs");
        }
    }
}
