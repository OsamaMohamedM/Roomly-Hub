using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class add_booking_payment_flow_fields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApprovedByHost",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentInvoiceId",
                table: "Bookings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentInvoiceKey",
                table: "Bookings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PaymentInvoiceId",
                table: "Bookings",
                column: "PaymentInvoiceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_PaymentInvoiceId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsApprovedByHost",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PaymentInvoiceId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PaymentInvoiceKey",
                table: "Bookings");
        }
    }
}
