using AuroraPOS.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AuroraPOS.ViewModels
{
	public class CategoryViewModel
	{
		public long ID { get; set; }
		public string Name { get; set; }
		public int Plato { get; set; }
		public long GroupID { get; set; }
		public long CourseID { get; set; }
		public string? GroupName { get; set; }
		public bool IsActive { get; set; } = true;
		public List<long>? Taxes { get; set; }
		public List<long>? Propinas { get; set; }
		public List<long>? PrinterChannels { get; set; }
		public List<CategoryQuestionModel>? Questions { get; set; }
		public CategoryViewModel()
		{
			Questions = new List<CategoryQuestionModel>();
		}
	}

	public class CategoryQuestionModel
	{
		public long ID { get; set; }
		public int DisplayOrder { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
	}
}
