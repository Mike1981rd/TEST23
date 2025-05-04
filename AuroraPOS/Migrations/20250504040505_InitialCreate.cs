using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AuroraPOS.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountDescriptions",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountDescriptions", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "AccountTypes",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountTypes", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Areas",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Width = table.Column<decimal>(type: "numeric", nullable: false),
                    Height = table.Column<decimal>(type: "numeric", nullable: false),
                    BackColor = table.Column<string>(type: "text", nullable: true),
                    BackImage = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsSystemDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "CancelReasons",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    IsPrintAccount = table.Column<bool>(type: "boolean", nullable: false),
                    IsPrintOverrideChannel = table.Column<bool>(type: "boolean", nullable: false),
                    IsReduceInventory = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancelReasons", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "CloseDrawers",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    StationID = table.Column<long>(type: "bigint", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    TransTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    TipTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    TransDifference = table.Column<decimal>(type: "numeric", nullable: false),
                    TipDifference = table.Column<decimal>(type: "numeric", nullable: false),
                    Denominations = table.Column<string>(type: "text", nullable: false),
                    PaymentMethods = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloseDrawers", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryCarriers",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryCarriers", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryZones",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    Time = table.Column<decimal>(type: "numeric", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryZones", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Denominations",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Denominations", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Discounts",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DiscountAmountType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discounts", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ErrorOuts",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorOuts", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroupName = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Kitchen",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PrinterID = table.Column<long>(type: "bigint", nullable: false),
                    Stations = table.Column<List<long>>(type: "bigint[]", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kitchen", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "KitchenOrder",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KitchenID = table.Column<long>(type: "bigint", nullable: false),
                    OrderID = table.Column<long>(type: "bigint", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompleteTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenOrder", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "KitchenOrderItem",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KitchenOrderID = table.Column<long>(type: "bigint", nullable: false),
                    OrderItemID = table.Column<long>(type: "bigint", nullable: false),
                    CompleteTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenOrderItem", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "logs",
                columns: table => new
                {
                    idlog = table.Column<int>(name: "id_log", type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ubicacion = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logs", x => x.idlog);
                });

            migrationBuilder.CreateTable(
                name: "Menus",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menus", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "OrderComprobantes",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    ComprobanteNumber = table.Column<string>(type: "text", nullable: false),
                    ComprobanteName = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderComprobantes", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Tasa = table.Column<decimal>(type: "numeric", nullable: false),
                    Tip = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentType = table.Column<string>(type: "text", nullable: false),
                    Image = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Value = table.Column<string>(type: "text", nullable: false),
                    DisplayValue = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Group = table.Column<string>(type: "text", nullable: false),
                    GroupOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Preferences",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Company = table.Column<string>(type: "text", nullable: true),
                    Logo = table.Column<string>(type: "text", nullable: true),
                    RNC = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Address1 = table.Column<string>(type: "text", nullable: true),
                    Address2 = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    PostalCode = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    Currency = table.Column<string>(type: "text", nullable: true),
                    SecondCurrency = table.Column<string>(type: "text", nullable: true),
                    Tasa = table.Column<decimal>(type: "numeric", nullable: false),
                    StationLimit = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preferences", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PrepareTypes",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SinChofer = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrepareTypes", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PrinterChannels",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrinterChannels", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Printers",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Model = table.Column<string>(type: "text", nullable: true),
                    PhysicalName = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Printers", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PrinterTasks",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ObjectID = table.Column<long>(type: "bigint", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    StationId = table.Column<long>(type: "bigint", nullable: true),
                    SucursalId = table.Column<long>(type: "bigint", nullable: true),
                    DivideNum = table.Column<int>(type: "integer", nullable: true),
                    SeatNum = table.Column<int>(type: "integer", nullable: true),
                    PhysicalName = table.Column<string>(type: "text", nullable: true),
                    Items = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrinterTasks", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PrintJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobId = table.Column<string>(type: "text", nullable: false),
                    PrinterName = table.Column<string>(type: "text", nullable: false),
                    HtmlContent = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PrintedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Promotions",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AmountType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    IsAllDay = table.Column<bool>(type: "boolean", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecurringStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Duration = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountType = table.Column<int>(type: "integer", nullable: false),
                    ApplyType = table.Column<int>(type: "integer", nullable: false),
                    FirstCount = table.Column<int>(type: "integer", nullable: false),
                    EveryCount = table.Column<int>(type: "integer", nullable: false),
                    EndOccurrence = table.Column<int>(type: "integer", nullable: false),
                    IsRecurNoEnd = table.Column<bool>(type: "boolean", nullable: false),
                    WeekNum = table.Column<int>(type: "integer", nullable: false),
                    WeekDays = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotions", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Propinas",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PropinaName = table.Column<string>(type: "text", nullable: false),
                    PropinaValue = table.Column<double>(type: "double precision", nullable: false),
                    IsInPurchaseOrder = table.Column<bool>(type: "boolean", nullable: false),
                    IsInArticle = table.Column<bool>(type: "boolean", nullable: false),
                    IsShipping = table.Column<bool>(type: "boolean", nullable: false),
                    IsToGoExclude = table.Column<bool>(type: "boolean", nullable: false),
                    IsBarcodeExclude = table.Column<bool>(type: "boolean", nullable: false),
                    IsKioskExclude = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Propinas", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MaxAnswer = table.Column<int>(type: "integer", nullable: false),
                    MinAnswer = table.Column<int>(type: "integer", nullable: false),
                    FreeChoice = table.Column<int>(type: "integer", nullable: false),
                    IsAutomatic = table.Column<bool>(type: "boolean", nullable: false),
                    IsForced = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReservationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuestCount = table.Column<int>(type: "integer", nullable: false),
                    Duration = table.Column<decimal>(type: "numeric", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric", nullable: false),
                    GuestName = table.Column<string>(type: "text", nullable: false),
                    ClientName = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: false),
                    TableID = table.Column<int>(type: "integer", nullable: false),
                    StationID = table.Column<int>(type: "integer", nullable: false),
                    AreaID = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TableName = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleName = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServingSize",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServingSize", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "SmartButtons",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsApplyPrice = table.Column<bool>(type: "boolean", nullable: false),
                    IsAfterText = table.Column<bool>(type: "boolean", nullable: false),
                    BackColor = table.Column<string>(type: "text", nullable: true),
                    TextColor = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmartButtons", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "StationWarehouses",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StationID = table.Column<long>(type: "bigint", nullable: false),
                    GroupID = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseID = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationWarehouses", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    RNC = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Direction = table.Column<string>(type: "text", nullable: true),
                    IsTaxIncluded = table.Column<bool>(type: "boolean", nullable: false),
                    IsFormalSupplier = table.Column<bool>(type: "boolean", nullable: false),
                    Seller = table.Column<string>(type: "text", nullable: true),
                    CellPhone = table.Column<string>(type: "text", nullable: true),
                    Avatar = table.Column<string>(type: "text", nullable: true),
                    DeliveryTime = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "t_formato_impresion_general",
                columns: table => new
                {
                    fid = table.Column<int>(name: "f_id", type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fdescripcion = table.Column<string>(name: "f_descripcion", type: "text", nullable: true),
                    flimite = table.Column<int>(name: "f_limite", type: "integer", nullable: true),
                    fcometario = table.Column<string>(name: "f_cometario", type: "text", nullable: true),
                    forden = table.Column<int>(name: "f_orden", type: "integer", nullable: true),
                    farchivo = table.Column<string>(name: "f_archivo", type: "text", nullable: true),
                    fnombrearchivo = table.Column<string>(name: "f_nombre_archivo", type: "text", nullable: true),
                    fsecuencianocontrol = table.Column<int>(name: "f_secuencia_no_control", type: "integer", nullable: true),
                    fformatoimpresion = table.Column<int>(name: "f_formato_impresion", type: "integer", nullable: true),
                    fimpresionporpantalla = table.Column<bool>(name: "f_impresion_por_pantalla", type: "boolean", nullable: true),
                    fimpresora = table.Column<string>(name: "f_impresora", type: "text", nullable: true),
                    farchivo1 = table.Column<string>(name: "f_archivo1", type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_formato_impresion_general", x => x.fid);
                });

            migrationBuilder.CreateTable(
                name: "t_formato_impresion_reportes",
                columns: table => new
                {
                    fid = table.Column<int>(name: "f_id", type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fdescripcion = table.Column<string>(name: "f_descripcion", type: "text", nullable: true),
                    flimite = table.Column<int>(name: "f_limite", type: "integer", nullable: true),
                    fcometario = table.Column<string>(name: "f_cometario", type: "text", nullable: true),
                    forden = table.Column<int>(name: "f_orden", type: "integer", nullable: true),
                    farchivo = table.Column<string>(name: "f_archivo", type: "text", nullable: true),
                    fnombrearchivo = table.Column<string>(name: "f_nombre_archivo", type: "text", nullable: true),
                    fsecuencianocontrol = table.Column<int>(name: "f_secuencia_no_control", type: "integer", nullable: true),
                    fformatoimpresion = table.Column<int>(name: "f_formato_impresion", type: "integer", nullable: true),
                    fimpresionporpantalla = table.Column<bool>(name: "f_impresion_por_pantalla", type: "boolean", nullable: true),
                    fimpresora = table.Column<string>(name: "f_impresora", type: "text", nullable: true),
                    farchivo1 = table.Column<string>(name: "f_archivo1", type: "text", nullable: true),
                    factivo = table.Column<bool>(name: "f_activo", type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_formato_impresion_reportes", x => x.fid);
                });

            migrationBuilder.CreateTable(
                name: "t_impresion",
                columns: table => new
                {
                    idreporte = table.Column<int>(name: "id_reporte", type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    extencion = table.Column<string>(type: "text", nullable: false),
                    cuerpo = table.Column<string>(type: "text", nullable: false),
                    impresora = table.Column<string>(type: "text", nullable: false),
                    idestacion = table.Column<string>(name: "id_estacion", type: "text", nullable: false),
                    statusimpresion = table.Column<int>(name: "status_impresion", type: "integer", nullable: false),
                    numerocopias = table.Column<int>(name: "numero_copias", type: "integer", nullable: false),
                    IDSucursal = table.Column<int>(type: "integer", nullable: false),
                    IsReprint = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_impresion", x => x.idreporte);
                });

            migrationBuilder.CreateTable(
                name: "t_impresoras",
                columns: table => new
                {
                    fid = table.Column<int>(name: "f_id", type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fidestacion = table.Column<string>(name: "f_id_estacion", type: "text", nullable: true),
                    fimpresora = table.Column<string>(name: "f_impresora", type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_impresoras", x => x.fid);
                });

            migrationBuilder.CreateTable(
                name: "t_sucursal",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_sucursal", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Pin = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ProfileImage = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    ZipCode = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Vouchers",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Class = table.Column<string>(type: "text", nullable: false),
                    Reorder = table.Column<decimal>(type: "numeric", nullable: false),
                    Secuencia = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    IsRequireRNC = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vouchers", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WarehouseName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    WarehouseNum = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "WorkDay",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Day = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IDSucursal = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkDay", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountTypeID = table.Column<long>(type: "bigint", nullable: false),
                    AccountDescriptionID = table.Column<long>(type: "bigint", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    CreditOrDebit = table.Column<int>(type: "integer", nullable: false),
                    Defaults = table.Column<List<int>>(type: "integer[]", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Accounts_AccountDescriptions_AccountDescriptionID",
                        column: x => x.AccountDescriptionID,
                        principalTable: "AccountDescriptions",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Accounts_AccountTypes_AccountTypeID",
                        column: x => x.AccountTypeID,
                        principalTable: "AccountTypes",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AreaObjects",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Width = table.Column<decimal>(type: "numeric", nullable: false),
                    Height = table.Column<decimal>(type: "numeric", nullable: false),
                    PositionX = table.Column<decimal>(type: "numeric", nullable: false),
                    PositionY = table.Column<decimal>(type: "numeric", nullable: false),
                    Radius = table.Column<decimal>(type: "numeric", nullable: false),
                    SeatCount = table.Column<int>(type: "integer", nullable: false),
                    BackColor = table.Column<string>(type: "text", nullable: true),
                    TextColor = table.Column<string>(type: "text", nullable: true),
                    BorderColor = table.Column<string>(type: "text", nullable: true),
                    BackImage = table.Column<string>(type: "text", nullable: true),
                    AreaID = table.Column<long>(type: "bigint", nullable: false),
                    Shape = table.Column<int>(type: "integer", nullable: false),
                    ObjectType = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaObjects", x => x.ID);
                    table.ForeignKey(
                        name: "FK_AreaObjects_Areas_AreaID",
                        column: x => x.AreaID,
                        principalTable: "Areas",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Plato = table.Column<int>(type: "integer", nullable: false),
                    CourseID = table.Column<long>(type: "bigint", nullable: false),
                    GroupID = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Categories_Groups_GroupID",
                        column: x => x.GroupID,
                        principalTable: "Groups",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stations",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PriceSelect = table.Column<int>(type: "integer", nullable: false),
                    MenuSelectID = table.Column<long>(type: "bigint", nullable: true),
                    TypeOfSales = table.Column<string>(type: "text", nullable: true),
                    OrderMode = table.Column<int>(type: "integer", nullable: false),
                    Bussiness = table.Column<string>(type: "text", nullable: true),
                    SalesMode = table.Column<int>(type: "integer", nullable: false),
                    PrintCopy = table.Column<int>(type: "integer", nullable: false),
                    IDSucursal = table.Column<int>(type: "integer", nullable: false),
                    AreaPrices = table.Column<string>(type: "text", nullable: true),
                    ImprimirPrecuentaDelivery = table.Column<bool>(type: "boolean", nullable: false),
                    PrecioDelivery = table.Column<int>(type: "integer", nullable: true),
                    PrepareTypeDefault = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stations", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Stations_Menus_MenuSelectID",
                        column: x => x.MenuSelectID,
                        principalTable: "Menus",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "PromotionTargets",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Menu = table.Column<long>(type: "bigint", nullable: false),
                    MenuName = table.Column<string>(type: "text", nullable: false),
                    ProductRange = table.Column<int>(type: "integer", nullable: false),
                    TargetId = table.Column<long>(type: "bigint", nullable: false),
                    ServingSizeID = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    PromotionID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionTargets", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PromotionTargets_Promotions_PromotionID",
                        column: x => x.PromotionID,
                        principalTable: "Promotions",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "PermissionRole",
                columns: table => new
                {
                    PermissionsID = table.Column<long>(type: "bigint", nullable: false),
                    RolesID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionRole", x => new { x.PermissionsID, x.RolesID });
                    table.ForeignKey(
                        name: "FK_PermissionRole_Permissions_PermissionsID",
                        column: x => x.PermissionsID,
                        principalTable: "Permissions",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PermissionRole_Role_RolesID",
                        column: x => x.RolesID,
                        principalTable: "Role",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SmartButtonItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    ButtonID = table.Column<long>(type: "bigint", nullable: false),
                    QuestionID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmartButtonItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SmartButtonItems_Questions_QuestionID",
                        column: x => x.QuestionID,
                        principalTable: "Questions",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_SmartButtonItems_SmartButtons_ButtonID",
                        column: x => x.ButtonID,
                        principalTable: "SmartButtons",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleUser",
                columns: table => new
                {
                    RolesID = table.Column<long>(type: "bigint", nullable: false),
                    UsersID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleUser", x => new { x.RolesID, x.UsersID });
                    table.ForeignKey(
                        name: "FK_RoleUser_Role_RolesID",
                        column: x => x.RolesID,
                        principalTable: "Role",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleUser_User_UsersID",
                        column: x => x.UsersID,
                        principalTable: "User",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    RNC = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Address1 = table.Column<string>(type: "text", nullable: true),
                    Address2 = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    PostalCode = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    Avatar = table.Column<string>(type: "text", nullable: true),
                    Company = table.Column<string>(type: "text", nullable: true),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    CreditDays = table.Column<int>(type: "integer", nullable: false),
                    VoucherID = table.Column<long>(type: "bigint", nullable: true),
                    DeliveryZoneID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Customers_Vouchers_VoucherID",
                        column: x => x.VoucherID,
                        principalTable: "Vouchers",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Taxs",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TaxName = table.Column<string>(type: "text", nullable: false),
                    TaxValue = table.Column<double>(type: "double precision", nullable: false),
                    IsInPurchaseOrder = table.Column<bool>(type: "boolean", nullable: false),
                    IsInArticle = table.Column<bool>(type: "boolean", nullable: false),
                    IsShipping = table.Column<bool>(type: "boolean", nullable: false),
                    IsToGoExclude = table.Column<bool>(type: "boolean", nullable: false),
                    IsBarcodeExclude = table.Column<bool>(type: "boolean", nullable: false),
                    IsKioskExclude = table.Column<bool>(type: "boolean", nullable: false),
                    VoucherID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Taxs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Taxs_Vouchers_VoucherID",
                        column: x => x.VoucherID,
                        principalTable: "Vouchers",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "MoveArticles",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FromWarehouseID = table.Column<long>(type: "bigint", nullable: false),
                    ToWarehouseID = table.Column<long>(type: "bigint", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    MoveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoveArticles", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MoveArticles_Warehouses_FromWarehouseID",
                        column: x => x.FromWarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MoveArticles_Warehouses_ToWarehouseID",
                        column: x => x.ToWarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SupplierID = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseID = table.Column<long>(type: "bigint", nullable: false),
                    NCF = table.Column<string>(type: "text", nullable: false),
                    Term = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaymentStatus = table.Column<int>(type: "integer", nullable: false),
                    OrderTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsDiscountPercent = table.Column<bool>(type: "boolean", nullable: false),
                    IsDiscountPercentItems = table.Column<bool>(type: "boolean", nullable: false),
                    Discount = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    Shipping = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierID",
                        column: x => x.SupplierID,
                        principalTable: "Suppliers",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseStockChangeHistory",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WarehouseID = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    ReasonId = table.Column<long>(type: "bigint", nullable: false),
                    UnitNum = table.Column<int>(type: "integer", nullable: false),
                    BeforeBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    AfterBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    ReasonType = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseStockChangeHistory", x => x.ID);
                    table.ForeignKey(
                        name: "FK_WarehouseStockChangeHistory_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseStocks",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WarehouseID = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseStocks", x => x.ID);
                    table.ForeignKey(
                        name: "FK_WarehouseStocks_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    AccountID = table.Column<long>(type: "bigint", nullable: false),
                    ItemID = table.Column<long>(type: "bigint", nullable: false),
                    Target = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_AccountItems_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CategoryPrinterChannel",
                columns: table => new
                {
                    CategoriesID = table.Column<long>(type: "bigint", nullable: false),
                    PrinterChannelsID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryPrinterChannel", x => new { x.CategoriesID, x.PrinterChannelsID });
                    table.ForeignKey(
                        name: "FK_CategoryPrinterChannel_Categories_CategoriesID",
                        column: x => x.CategoriesID,
                        principalTable: "Categories",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryPrinterChannel_PrinterChannels_PrinterChannelsID",
                        column: x => x.PrinterChannelsID,
                        principalTable: "PrinterChannels",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CategoryPropina",
                columns: table => new
                {
                    CategoriesID = table.Column<long>(type: "bigint", nullable: false),
                    PropinasID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryPropina", x => new { x.CategoriesID, x.PropinasID });
                    table.ForeignKey(
                        name: "FK_CategoryPropina_Categories_CategoriesID",
                        column: x => x.CategoriesID,
                        principalTable: "Categories",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryPropina_Propinas_PropinasID",
                        column: x => x.PropinasID,
                        principalTable: "Propinas",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CategoryQuestion",
                columns: table => new
                {
                    CategoriesID = table.Column<long>(type: "bigint", nullable: false),
                    QuestionsID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryQuestion", x => new { x.CategoriesID, x.QuestionsID });
                    table.ForeignKey(
                        name: "FK_CategoryQuestion_Categories_CategoriesID",
                        column: x => x.CategoriesID,
                        principalTable: "Categories",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryQuestion_Questions_QuestionsID",
                        column: x => x.QuestionsID,
                        principalTable: "Questions",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AreaStation",
                columns: table => new
                {
                    AreasID = table.Column<long>(type: "bigint", nullable: false),
                    StationsID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaStation", x => new { x.AreasID, x.StationsID });
                    table.ForeignKey(
                        name: "FK_AreaStation_Areas_AreasID",
                        column: x => x.AreasID,
                        principalTable: "Areas",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AreaStation_Stations_StationsID",
                        column: x => x.StationsID,
                        principalTable: "Stations",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    Tax = table.Column<decimal>(type: "numeric", nullable: false),
                    Propina = table.Column<decimal>(type: "numeric", nullable: false),
                    Tip = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount = table.Column<decimal>(type: "numeric", nullable: false),
                    Delivery = table.Column<decimal>(type: "numeric", nullable: true),
                    StationID = table.Column<long>(type: "bigint", nullable: false),
                    AreaID = table.Column<long>(type: "bigint", nullable: true),
                    TableID = table.Column<long>(type: "bigint", nullable: true),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentStatus = table.Column<int>(type: "integer", nullable: false),
                    PayAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ClientName = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    ConduceOrderId = table.Column<long>(type: "bigint", nullable: true),
                    WaiterName = table.Column<string>(type: "text", nullable: true),
                    IsDivider = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    Terms = table.Column<string>(type: "text", nullable: true),
                    Person = table.Column<int>(type: "integer", nullable: false),
                    OrderTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OrderMode = table.Column<int>(type: "integer", nullable: false),
                    ComprobantesID = table.Column<long>(type: "bigint", nullable: false),
                    ComprobanteNumber = table.Column<string>(type: "text", nullable: true),
                    ComprobanteName = table.Column<string>(type: "text", nullable: true),
                    PrepareTypeID = table.Column<long>(type: "bigint", nullable: true),
                    PromesaPago = table.Column<decimal>(type: "numeric", nullable: false),
                    Factura = table.Column<long>(type: "bigint", nullable: true),
                    IsConduce = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Orders_AreaObjects_TableID",
                        column: x => x.TableID,
                        principalTable: "AreaObjects",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Orders_Areas_AreaID",
                        column: x => x.AreaID,
                        principalTable: "Areas",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Orders_PrepareTypes_PrepareTypeID",
                        column: x => x.PrepareTypeID,
                        principalTable: "PrepareTypes",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Orders_Stations_StationID",
                        column: x => x.StationID,
                        principalTable: "Stations",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StationPrinterChannel",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StationID = table.Column<long>(type: "bigint", nullable: false),
                    PrinterChannelID = table.Column<long>(type: "bigint", nullable: false),
                    PrinterID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationPrinterChannel", x => x.ID);
                    table.ForeignKey(
                        name: "FK_StationPrinterChannel_PrinterChannels_PrinterChannelID",
                        column: x => x.PrinterChannelID,
                        principalTable: "PrinterChannels",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StationPrinterChannel_Printers_PrinterID",
                        column: x => x.PrinterID,
                        principalTable: "Printers",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_StationPrinterChannel_Stations_StationID",
                        column: x => x.StationID,
                        principalTable: "Stations",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CategoryTax",
                columns: table => new
                {
                    CategoriesID = table.Column<long>(type: "bigint", nullable: false),
                    TaxsID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryTax", x => new { x.CategoriesID, x.TaxsID });
                    table.ForeignKey(
                        name: "FK_CategoryTax_Categories_CategoriesID",
                        column: x => x.CategoriesID,
                        principalTable: "Categories",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryTax_Taxs_TaxsID",
                        column: x => x.TaxsID,
                        principalTable: "Taxs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MoveArticleItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemID = table.Column<long>(type: "bigint", nullable: false),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitNum = table.Column<int>(type: "integer", nullable: false),
                    MoveArticleID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoveArticleItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MoveArticleItems_MoveArticles_MoveArticleID",
                        column: x => x.MoveArticleID,
                        principalTable: "MoveArticles",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Deliverys",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StatusUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Address1 = table.Column<string>(type: "text", nullable: true),
                    Adress2 = table.Column<string>(type: "text", nullable: true),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    DeliveryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OrderID = table.Column<long>(type: "bigint", nullable: true),
                    DeliveryCarrierID = table.Column<long>(type: "bigint", nullable: true),
                    DeliveryZoneID = table.Column<long>(type: "bigint", nullable: true),
                    CustomerID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deliverys", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Deliverys_Customers_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "Customers",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Deliverys_DeliveryCarriers_DeliveryCarrierID",
                        column: x => x.DeliveryCarrierID,
                        principalTable: "DeliveryCarriers",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Deliverys_DeliveryZones_DeliveryZoneID",
                        column: x => x.DeliveryZoneID,
                        principalTable: "DeliveryZones",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Deliverys_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "DividerItem",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DividerNum = table.Column<int>(type: "integer", nullable: false),
                    ClientName = table.Column<string>(type: "text", nullable: true),
                    ClientId = table.Column<long>(type: "bigint", nullable: false),
                    ComprebanteId = table.Column<long>(type: "bigint", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    OrderID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DividerItem", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DividerItem_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "OrderTransactions",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    OrderID = table.Column<long>(type: "bigint", nullable: true),
                    Method = table.Column<string>(type: "text", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    BeforeBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    AfterBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    Difference = table.Column<decimal>(type: "numeric", nullable: false),
                    ReferenceId = table.Column<long>(type: "bigint", nullable: false),
                    Memo = table.Column<string>(type: "text", nullable: true),
                    SeatNum = table.Column<int>(type: "integer", nullable: false),
                    DividerNum = table.Column<int>(type: "integer", nullable: false),
                    PaymentType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTransactions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OrderTransactions_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "SeatItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeatNum = table.Column<int>(type: "integer", nullable: false),
                    ClientName = table.Column<string>(type: "text", nullable: true),
                    ClientId = table.Column<long>(type: "bigint", nullable: false),
                    ComprebanteId = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SeatItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Answers",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    PriceType = table.Column<int>(type: "integer", nullable: false),
                    FixedPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    RollPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    QuestionID = table.Column<long>(type: "bigint", nullable: false),
                    ForcedQuestionID = table.Column<int>(type: "integer", nullable: false),
                    IsPreSelected = table.Column<bool>(type: "boolean", nullable: false),
                    HasQty = table.Column<bool>(type: "boolean", nullable: false),
                    MatchSize = table.Column<bool>(type: "boolean", nullable: false),
                    HasDivisor = table.Column<bool>(type: "boolean", nullable: false),
                    ServingSizeID = table.Column<int>(type: "integer", nullable: false),
                    Comentario = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Answers", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Answers_Questions_QuestionID",
                        column: x => x.QuestionID,
                        principalTable: "Questions",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CategoryID = table.Column<long>(type: "bigint", nullable: true),
                    SubCategoryID = table.Column<long>(type: "bigint", nullable: true),
                    WarehouseID = table.Column<long>(type: "bigint", nullable: true),
                    TaxID = table.Column<long>(type: "bigint", nullable: true),
                    Performance = table.Column<double>(type: "double precision", nullable: false),
                    BrandID = table.Column<long>(type: "bigint", nullable: true),
                    MinimumQuantity = table.Column<double>(type: "double precision", nullable: false),
                    MinimumUnit = table.Column<int>(type: "integer", nullable: false),
                    MaximumQuantity = table.Column<double>(type: "double precision", nullable: false),
                    MaximumUnit = table.Column<int>(type: "integer", nullable: false),
                    DefaultUnitNum = table.Column<int>(type: "integer", nullable: false),
                    ScannerUnit = table.Column<int>(type: "integer", nullable: false),
                    Photo = table.Column<string>(type: "text", nullable: true),
                    Barcode = table.Column<string>(type: "text", nullable: true),
                    PrimarySupplier = table.Column<long>(type: "bigint", nullable: false),
                    SaftyStock = table.Column<int>(type: "integer", nullable: false),
                    DepleteCondition = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Articles_Brands_BrandID",
                        column: x => x.BrandID,
                        principalTable: "Brands",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Articles_Categories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Articles_Taxs_TaxID",
                        column: x => x.TaxID,
                        principalTable: "Taxs",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Articles_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "InventoryItemSupplier",
                columns: table => new
                {
                    ArticlesID = table.Column<long>(type: "bigint", nullable: false),
                    SuppliersID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItemSupplier", x => new { x.ArticlesID, x.SuppliersID });
                    table.ForeignKey(
                        name: "FK_InventoryItemSupplier_Articles_ArticlesID",
                        column: x => x.ArticlesID,
                        principalTable: "Articles",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryItemSupplier_Suppliers_SuppliersID",
                        column: x => x.SuppliersID,
                        principalTable: "Suppliers",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseOrderID = table.Column<long>(type: "bigint", nullable: false),
                    ItemID = table.Column<long>(type: "bigint", nullable: false),
                    UnitNum = table.Column<int>(type: "integer", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxID = table.Column<long>(type: "bigint", nullable: true),
                    TaxRate = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_Articles_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Articles",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderID",
                        column: x => x.PurchaseOrderID,
                        principalTable: "PurchaseOrders",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_Taxs_TaxID",
                        column: x => x.TaxID,
                        principalTable: "Taxs",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "CanceledItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReasonID = table.Column<long>(type: "bigint", nullable: false),
                    ItemID = table.Column<long>(type: "bigint", nullable: false),
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanceledItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CanceledItems_CancelReasons_ReasonID",
                        column: x => x.ReasonID,
                        principalTable: "CancelReasons",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DamageArticles",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ArticleID = table.Column<long>(type: "bigint", nullable: true),
                    SubRecipeID = table.Column<long>(type: "bigint", nullable: true),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    UnitNum = table.Column<int>(type: "integer", nullable: false),
                    UnitName = table.Column<string>(type: "text", nullable: true),
                    Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    WarehouseID = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DamageDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DamageArticles", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DamageArticles_Articles_ArticleID",
                        column: x => x.ArticleID,
                        principalTable: "Articles",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_DamageArticles_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Medias",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    URL = table.Column<string>(type: "text", nullable: true),
                    Src = table.Column<string>(type: "text", nullable: true),
                    IsURL = table.Column<bool>(type: "boolean", nullable: false),
                    ItemID = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    DestEntity = table.Column<int>(type: "integer", nullable: false),
                    DamageArticleID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medias", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Medias_DamageArticles_DamageArticleID",
                        column: x => x.DamageArticleID,
                        principalTable: "DamageArticles",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "MenuCategories",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    BackColor = table.Column<string>(type: "text", nullable: false),
                    TextColor = table.Column<string>(type: "text", nullable: false),
                    MediaID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuCategories", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MenuCategories_Medias_MediaID",
                        column: x => x.MediaID,
                        principalTable: "Medias",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "MenuGroups",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    BackColor = table.Column<string>(type: "text", nullable: false),
                    TextColor = table.Column<string>(type: "text", nullable: false),
                    MediaID = table.Column<long>(type: "bigint", nullable: true),
                    MenuID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuGroups", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MenuGroups_Medias_MediaID",
                        column: x => x.MediaID,
                        principalTable: "Medias",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_MenuGroups_Menus_MenuID",
                        column: x => x.MenuID,
                        principalTable: "Menus",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "MenuSubCategoris",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    BackColor = table.Column<string>(type: "text", nullable: false),
                    TextColor = table.Column<string>(type: "text", nullable: false),
                    MediaID = table.Column<long>(type: "bigint", nullable: true),
                    MenuCategoryID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuSubCategoris", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MenuSubCategoris_Medias_MediaID",
                        column: x => x.MediaID,
                        principalTable: "Medias",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_MenuSubCategoris_MenuCategories_MenuCategoryID",
                        column: x => x.MenuCategoryID,
                        principalTable: "MenuCategories",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "MenuCategoryMenuGroup",
                columns: table => new
                {
                    CategoriesID = table.Column<long>(type: "bigint", nullable: false),
                    GroupsID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuCategoryMenuGroup", x => new { x.CategoriesID, x.GroupsID });
                    table.ForeignKey(
                        name: "FK_MenuCategoryMenuGroup_MenuCategories_CategoriesID",
                        column: x => x.CategoriesID,
                        principalTable: "MenuCategories",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MenuCategoryMenuGroup_MenuGroups_GroupsID",
                        column: x => x.GroupsID,
                        principalTable: "MenuGroups",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiscountItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TargetType = table.Column<int>(type: "integer", nullable: false),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    AmountType = table.Column<int>(type: "integer", nullable: false),
                    ItemID = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    DividerId = table.Column<int>(type: "integer", nullable: false),
                    OrderID = table.Column<long>(type: "bigint", nullable: true),
                    OrderItemID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DiscountItems_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "ItemUnits",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric", nullable: false),
                    CodeBar = table.Column<string>(type: "text", nullable: true),
                    PayItem = table.Column<decimal>(type: "numeric", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitID = table.Column<long>(type: "bigint", nullable: false),
                    InventoryItemID = table.Column<long>(type: "bigint", nullable: true),
                    SubRecipeID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemUnits", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ItemUnits_Articles_InventoryItemID",
                        column: x => x.InventoryItemID,
                        principalTable: "Articles",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_ItemUnits_Units_UnitID",
                        column: x => x.UnitID,
                        principalTable: "Units",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuProducts",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroupID = table.Column<long>(type: "bigint", nullable: false),
                    CategoryID = table.Column<long>(type: "bigint", nullable: false),
                    SubCategoryID = table.Column<long>(type: "bigint", nullable: false),
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    MenuID = table.Column<long>(type: "bigint", nullable: false),
                    MenuCategoryID = table.Column<long>(type: "bigint", nullable: true),
                    MenuGroupID = table.Column<long>(type: "bigint", nullable: true),
                    MenuSubCategoryID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuProducts", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MenuProducts_MenuCategories_MenuCategoryID",
                        column: x => x.MenuCategoryID,
                        principalTable: "MenuCategories",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_MenuProducts_MenuGroups_MenuGroupID",
                        column: x => x.MenuGroupID,
                        principalTable: "MenuGroups",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_MenuProducts_MenuSubCategoris_MenuSubCategoryID",
                        column: x => x.MenuSubCategoryID,
                        principalTable: "MenuSubCategoris",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderID = table.Column<long>(type: "bigint", nullable: false),
                    ProductID = table.Column<long>(type: "bigint", nullable: false),
                    MenuProductID = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SeatNum = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount = table.Column<decimal>(type: "numeric", nullable: false),
                    Tax = table.Column<decimal>(type: "numeric", nullable: false),
                    Propina = table.Column<decimal>(type: "numeric", nullable: false),
                    HoldTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DividerNum = table.Column<int>(type: "integer", nullable: false),
                    HasPromotion = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    Course = table.Column<string>(type: "text", nullable: true),
                    CourseID = table.Column<long>(type: "bigint", nullable: false),
                    ServingSizeID = table.Column<int>(type: "integer", nullable: false),
                    ServingSizeName = table.Column<string>(type: "text", nullable: true),
                    Costo = table.Column<decimal>(type: "numeric", nullable: false),
                    AnswerCosto = table.Column<decimal>(type: "numeric", nullable: false),
                    AnswerVenta = table.Column<decimal>(type: "numeric", nullable: false),
                    SeatItemID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_SeatItems_SeatItemID",
                        column: x => x.SeatItemID,
                        principalTable: "SeatItems",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "PropinaItem",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PropinaId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Percent = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    IsExempt = table.Column<bool>(type: "boolean", nullable: false),
                    ToGoExclude = table.Column<bool>(type: "boolean", nullable: false),
                    BarcodeExclude = table.Column<bool>(type: "boolean", nullable: false),
                    OrderID = table.Column<long>(type: "bigint", nullable: true),
                    OrderItemID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropinaItem", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PropinaItem_OrderItems_OrderItemID",
                        column: x => x.OrderItemID,
                        principalTable: "OrderItems",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_PropinaItem_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "QuestionItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnswerID = table.Column<long>(type: "bigint", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CanRoll = table.Column<bool>(type: "boolean", nullable: false),
                    ServingSizeID = table.Column<int>(type: "integer", nullable: false),
                    ServingSizeName = table.Column<string>(type: "text", nullable: true),
                    Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    Divisor = table.Column<int>(type: "integer", nullable: false),
                    SubDescription = table.Column<string>(type: "text", nullable: true),
                    SubPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    IsPreSelect = table.Column<bool>(type: "boolean", nullable: false),
                    FreeChoice = table.Column<int>(type: "integer", nullable: false),
                    OrderItemID = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_QuestionItems_Answers_AnswerID",
                        column: x => x.AnswerID,
                        principalTable: "Answers",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionItems_OrderItems_OrderItemID",
                        column: x => x.OrderItemID,
                        principalTable: "OrderItems",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxItem",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TaxId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Percent = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    IsExempt = table.Column<bool>(type: "boolean", nullable: false),
                    ToGoExclude = table.Column<bool>(type: "boolean", nullable: false),
                    BarcodeExclude = table.Column<bool>(type: "boolean", nullable: false),
                    OrderID = table.Column<long>(type: "bigint", nullable: true),
                    OrderItemID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxItem", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TaxItem_OrderItems_OrderItemID",
                        column: x => x.OrderItemID,
                        principalTable: "OrderItems",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_TaxItem_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "PrinterChannelProduct",
                columns: table => new
                {
                    PrinterChannelsID = table.Column<long>(type: "bigint", nullable: false),
                    ProductsID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrinterChannelProduct", x => new { x.PrinterChannelsID, x.ProductsID });
                    table.ForeignKey(
                        name: "FK_PrinterChannelProduct_PrinterChannels_PrinterChannelsID",
                        column: x => x.PrinterChannelsID,
                        principalTable: "PrinterChannels",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemID = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitNum = table.Column<int>(type: "integer", nullable: false),
                    ServingSizeID = table.Column<int>(type: "integer", nullable: false),
                    ProductID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductItems", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ProductPropina",
                columns: table => new
                {
                    ProductsID = table.Column<long>(type: "bigint", nullable: false),
                    PropinasID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPropina", x => new { x.ProductsID, x.PropinasID });
                    table.ForeignKey(
                        name: "FK_ProductPropina_Propinas_PropinasID",
                        column: x => x.PropinasID,
                        principalTable: "Propinas",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductQuestion",
                columns: table => new
                {
                    ProductsID = table.Column<long>(type: "bigint", nullable: false),
                    QuestionsID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductQuestion", x => new { x.ProductsID, x.QuestionsID });
                    table.ForeignKey(
                        name: "FK_ProductQuestion_Questions_QuestionsID",
                        column: x => x.QuestionsID,
                        principalTable: "Questions",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CategoryID = table.Column<long>(type: "bigint", nullable: false),
                    SubCategoryID = table.Column<long>(type: "bigint", nullable: true),
                    Printer = table.Column<string>(type: "text", nullable: true),
                    Barcode = table.Column<string>(type: "text", nullable: true),
                    BackColor = table.Column<string>(type: "text", nullable: true),
                    TextColor = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<List<decimal>>(type: "numeric[]", nullable: false),
                    ProductCost = table.Column<decimal>(type: "numeric", nullable: false),
                    Photo = table.Column<string>(type: "text", nullable: true),
                    InventoryCount = table.Column<decimal>(type: "numeric", nullable: false),
                    InventoryCountDownActive = table.Column<bool>(type: "boolean", nullable: false),
                    CourseID = table.Column<long>(type: "bigint", nullable: false),
                    HasServingSize = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductServingSize",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServingSizeID = table.Column<long>(type: "bigint", nullable: false),
                    ServingSizeName = table.Column<string>(type: "text", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<List<decimal>>(type: "numeric[]", nullable: false),
                    ProductID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductServingSize", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ProductServingSize_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "ProductTax",
                columns: table => new
                {
                    ProductsID = table.Column<long>(type: "bigint", nullable: false),
                    TaxesID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTax", x => new { x.ProductsID, x.TaxesID });
                    table.ForeignKey(
                        name: "FK_ProductTax_Products_ProductsID",
                        column: x => x.ProductsID,
                        principalTable: "Products",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductTax_Taxs_TaxesID",
                        column: x => x.TaxesID,
                        principalTable: "Taxs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubCategories",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CategoryID = table.Column<long>(type: "bigint", nullable: false),
                    ProductID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubCategories", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SubCategories_Categories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubCategories_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "SubRecipes",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CategoryID = table.Column<long>(type: "bigint", nullable: true),
                    SubCategoryID = table.Column<long>(type: "bigint", nullable: true),
                    MinimumQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    MinimumUnit = table.Column<int>(type: "integer", nullable: false),
                    MaximumQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    MaximumUnit = table.Column<int>(type: "integer", nullable: false),
                    Barcode = table.Column<string>(type: "text", nullable: true),
                    SaftyStock = table.Column<int>(type: "integer", nullable: false),
                    UnitNumber = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubRecipes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SubRecipes_Categories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_SubRecipes_SubCategories_SubCategoryID",
                        column: x => x.SubCategoryID,
                        principalTable: "SubCategories",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "SubRecipeItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemID = table.Column<long>(type: "bigint", nullable: false),
                    IsArticle = table.Column<bool>(type: "boolean", nullable: false),
                    FirstQty = table.Column<decimal>(type: "numeric", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitNum = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric", nullable: false),
                    ItemCost = table.Column<decimal>(type: "numeric", nullable: false),
                    SubRecipeID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubRecipeItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SubRecipeItems_SubRecipes_SubRecipeID",
                        column: x => x.SubRecipeID,
                        principalTable: "SubRecipes",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "SubRecipeProductions",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WarehouseID = table.Column<long>(type: "bigint", nullable: false),
                    ProductionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubRecipeID = table.Column<long>(type: "bigint", nullable: false),
                    UnitNum = table.Column<int>(type: "integer", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    EndQty = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubRecipeProductions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SubRecipeProductions_SubRecipes_SubRecipeID",
                        column: x => x.SubRecipeID,
                        principalTable: "SubRecipes",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubRecipeProductions_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountItems_AccountID",
                table: "AccountItems",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountDescriptionID",
                table: "Accounts",
                column: "AccountDescriptionID");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountTypeID",
                table: "Accounts",
                column: "AccountTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_ProductID",
                table: "Answers",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_QuestionID",
                table: "Answers",
                column: "QuestionID");

            migrationBuilder.CreateIndex(
                name: "IX_AreaObjects_AreaID",
                table: "AreaObjects",
                column: "AreaID");

            migrationBuilder.CreateIndex(
                name: "IX_AreaStation_StationsID",
                table: "AreaStation",
                column: "StationsID");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_BrandID",
                table: "Articles",
                column: "BrandID");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_CategoryID",
                table: "Articles",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_SubCategoryID",
                table: "Articles",
                column: "SubCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_TaxID",
                table: "Articles",
                column: "TaxID");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_WarehouseID",
                table: "Articles",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_CanceledItems_ItemID",
                table: "CanceledItems",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_CanceledItems_ProductID",
                table: "CanceledItems",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_CanceledItems_ReasonID",
                table: "CanceledItems",
                column: "ReasonID");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_GroupID",
                table: "Categories",
                column: "GroupID");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryPrinterChannel_PrinterChannelsID",
                table: "CategoryPrinterChannel",
                column: "PrinterChannelsID");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryPropina_PropinasID",
                table: "CategoryPropina",
                column: "PropinasID");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryQuestion_QuestionsID",
                table: "CategoryQuestion",
                column: "QuestionsID");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryTax_TaxsID",
                table: "CategoryTax",
                column: "TaxsID");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_VoucherID",
                table: "Customers",
                column: "VoucherID");

            migrationBuilder.CreateIndex(
                name: "IX_DamageArticles_ArticleID",
                table: "DamageArticles",
                column: "ArticleID");

            migrationBuilder.CreateIndex(
                name: "IX_DamageArticles_SubRecipeID",
                table: "DamageArticles",
                column: "SubRecipeID");

            migrationBuilder.CreateIndex(
                name: "IX_DamageArticles_WarehouseID",
                table: "DamageArticles",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_Deliverys_CustomerID",
                table: "Deliverys",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_Deliverys_DeliveryCarrierID",
                table: "Deliverys",
                column: "DeliveryCarrierID");

            migrationBuilder.CreateIndex(
                name: "IX_Deliverys_DeliveryZoneID",
                table: "Deliverys",
                column: "DeliveryZoneID");

            migrationBuilder.CreateIndex(
                name: "IX_Deliverys_OrderID",
                table: "Deliverys",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountItems_OrderID",
                table: "DiscountItems",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountItems_OrderItemID",
                table: "DiscountItems",
                column: "OrderItemID");

            migrationBuilder.CreateIndex(
                name: "IX_DividerItem_OrderID",
                table: "DividerItem",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemSupplier_SuppliersID",
                table: "InventoryItemSupplier",
                column: "SuppliersID");

            migrationBuilder.CreateIndex(
                name: "IX_ItemUnits_InventoryItemID",
                table: "ItemUnits",
                column: "InventoryItemID");

            migrationBuilder.CreateIndex(
                name: "IX_ItemUnits_SubRecipeID",
                table: "ItemUnits",
                column: "SubRecipeID");

            migrationBuilder.CreateIndex(
                name: "IX_ItemUnits_UnitID",
                table: "ItemUnits",
                column: "UnitID");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_DamageArticleID",
                table: "Medias",
                column: "DamageArticleID");

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_MediaID",
                table: "MenuCategories",
                column: "MediaID");

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategoryMenuGroup_GroupsID",
                table: "MenuCategoryMenuGroup",
                column: "GroupsID");

            migrationBuilder.CreateIndex(
                name: "IX_MenuGroups_MediaID",
                table: "MenuGroups",
                column: "MediaID");

            migrationBuilder.CreateIndex(
                name: "IX_MenuGroups_MenuID",
                table: "MenuGroups",
                column: "MenuID");

            migrationBuilder.CreateIndex(
                name: "IX_MenuProducts_MenuCategoryID",
                table: "MenuProducts",
                column: "MenuCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_MenuProducts_MenuGroupID",
                table: "MenuProducts",
                column: "MenuGroupID");

            migrationBuilder.CreateIndex(
                name: "IX_MenuProducts_MenuSubCategoryID",
                table: "MenuProducts",
                column: "MenuSubCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_MenuProducts_ProductID",
                table: "MenuProducts",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_MenuSubCategoris_MediaID",
                table: "MenuSubCategoris",
                column: "MediaID");

            migrationBuilder.CreateIndex(
                name: "IX_MenuSubCategoris_MenuCategoryID",
                table: "MenuSubCategoris",
                column: "MenuCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_MoveArticleItems_MoveArticleID",
                table: "MoveArticleItems",
                column: "MoveArticleID");

            migrationBuilder.CreateIndex(
                name: "IX_MoveArticles_FromWarehouseID",
                table: "MoveArticles",
                column: "FromWarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_MoveArticles_ToWarehouseID",
                table: "MoveArticles",
                column: "ToWarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderID",
                table: "OrderItems",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductID",
                table: "OrderItems",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_SeatItemID",
                table: "OrderItems",
                column: "SeatItemID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AreaID",
                table: "Orders",
                column: "AreaID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PrepareTypeID",
                table: "Orders",
                column: "PrepareTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StationID",
                table: "Orders",
                column: "StationID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TableID",
                table: "Orders",
                column: "TableID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTransactions_OrderID",
                table: "OrderTransactions",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRole_RolesID",
                table: "PermissionRole",
                column: "RolesID");

            migrationBuilder.CreateIndex(
                name: "IX_PrinterChannelProduct_ProductsID",
                table: "PrinterChannelProduct",
                column: "ProductsID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductItems_ProductID",
                table: "ProductItems",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPropina_PropinasID",
                table: "ProductPropina",
                column: "PropinasID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductQuestion_QuestionsID",
                table: "ProductQuestion",
                column: "QuestionsID");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryID",
                table: "Products",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SubCategoryID",
                table: "Products",
                column: "SubCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductServingSize_ProductID",
                table: "ProductServingSize",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTax_TaxesID",
                table: "ProductTax",
                column: "TaxesID");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionTargets_PromotionID",
                table: "PromotionTargets",
                column: "PromotionID");

            migrationBuilder.CreateIndex(
                name: "IX_PropinaItem_OrderID",
                table: "PropinaItem",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_PropinaItem_OrderItemID",
                table: "PropinaItem",
                column: "OrderItemID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_ItemID",
                table: "PurchaseOrderItems",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_PurchaseOrderID",
                table: "PurchaseOrderItems",
                column: "PurchaseOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_TaxID",
                table: "PurchaseOrderItems",
                column: "TaxID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierID",
                table: "PurchaseOrders",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_WarehouseID",
                table: "PurchaseOrders",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionItems_AnswerID",
                table: "QuestionItems",
                column: "AnswerID");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionItems_OrderItemID",
                table: "QuestionItems",
                column: "OrderItemID");

            migrationBuilder.CreateIndex(
                name: "IX_RoleUser_UsersID",
                table: "RoleUser",
                column: "UsersID");

            migrationBuilder.CreateIndex(
                name: "IX_SeatItems_OrderId",
                table: "SeatItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartButtonItems_ButtonID",
                table: "SmartButtonItems",
                column: "ButtonID");

            migrationBuilder.CreateIndex(
                name: "IX_SmartButtonItems_QuestionID",
                table: "SmartButtonItems",
                column: "QuestionID");

            migrationBuilder.CreateIndex(
                name: "IX_StationPrinterChannel_PrinterChannelID",
                table: "StationPrinterChannel",
                column: "PrinterChannelID");

            migrationBuilder.CreateIndex(
                name: "IX_StationPrinterChannel_PrinterID",
                table: "StationPrinterChannel",
                column: "PrinterID");

            migrationBuilder.CreateIndex(
                name: "IX_StationPrinterChannel_StationID",
                table: "StationPrinterChannel",
                column: "StationID");

            migrationBuilder.CreateIndex(
                name: "IX_Stations_MenuSelectID",
                table: "Stations",
                column: "MenuSelectID");

            migrationBuilder.CreateIndex(
                name: "IX_SubCategories_CategoryID",
                table: "SubCategories",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_SubCategories_ProductID",
                table: "SubCategories",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_SubRecipeItems_SubRecipeID",
                table: "SubRecipeItems",
                column: "SubRecipeID");

            migrationBuilder.CreateIndex(
                name: "IX_SubRecipeProductions_SubRecipeID",
                table: "SubRecipeProductions",
                column: "SubRecipeID");

            migrationBuilder.CreateIndex(
                name: "IX_SubRecipeProductions_WarehouseID",
                table: "SubRecipeProductions",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_SubRecipes_CategoryID",
                table: "SubRecipes",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_SubRecipes_SubCategoryID",
                table: "SubRecipes",
                column: "SubCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_TaxItem_OrderID",
                table: "TaxItem",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_TaxItem_OrderItemID",
                table: "TaxItem",
                column: "OrderItemID");

            migrationBuilder.CreateIndex(
                name: "IX_Taxs_VoucherID",
                table: "Taxs",
                column: "VoucherID");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockChangeHistory_WarehouseID",
                table: "WarehouseStockChangeHistory",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_WarehouseID",
                table: "WarehouseStocks",
                column: "WarehouseID");

            migrationBuilder.AddForeignKey(
                name: "FK_Answers_Products_ProductID",
                table: "Answers",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_SubCategories_SubCategoryID",
                table: "Articles",
                column: "SubCategoryID",
                principalTable: "SubCategories",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_CanceledItems_OrderItems_ItemID",
                table: "CanceledItems",
                column: "ItemID",
                principalTable: "OrderItems",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CanceledItems_Products_ProductID",
                table: "CanceledItems",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DamageArticles_SubRecipes_SubRecipeID",
                table: "DamageArticles",
                column: "SubRecipeID",
                principalTable: "SubRecipes",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscountItems_OrderItems_OrderItemID",
                table: "DiscountItems",
                column: "OrderItemID",
                principalTable: "OrderItems",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemUnits_SubRecipes_SubRecipeID",
                table: "ItemUnits",
                column: "SubRecipeID",
                principalTable: "SubRecipes",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuProducts_Products_ProductID",
                table: "MenuProducts",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Products_ProductID",
                table: "OrderItems",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrinterChannelProduct_Products_ProductsID",
                table: "PrinterChannelProduct",
                column: "ProductsID",
                principalTable: "Products",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductItems_Products_ProductID",
                table: "ProductItems",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPropina_Products_ProductsID",
                table: "ProductPropina",
                column: "ProductsID",
                principalTable: "Products",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductQuestion_Products_ProductsID",
                table: "ProductQuestion",
                column: "ProductsID",
                principalTable: "Products",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_SubCategories_SubCategoryID",
                table: "Products",
                column: "SubCategoryID",
                principalTable: "SubCategories",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubCategories_Products_ProductID",
                table: "SubCategories");

            migrationBuilder.DropTable(
                name: "AccountItems");

            migrationBuilder.DropTable(
                name: "AreaStation");

            migrationBuilder.DropTable(
                name: "CanceledItems");

            migrationBuilder.DropTable(
                name: "CategoryPrinterChannel");

            migrationBuilder.DropTable(
                name: "CategoryPropina");

            migrationBuilder.DropTable(
                name: "CategoryQuestion");

            migrationBuilder.DropTable(
                name: "CategoryTax");

            migrationBuilder.DropTable(
                name: "CloseDrawers");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Deliverys");

            migrationBuilder.DropTable(
                name: "Denominations");

            migrationBuilder.DropTable(
                name: "DiscountItems");

            migrationBuilder.DropTable(
                name: "Discounts");

            migrationBuilder.DropTable(
                name: "DividerItem");

            migrationBuilder.DropTable(
                name: "ErrorOuts");

            migrationBuilder.DropTable(
                name: "InventoryItemSupplier");

            migrationBuilder.DropTable(
                name: "ItemUnits");

            migrationBuilder.DropTable(
                name: "Kitchen");

            migrationBuilder.DropTable(
                name: "KitchenOrder");

            migrationBuilder.DropTable(
                name: "KitchenOrderItem");

            migrationBuilder.DropTable(
                name: "logs");

            migrationBuilder.DropTable(
                name: "MenuCategoryMenuGroup");

            migrationBuilder.DropTable(
                name: "MenuProducts");

            migrationBuilder.DropTable(
                name: "MoveArticleItems");

            migrationBuilder.DropTable(
                name: "OrderComprobantes");

            migrationBuilder.DropTable(
                name: "OrderTransactions");

            migrationBuilder.DropTable(
                name: "PaymentMethods");

            migrationBuilder.DropTable(
                name: "PermissionRole");

            migrationBuilder.DropTable(
                name: "Preferences");

            migrationBuilder.DropTable(
                name: "PrinterChannelProduct");

            migrationBuilder.DropTable(
                name: "PrinterTasks");

            migrationBuilder.DropTable(
                name: "PrintJobs");

            migrationBuilder.DropTable(
                name: "ProductItems");

            migrationBuilder.DropTable(
                name: "ProductPropina");

            migrationBuilder.DropTable(
                name: "ProductQuestion");

            migrationBuilder.DropTable(
                name: "ProductServingSize");

            migrationBuilder.DropTable(
                name: "ProductTax");

            migrationBuilder.DropTable(
                name: "PromotionTargets");

            migrationBuilder.DropTable(
                name: "PropinaItem");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItems");

            migrationBuilder.DropTable(
                name: "QuestionItems");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "RoleUser");

            migrationBuilder.DropTable(
                name: "ServingSize");

            migrationBuilder.DropTable(
                name: "SmartButtonItems");

            migrationBuilder.DropTable(
                name: "StationPrinterChannel");

            migrationBuilder.DropTable(
                name: "StationWarehouses");

            migrationBuilder.DropTable(
                name: "SubRecipeItems");

            migrationBuilder.DropTable(
                name: "SubRecipeProductions");

            migrationBuilder.DropTable(
                name: "t_formato_impresion_general");

            migrationBuilder.DropTable(
                name: "t_formato_impresion_reportes");

            migrationBuilder.DropTable(
                name: "t_impresion");

            migrationBuilder.DropTable(
                name: "t_impresoras");

            migrationBuilder.DropTable(
                name: "t_sucursal");

            migrationBuilder.DropTable(
                name: "TaxItem");

            migrationBuilder.DropTable(
                name: "WarehouseStockChangeHistory");

            migrationBuilder.DropTable(
                name: "WarehouseStocks");

            migrationBuilder.DropTable(
                name: "WorkDay");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "CancelReasons");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "DeliveryCarriers");

            migrationBuilder.DropTable(
                name: "DeliveryZones");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "MenuGroups");

            migrationBuilder.DropTable(
                name: "MenuSubCategoris");

            migrationBuilder.DropTable(
                name: "MoveArticles");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Propinas");

            migrationBuilder.DropTable(
                name: "Promotions");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "Answers");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "SmartButtons");

            migrationBuilder.DropTable(
                name: "PrinterChannels");

            migrationBuilder.DropTable(
                name: "Printers");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "AccountDescriptions");

            migrationBuilder.DropTable(
                name: "AccountTypes");

            migrationBuilder.DropTable(
                name: "MenuCategories");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "SeatItems");

            migrationBuilder.DropTable(
                name: "Medias");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "DamageArticles");

            migrationBuilder.DropTable(
                name: "AreaObjects");

            migrationBuilder.DropTable(
                name: "PrepareTypes");

            migrationBuilder.DropTable(
                name: "Stations");

            migrationBuilder.DropTable(
                name: "Articles");

            migrationBuilder.DropTable(
                name: "SubRecipes");

            migrationBuilder.DropTable(
                name: "Areas");

            migrationBuilder.DropTable(
                name: "Menus");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "Taxs");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "Vouchers");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "SubCategories");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Groups");
        }
    }
}
