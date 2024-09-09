namespace AuroraPOS.ViewModels
{
    public class QuestionViewModel
    {
        public long ID { get; set; }
        public string Name { get; set; }
        public bool IsForced { get; set; }
        public int Answers { get; set; }
        public string Type { get; set; }
        public bool IsActive { get; set; }
    }
}
