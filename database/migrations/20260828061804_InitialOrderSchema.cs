using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RxFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialOrderSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Prescription_Sphere = table.Column<decimal>(type: "numeric", nullable: false),
                    Prescription_Cylinder = table.Column<decimal>(type: "numeric", nullable: false),
                    Prescription_Axis = table.Column<int>(type: "integer", nullable: false),
                    Frame_Id = table.Column<string>(type: "text", nullable: true),
                    Frame_A = table.Column<decimal>(type: "numeric", nullable: false),
                    Frame_B = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
