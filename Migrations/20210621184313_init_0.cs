using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RY.TransferImagePro.Migrations
{
    public partial class init_0 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImageInformations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(maxLength: 40, nullable: true),
                    FileExtension = table.Column<string>(maxLength: 10, nullable: true),
                    FileSize = table.Column<long>(nullable: false),
                    Location = table.Column<string>(maxLength: 200, nullable: true),
                    CreateTime = table.Column<DateTime>(nullable: false),
                    UploadTime = table.Column<DateTime>(nullable: false),
                    HasUploaded = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageInformations", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageInformations");
        }
    }
}
