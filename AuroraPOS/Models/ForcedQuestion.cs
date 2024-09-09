using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography.Pkcs;

namespace AuroraPOS.Models
{
   
    public class Question : BaseEntity
    {
        public string Name { get; set; }
        public int MaxAnswer { get; set; }
        public int MinAnswer { get; set; }
        public int FreeChoice { get; set; }
        public bool IsAutomatic { get; set; }
        public bool IsForced { get; set; } 
        public int DisplayOrder { get; set; }
        public List<Answer>? Answers { get; set; }
        public List<SmartButtonItem>? SmartButtons { get; set; }
        public virtual ICollection<Product>? Products { get; set; }
        public virtual ICollection<Category>? Categories { get; set; }
    }

    public class Answer : BaseEntity
    {
        public int Order { get; set; }
        public Product Product { get; set; }
        public AnswerPriceType PriceType { get; set; }
        public decimal FixedPrice { get; set; }
        public decimal RollPrice { get; set; }
        public long QuestionID { get; set; }
        public int ForcedQuestionID { get; set; }
        public bool IsPreSelected { get; set; }
        public bool HasQty { get; set; }
        public bool MatchSize { get; set; }
        public bool HasDivisor { get; set; }
        public int ServingSizeID { get; set; }
        public bool Comentario { get; set; }
        [NotMapped]
        public decimal Qty { get; set; }
    }

    public class SmartButtonItem : BaseEntity
    {   
        public int Order { get; set; }
        public SmartButton Button { get; set; }
    }
    public class SmartButton : BaseEntity
    {
        public string Name { get; set; }
        public bool IsApplyPrice { get; set; }
        public bool IsAfterText { get; set; }
        public string? BackColor { get; set; }
        public string? TextColor { get; set; }
    }
    public enum AnswerPriceType
    {       
        ChangePrice,
        Price1,
        Price2, 
        Price3, 
        Price4, 
        Price5, 
        Price6, 
        Price7, 
        Price8, 
        FixedPrice,
        RegularPrice
    }
}
