using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class POSAnswerDetail
    {
        public int Status { get; set; }
        public Answer? Answer { get; set; }
        public Question? SubQuestion { get; set; }
    }
}
