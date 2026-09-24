using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionConcurrencyAndWeights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "relative_duration_weight",
                schema: "yaf",
                table: "production_shots",
                type: "decimal(10,4)",
                precision: 10,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                schema: "yaf",
                table: "production_packages",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "relative_duration_weight",
                schema: "yaf",
                table: "production_shots");

            migrationBuilder.DropColumn(
                name: "row_version",
                schema: "yaf",
                table: "production_packages");
        }
    }
}
