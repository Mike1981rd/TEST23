using System.ComponentModel.DataAnnotations;

namespace AuroraPOS.ViewModels
{
	public class LoginViewModel
	{
		
		public string UserName { get; set; }
		[Required]
		public string Password { get; set; }

        public bool SelectDatabase { get; set; }

        public string? Database { get; set; }

        [StringLength(4)]
        public string CaptchaCode { get; set; }
    }
}
