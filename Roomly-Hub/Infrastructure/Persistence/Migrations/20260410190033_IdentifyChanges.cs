using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IdentifyChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitNumber",
                table: "Rooms");

            migrationBuilder.AddColumn<int>(
                name: "BookingMode",
                table: "Rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CancellationPolicy",
                table: "Rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Rooms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckInDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CheckOutDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    Source = table.Column<string>(type: "text", nullable: false, defaultValue: "Standard"),
                    BookingMode = table.Column<string>(type: "text", nullable: false, defaultValue: "InstantBook"),
                    CancellationPolicy = table.Column<string>(type: "text", nullable: false, defaultValue: "FreeCancellation"),
                    PaymentMethod = table.Column<string>(type: "text", nullable: false),
                    PaymentStatus = table.Column<string>(type: "text", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledBy = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Users_GuestId",
                        column: x => x.GuestId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomAvailabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "varchar(40)", nullable: false, defaultValue: "HostBlocked"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomAvailabilities_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_GuestId",
                table: "Bookings",
                column: "GuestId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RoomId",
                table: "Bookings",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RoomId_CheckInDate_CheckOutDate",
                table: "Bookings",
                columns: new[] { "RoomId", "CheckInDate", "CheckOutDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomAvailabilities_RoomId_BlockedDate",
                table: "RoomAvailabilities",
                columns: new[] { "RoomId", "BlockedDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "RoomAvailabilities");

            migrationBuilder.DropColumn(
                name: "BookingMode",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "CancellationPolicy",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Rooms");

            migrationBuilder.AddColumn<string>(
                name: "UnitNumber",
                table: "Rooms",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
