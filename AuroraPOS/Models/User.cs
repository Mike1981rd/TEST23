using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using AuroraPOS.ModelsCentral;

namespace AuroraPOS.Models
{
	public enum UserType
	{
		Admin = 0,
		Manager = 2,
		Waiter = 4,
		Kitchen = 8
	}
	public class User : BaseEntity
	{		
		public string Username { get; set; }		
		public string Password { get; set; }
		public string Pin { get; set; }
		public string Email { get; set; } = "";
		public string PhoneNumber { get; set; } = "";
		public UserType Type { get; set; }
		public string ProfileImage { get; set; } = "";
		public string FullName { get; set; } = "";
		public string Address { get; set; } = "";
		public string City { get; set; } = "";
		public string State { get; set; } = "";
		public string ZipCode { get; set; } = "";
		public virtual ICollection<Role> Roles { get; set; }        

    }
}
