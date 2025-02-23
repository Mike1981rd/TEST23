using AuroraPOS.Controllers;

namespace AuroraPOS.ModelsJWT
{
    public class AddDiscountRequest
    {
        public int stationId { get; set; }
        public DiscountModel discountModel { get; set; }
    }
}
