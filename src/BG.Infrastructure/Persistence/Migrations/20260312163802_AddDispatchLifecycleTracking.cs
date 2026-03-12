using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchLifecycleTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeliveredAtUtc",
                table: "guarantee_correspondence",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryNote",
                table: "guarantee_correspondence",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryReference",
                table: "guarantee_correspondence",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchChannel",
                table: "guarantee_correspondence",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchNote",
                table: "guarantee_correspondence",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchReference",
                table: "guarantee_correspondence",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DispatchedAtUtc",
                table: "guarantee_correspondence",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastPrintMode",
                table: "guarantee_correspondence",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastPrintedAtUtc",
                table: "guarantee_correspondence",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrintCount",
                table: "guarantee_correspondence",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveredAtUtc",
                table: "guarantee_correspondence");

            migrationBuilder.DropColumn(
                name: "DeliveryNote",
                table: "guarantee_correspondence");

            migrationBuilder.DropColumn(
                name: "DeliveryReference",
                table: "guarantee_correspondence");

            migrationBuilder.DropColumn(
                name: "DispatchChannel",
                table: "guarantee_correspondence");

            migrationBuilder.DropColumn(
                name: "DispatchNote",
                table: "guarantee_correspondence");

            migrationBuilder.DropColumn(
                name: "DispatchReference",
                table: "guarantee_correspondence");

            migrationBuilder.DropColumn(
                name: "DispatchedAtUtc",
                table: "guarantee_correspondence");

            migrationBuilder.DropColumn(
                name: "LastPrintMode",
                table: "guarantee_correspondence");

            migrationBuilder.DropColumn(
                name: "LastPrintedAtUtc",
                table: "guarantee_correspondence");

            migrationBuilder.DropColumn(
                name: "PrintCount",
                table: "guarantee_correspondence");
        }
    }
}
