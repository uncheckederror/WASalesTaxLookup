using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WASalesTax.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AddressRanges",
                columns: table => new
                {
                    AddressRangeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AddressRangeLowerBound = table.Column<int>(type: "INTEGER", nullable: true),
                    AddressRangeUpperBound = table.Column<int>(type: "INTEGER", nullable: true),
                    OddOrEven = table.Column<char>(type: "TEXT", nullable: false),
                    Street = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<string>(type: "TEXT", nullable: true),
                    ZipCode = table.Column<string>(type: "TEXT", nullable: true),
                    ZipCodePlus4 = table.Column<string>(type: "TEXT", nullable: true),
                    Period = table.Column<string>(type: "TEXT", nullable: true),
                    LocationCode = table.Column<int>(type: "INTEGER", nullable: false),
                    RTA = table.Column<char>(type: "TEXT", nullable: false),
                    PTBAName = table.Column<string>(type: "TEXT", nullable: true),
                    CEZName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddressRanges", x => x.AddressRangeId);
                });

            migrationBuilder.CreateTable(
                name: "TaxRates",
                columns: table => new
                {
                    LocationCode = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<double>(type: "REAL", nullable: false),
                    Local = table.Column<double>(type: "REAL", nullable: false),
                    RTA = table.Column<double>(type: "REAL", nullable: false),
                    Rate = table.Column<double>(type: "REAL", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRates", x => x.LocationCode);
                });

            migrationBuilder.CreateTable(
                name: "ZipCodes",
                columns: table => new
                {
                    ShortZipId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Zip = table.Column<string>(type: "TEXT", nullable: true),
                    Plus4LowerBound = table.Column<string>(type: "TEXT", nullable: true),
                    Plus4UpperBound = table.Column<string>(type: "TEXT", nullable: true),
                    LocationCode = table.Column<int>(type: "INTEGER", nullable: false),
                    State = table.Column<string>(type: "TEXT", nullable: true),
                    Local = table.Column<string>(type: "TEXT", nullable: true),
                    TotalRate = table.Column<string>(type: "TEXT", nullable: true),
                    EffectiveStartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveEndDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZipCodes", x => x.ShortZipId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AddressRanges");

            migrationBuilder.DropTable(
                name: "TaxRates");

            migrationBuilder.DropTable(
                name: "ZipCodes");
        }
    }
}
