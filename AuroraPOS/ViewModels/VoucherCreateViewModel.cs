using AuroraPOS.Models;

namespace AuroraPOS.ViewModels
{
	public class VoucherCreateViewModel : Voucher
	{
		public List<long> TaxeIDs { get; set; }
	}
}
