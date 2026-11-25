using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanDistrict.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillCostEstimateVatRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "CostEstimates" AS estimate
                SET "VatRate" = tariff."VatRate"
                FROM "Tariffs" AS tariff
                WHERE estimate."TariffId" = tariff."Id"
                  AND estimate."VatRate" = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
