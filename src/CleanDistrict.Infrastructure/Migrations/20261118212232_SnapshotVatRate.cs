using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanDistrict.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SnapshotVatRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "CostEstimates",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "CostEstimates");
        }
    }
}
