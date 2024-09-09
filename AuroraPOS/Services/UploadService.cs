using System.Drawing;
using static System.Net.Mime.MediaTypeNames;

namespace AuroraPOS.Services
{
	public interface IUploadService
	{
		Task<string> UploadImageFile(string sub, IFormFile file);
	}
	public class UploadService : IUploadService
	{
		private readonly IWebHostEnvironment _env;
		public UploadService(IWebHostEnvironment env)
		{
			_env = env;
		}
		public async Task<string> UploadImageFile(string sub, IFormFile file)
		{
			string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "images", sub);

			if (!Directory.Exists(uploadsFolder))
			{
				Directory.CreateDirectory(uploadsFolder);
			}
			var uniqueFileName = Guid.NewGuid().ToString();
			uniqueFileName += Path.GetExtension(file.FileName);
			var uploadsFile = Path.Combine(uploadsFolder, uniqueFileName);
			try
			{
				using (Stream fileStream = new FileStream(uploadsFile, FileMode.Create))
				{
					await file.CopyToAsync(fileStream);
				}
				//var image = System.Drawing.Image.FromStream(file.OpenReadStream(), true, true);
				//var newImage = new Bitmap(image.Width, image.Height);
				//using (var g = Graphics.FromImage(newImage))
				//{
				//	g.DrawImage(image, 0, 0, image.Width, image.Height);
				//	newImage.Save(uploadsFile);
				//}
			}
			catch
			{
				return string.Empty;
			}
			var uploadsUrl =  $"/uploads/images/{sub}/{uniqueFileName}";
			return uploadsUrl;
		}
	}
}
