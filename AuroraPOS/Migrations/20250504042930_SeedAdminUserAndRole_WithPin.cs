using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuroraPOS.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUserAndRoleWithPin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Role",
                columns: new[] { "ID", "CreatedBy", "CreatedDate", "IsActive", "IsDeleted", "Priority", "RoleName", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 1L, null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, 1, "Admin", null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "User",
                columns: new[] { "ID", "Address", "City", "CreatedBy", "CreatedDate", "Email", "FullName", "IsActive", "IsDeleted", "Password", "PhoneNumber", "Pin", "ProfileImage", "State", "Type", "UpdatedBy", "UpdatedDate", "Username", "ZipCode" },
                values: new object[] { 1L, "", "", null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@aurora.com", "Administrador", true, false, "admin123", "0000000000", "1234", "", "", 0, null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin", "" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Role",
                keyColumn: "ID",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                table: "User",
                keyColumn: "ID",
                keyValue: 1L);
        }
    }
}
