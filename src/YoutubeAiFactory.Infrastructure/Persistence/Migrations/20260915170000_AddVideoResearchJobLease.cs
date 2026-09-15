using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations;

[Migration("20260915170000_AddVideoResearchJobLease")]
public partial class AddVideoResearchJobLease : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "lease_id",
            schema: "yaf",
            table: "jobs",
            type: "uniqueidentifier",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "lease_id",
            schema: "yaf",
            table: "jobs");
    }
}
