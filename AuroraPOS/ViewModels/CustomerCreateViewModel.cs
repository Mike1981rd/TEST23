using AuroraPOS.Models;

namespace AuroraPOS.ViewModels
{
	public class CustomerCreateViewModel : Customer
	{
		public long VoucherId { get; set; }
	}

    public class CustomerSimpleCreateViewModel 
    {
        public long ID { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string RNC { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public long? ZoneId { get; set; }
        public long? VoucherId { get; set; }
	}
}
