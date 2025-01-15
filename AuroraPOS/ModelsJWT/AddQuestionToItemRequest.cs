using AuroraPOS.Controllers;

namespace AuroraPOS.ModelsJWT
{
    public class AddQuestionToItemRequest
    {
        public long ItemId { get; set; }
        public long ServingSizeId { get; set; }
        public List<AddQuestionModel> Questions { get; set; } = new List<AddQuestionModel>();
        public int StationId { get; set; }
    }
}
