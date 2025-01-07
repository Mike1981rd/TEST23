using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class ActiveCustomerList
    {
        public string? draw { get; set; }
        public int recordsFiltered { get; set; } = 0;
        public int recordsTotal { get; set; } = 0;
        public List<Customer>? activeCustomers { get; set; }
    }
}
