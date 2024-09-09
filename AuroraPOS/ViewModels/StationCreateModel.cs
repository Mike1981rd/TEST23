using AuroraPOS.Models;

namespace AuroraPOS.ViewModels
{
	public class StationCreateModel : Station
	{
		public List<AreaModel> Areas { get; set; }
		public long MenuId { get; set; }
		public long WarehouseId { get; set; }
		public List<StationPrinterCreateModel> PrinterChannels { get; set; }
		public List<StationWarehouseCreateModel> GroupWarehouses { get; set; }
	}


	public class StationPrinterCreateModel
	{
		public long PrinterChannelID { get; set; }
		public long PrinterID { get; set; }
	}

	public class StationWarehouseCreateModel
	{
		public long GroupID { get; set; }
		public long WarehouseID { get; set; }
	}
}
