using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace Anidex.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public UploadController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided.");

            // Validate file type (allow images only)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!Array.Exists(allowedExtensions, e => e == extension))
                return BadRequest("Invalid file type. Only JPG, JPEG, PNG, GIF are allowed.");

            // Limit file size (e.g., 5 MB)
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("File size exceeds 5 MB limit.");

            // Generate a unique filename
            var fileName = $"{System.Guid.NewGuid()}{extension}";
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");

            // Ensure the uploads directory exists
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the relative URL path
            var url = $"/uploads/{fileName}";
            return Ok(new { url });
        }
    }
}