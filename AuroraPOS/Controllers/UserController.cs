using AuroraPOS.Data;
using AuroraPOS.Models;
using AuroraPOS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuroraPOS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : BaseController
	{
		private readonly AppDbContext _dbContext;
		public UserController(ExtendedAppDbContext dbContext)
		{
			_dbContext = dbContext._context;
		}
		public IActionResult UserList()
		{
			return View();
		}
		public IActionResult EditUser(long UserId)
		{
			return View();
		}
		public IActionResult AddUser()
		{
			return View();
		}
	}
}
