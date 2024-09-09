using AuroraPOS.Models;

namespace AuroraPOS.ViewModels
{
    public class PromotionCreateModel : Promotion
    {
        public string StartDateStr { get; set; }
        public string EndDateStr { get; set; }
        public string RecurringStartDateStr { get; set; }
    
    }
}
