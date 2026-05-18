using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiTrack.Migrations
{
    /// <inheritdoc />
    public partial class StandardLDMSRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Routes__Shipment__07C12930",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Routes_ShipmentId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "ShipmentId",
                table: "Routes");

            migrationBuilder.AddColumn<int>(
                name: "RouteId",
                table: "Shipments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DriverId",
                table: "Routes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteNumber",
                table: "Routes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_RouteId",
                table: "Shipments",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_DriverId",
                table: "Routes",
                column: "DriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Drivers_DriverId",
                table: "Routes",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "DriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_Routes_RouteId",
                table: "Shipments",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "RouteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Drivers_DriverId",
                table: "Routes");

            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Routes_RouteId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_RouteId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Routes_DriverId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "EstimatedCost",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "RouteId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "ShippingFee",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "RouteNumber",
                table: "Routes");

            migrationBuilder.AddColumn<int>(
                name: "ShipmentId",
                table: "Routes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_ShipmentId",
                table: "Routes",
                column: "ShipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Shipments_ShipmentId",
                table: "Routes",
                column: "ShipmentId",
                principalTable: "Shipments",
                principalColumn: "ShipmentId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
