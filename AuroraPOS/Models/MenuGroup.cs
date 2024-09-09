namespace AuroraPOS.Models
{
	public class Menu : BaseEntity 
	{
		public string Name { get; set; }
		public string? Description { get; set; }
		public MenuMode Mode { get; set; }
		public ICollection<MenuGroup>? Groups { get; set; }
    }

	public class MenuGroup : BaseEntity
	{
		public int Order { get; set; }
		public string Name { get; set; }
		public string BackColor { get; set; }
		public string TextColor { get; set; }
		public GeneralMedia? Media { get; set; }
		public ICollection<MenuCategory>? Categories { get; set; }
		public ICollection<MenuProduct>? Products { get; set; }
	}

	public class MenuCategory : BaseEntity
	{
        public int Order { get; set; }
		public string Name { get; set; }
		public string BackColor { get; set; }
		public string TextColor { get; set; }
		public GeneralMedia? Media { get; set; }
        
        public ICollection<MenuSubCategory>? SubCategories { get; set; }
        public virtual ICollection<MenuGroup>? Groups { get; set; }
        public ICollection<MenuProduct>? Products { get; set; }
    }

	public class MenuSubCategory : BaseEntity
	{
        public int Order { get; set; }
        public string Name { get; set; }
		public string BackColor { get; set; }
		public string TextColor { get; set; }
		public GeneralMedia? Media { get; set; }
        public ICollection<MenuProduct>? Products { get; set; }        
    }

	public class MenuProduct : BaseEntity
	{
		public long GroupID { get; set; }
		public long CategoryID { get; set; }
		public long SubCategoryID { get; set; }
        public Product Product { get; set; }
		public int Order { get; set; }
		public long MenuID { get; set; }
	}

	public enum MenuMode
	{
		Restaurant,
		Kiosk
	}
}
