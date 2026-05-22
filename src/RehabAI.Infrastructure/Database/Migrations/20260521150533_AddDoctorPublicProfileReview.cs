using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RehabAI.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorPublicProfileReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicProfileRejectionReason",
                table: "DoctorProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PublicProfileReviewStatus",
                table: "DoctorProfiles",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "DoctorProfiles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByAdminId",
                table: "DoctorProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedForReviewAt",
                table: "DoctorProfiles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [DoctorProfiles]
                SET [PublicProfileReviewStatus] = 3
                WHERE [PublicProfileApproved] = 1
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicProfileRejectionReason",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "PublicProfileReviewStatus",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "ReviewedByAdminId",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "SubmittedForReviewAt",
                table: "DoctorProfiles");
        }
    }
}
