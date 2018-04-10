using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace CinemaBookingClient.Data.Migrations
{
    public partial class TicketToOrderToSeance2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_CinemaHalls_CinemaHallId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_CinemaHallId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CinemaHallId",
                table: "Tickets");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CinemaHallId",
                table: "Tickets",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CinemaHallId",
                table: "Tickets",
                column: "CinemaHallId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_CinemaHalls_CinemaHallId",
                table: "Tickets",
                column: "CinemaHallId",
                principalTable: "CinemaHalls",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
