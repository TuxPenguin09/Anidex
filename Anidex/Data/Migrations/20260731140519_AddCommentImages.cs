using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Anidex.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old single-image URL column — replaced by 4 explicit image slots.
            migrationBuilder.DropColumn(
                name: "MediaUrl",
                table: "Comments");

            migrationBuilder.AddColumn<byte[]>(
                name: "Image1",
                table: "Comments",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Image2",
                table: "Comments",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Image3",
                table: "Comments",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Image4",
                table: "Comments",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image1ContentType",
                table: "Comments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image2ContentType",
                table: "Comments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image3ContentType",
                table: "Comments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image4ContentType",
                table: "Comments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image1",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "Image2",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "Image3",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "Image4",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "Image1ContentType",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "Image2ContentType",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "Image3ContentType",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "Image4ContentType",
                table: "Comments");

            migrationBuilder.AddColumn<string>(
                name: "MediaUrl",
                table: "Comments",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
