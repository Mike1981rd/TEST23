using AuroraPOS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection;

namespace AuroraPOS.Data
{
	public class AppDbContext : DbContext
	{
        private readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly IConfiguration Configuration;
		public string CurrentUser;
        public string DBForce;
        public AppDbContext(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
		{
			Configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public AppDbContext()
        {
            var configurationBuilder = new ConfigurationBuilder();
            var path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            configurationBuilder.AddJsonFile(path, false);

            Configuration = configurationBuilder.Build();
        }

        protected override void OnModelCreating(ModelBuilder builder)
		{
			builder.ApplyUtcDateTimeConverter();//Put before seed data and after model creation
		}
		public override int SaveChanges()
		{
			var entries = ChangeTracker
				.Entries()
				.Where(e => e.Entity is BaseEntity && (
				e.State == EntityState.Added
				|| e.State == EntityState.Modified));

			foreach (var entityEntry in entries)
			{
				((BaseEntity)entityEntry.Entity).UpdatedDate = DateTime.Now;
				((BaseEntity)entityEntry.Entity).UpdatedBy = CurrentUser;

                if (entityEntry.State == EntityState.Added)
				{
					((BaseEntity)entityEntry.Entity).CreatedDate = DateTime.Now;
                    ((BaseEntity)entityEntry.Entity).CreatedBy = CurrentUser;                    

                }


                if (((BaseEntity)entityEntry.Entity).ForceDate.HasValue)
                {
                    ((BaseEntity)entityEntry.Entity).UpdatedDate = ((BaseEntity)entityEntry.Entity).ForceDate.Value;
                    ((BaseEntity)entityEntry.Entity).CreatedDate = ((BaseEntity)entityEntry.Entity).ForceDate.Value;
                }

                if (!string.IsNullOrEmpty(((BaseEntity)entityEntry.Entity).ForceUpdatedBy))
                {
                    ((BaseEntity)entityEntry.Entity).UpdatedBy = ((BaseEntity)entityEntry.Entity).ForceUpdatedBy;                    
                }
            }
			return base.SaveChanges();
		}
		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
            string strDatabase = "AlfaPrimera";

            if (!string.IsNullOrEmpty(DBForce))
			{
				strDatabase = DBForce;

			}else
			{
                var cookieRequest = _httpContextAccessor.HttpContext.Request.Cookies["db"];
                string cookieResponse = GetCookieValueFromResponse(_httpContextAccessor.HttpContext.Response, "db");

                if (cookieResponse != null)
                {
                    strDatabase = cookieResponse;
                }
                else
                {
	                if (cookieRequest != null)
	                {
		                strDatabase = cookieRequest;		                
	                }
                }
            }			

            // connect to postgres with connection string from app settings
            options.UseNpgsql(Configuration.GetConnectionString(strDatabase));
		}

        string GetCookieValueFromResponse(HttpResponse response, string cookieName)
        {
            foreach (var headers in response.Headers.Values)
                foreach (var header in headers)
                    if (header.StartsWith($"{cookieName}="))
                    {
                        var p1 = header.IndexOf('=');
                        var p2 = header.IndexOf(';');
                        return header.Substring(p1 + 1, p2 - p1 - 1);
                    }
            return null;
        }

        public DbSet<User> User { get; set; }
		public DbSet<Role> Role { get; set; }
		public DbSet<Tax> Taxs { get; set; }
		public DbSet<Warehouse> Warehouses { get; set; }
		public DbSet<Group> Groups { get; set; }
		public DbSet<Supplier> Suppliers { get; set; }
		public DbSet<Printer> Printers { get; set; }
		public DbSet<PrinterTasks> PrinterTasks { get; set; }
		public DbSet<PrinterChannel> PrinterChannels { get; set; }
		public DbSet<Category> Categories { get; set; }
		public DbSet<SubCategory> SubCategories { get; set; }
		public DbSet<Brand> Brands { get; set; }
		public DbSet<Unit> Units { get; set; }
		public DbSet<ItemUnit> ItemUnits { get; set; }
		public DbSet<InventoryItem> Articles { get; set; }
		public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
		public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
		public DbSet<SubRecipe> SubRecipes { get; set; }
		public DbSet<SubRecipeItem> SubRecipeItems { get; set; }
		public DbSet<WarehouseStock> WarehouseStocks { get; set; }
		public DbSet<SubRecipeProduction> SubRecipeProductions { get; set; }
		public DbSet<WarehouseStockChangeHistory> WarehouseStockChangeHistory { get; set; }
		public DbSet<MoveArticle> MoveArticles { get; set; }
		public DbSet<MoveItem> MoveArticleItems { get; set; }
		public DbSet<GeneralMedia> Medias { get; set; }
		public DbSet<DamageArticle> DamageArticles { get; set; }
		public DbSet<Product> Products { get; set; }
		public DbSet<ProductRecipeItem> ProductItems { get; set; }
		public DbSet<Menu> Menus { get; set; }
		public DbSet<MenuGroup> MenuGroups { get; set; }
		public DbSet<MenuCategory> MenuCategories { get; set; }
		public DbSet<MenuSubCategory> MenuSubCategoris { get; set; }
		public DbSet<MenuProduct> MenuProducts { get; set; }
		public DbSet<Question> Questions { get; set; }
		public DbSet<Answer> Answers { get; set; }
		public DbSet<SmartButton> SmartButtons { get; set; }
		public DbSet<SmartButtonItem> SmartButtonItems { get; set; }
		public DbSet<Area> Areas { get; set; }
		public DbSet<AreaObject> AreaObjects { get; set; }
		public DbSet<Promotion> Promotions { get; set; }
		public DbSet<PromotionTarget> PromotionTargets { get; set; }
		public DbSet<Discount> Discounts { get; set; }
		public DbSet<DiscountItem> DiscountItems { get; set; }
		public DbSet<OrderItem> OrderItems { get; set; }
		public DbSet<Order> Orders { get; set; }
		public DbSet<OrderTransaction> OrderTransactions { get; set; }
		public DbSet<PaymentMethod> PaymentMethods { get; set; }
		public DbSet<QuestionItem> QuestionItems { get; set; }
		public DbSet<SeatItem> SeatItems { get; set; }
		public DbSet<CanceledOrderItem> CanceledItems { get; set; }
		public DbSet<CancelReason> CancelReasons { get; set; }
		public DbSet<Station> Stations { get; set; }
		public DbSet<StationPrinterChannel> StationPrinterChannel { get; set; }
		public DbSet<Customer> Customers { get; set; }
		public DbSet<PrepareTypes> PrepareTypes { get; set; }
		public DbSet<Preference> Preferences { get; set; }
		public DbSet<Denomination> Denominations { get; set; }
		public DbSet<Voucher> Vouchers { get; set; }
		public DbSet<Permission> Permissions { get; set; }
        public DbSet<PrintJob> PrintJobs { get; set; }

        public DbSet<DeliveryCarrier> DeliveryCarriers { get; set; }
        public DbSet<DeliveryZone> DeliveryZones { get; set; }
        public DbSet<Delivery> Deliverys { get; set; }
        public DbSet<t_impresion> t_impresion { get; set; }
        public DbSet<t_formato_impresion_general> t_formato_impresion_general { get; set; }
        public DbSet<t_formato_impresion_reportes> t_formato_impresion_reportes { get; set; }
        public DbSet<t_impresoras> t_impresoras { get; set; }
        public DbSet<logs> logs { get; set; }
        public DbSet<t_sucursal> t_sucursal { get; set; }
		public DbSet<CloseDrawer> CloseDrawers { get; set; }
		public DbSet<OrderComprobante> OrderComprobantes { get; set; }
		public DbSet<Propina> Propinas { get; set; }
		public DbSet<PropinaItem> PropinaItem { get; set; }
        public DbSet<WorkDay> WorkDay { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Kitchen> Kitchen { get; set; }
        public DbSet<KitchenOrder> KitchenOrder { get; set; }
        public DbSet<KitchenOrderItem> KitchenOrderItem { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<ServingSize> ServingSize { get; set; }
        public DbSet<ProductServingSize> ProductServingSize { get; set; }
        public DbSet<AccountDescription> AccountDescriptions { get; set; }
        public DbSet<AccountType> AccountTypes { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<AccountItem> AccountItems { get; set; }
		public DbSet<StationWarehouse> StationWarehouses { get; set; }
		public DbSet<ErrorOut> ErrorOuts { get; set; }
	}

	public static class UtcDateAnnotation
	{
		private const string IsUtcAnnotation = "IsUtc";
		private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new ValueConverter<DateTime, DateTime>(convertTo => DateTime.SpecifyKind(convertTo, DateTimeKind.Utc), convertFrom => convertFrom);

		public static PropertyBuilder<TProperty> IsUtc<TProperty>(this PropertyBuilder<TProperty> builder, bool isUtc = true) => builder.HasAnnotation(IsUtcAnnotation, isUtc);

		public static bool IsUtc(this IMutableProperty property)
		{
			if (property != null && property.PropertyInfo != null)
			{
				var attribute = property.PropertyInfo.GetCustomAttribute<IsUtcAttribute>();
				if (attribute is not null && attribute.IsUtc)
				{
					return true;
				}

				return ((bool?)property.FindAnnotation(IsUtcAnnotation)?.Value) ?? true;
			}
			return true;
		}

		/// <summary>
		/// Make sure this is called after configuring all your entities.
		/// </summary>
		public static void ApplyUtcDateTimeConverter(this ModelBuilder builder)
		{
			foreach (var entityType in builder.Model.GetEntityTypes())
			{
				foreach (var property in entityType.GetProperties())
				{
					if (!property.IsUtc())
					{
						continue;
					}

					if (property.ClrType == typeof(DateTime) ||
						property.ClrType == typeof(DateTime?))
					{
						property.SetValueConverter(UtcConverter);
					}
				}
			}
		}
	}
	public class IsUtcAttribute : Attribute
	{
		public IsUtcAttribute(bool isUtc = true) => this.IsUtc = isUtc;
		public bool IsUtc { get; }
	}
}
