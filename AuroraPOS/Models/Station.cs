namespace AuroraPOS.Models
{
    public class Station : BaseEntity
    {
        public string Name { get; set; }
        public int PriceSelect { get; set; }
        public Menu? MenuSelect { get; set; }
        public string? TypeOfSales { get; set; }
        public OrderMode OrderMode { get; set; }
        public string? Bussiness { get; set; }
        public SalesMode SalesMode { get; set; }
        public int PrintCopy { get; set; }
        public int IDSucursal { get; set; }
        public string? AreaPrices { get; set; }
        public bool ImprimirPrecuentaDelivery { get; set; }
        //public Warehouse? Warehouse { get; set; }
        public List<Area>? Areas { get; set; }
        public List<Order>? Orders { get; set; }
        public List<StationPrinterChannel>? Printers { get; set; }
        public int? PrecioDelivery { get; set; }
        public int? PrepareTypeDefault { get; set; }
        
    }

    public class StationPrinterChannel : BaseEntity
    {
        public long StationID { get; set; }
        public PrinterChannel PrinterChannel { get; set; }
        public Printer? Printer { get; set; }
        
    }

    public enum SalesMode
    {
        Restaurant,
        Barcode,
        Kiosk
    }

	public class AreaModel
	{
		public long AreaID { get; set; }
		public int PriceSelect { get; set; }
		public int Order { get; set; }
	}

    public class StationWarehouse: BaseEntity
    {
        public long StationID { get; set; }
        public long GroupID { get; set; }
        public long WarehouseID { get; set; }
    }
}
