using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace GourmetApi.Services
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloud;

        public CloudinaryService(IConfiguration cfg)
        {
            var cloudName = cfg["Cloudinary:CloudName"];
            var apiKey = cfg["Cloudinary:ApiKey"];
            var apiSecret = cfg["Cloudinary:ApiSecret"];

            if (string.IsNullOrWhiteSpace(cloudName) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(apiSecret))
            {
                throw new Exception("Cloudinary config missing (CloudName/ApiKey/ApiSecret). Revisa appsettings / variables de entorno.");
            }

            var acc = new Account(cloudName, apiKey, apiSecret);
            _cloud = new Cloudinary(acc);
            _cloud.Api.Secure = true; // ✅ importante
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder)
        {
            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false
            };

            var result = await _cloud.UploadAsync(uploadParams);

            // ✅ si cloudinary devolvió error, acá lo ves
            if (result.Error != null)
                throw new Exception($"Cloudinary upload failed: {result.Error.Message} (HTTP {result.Error.Message.Length})");

            // ✅ a veces StatusCode puede venir raro, pero si no hay Error y hay URL, está OK
            var url = result.SecureUrl?.ToString() ?? result.Url?.ToString();
            if (string.IsNullOrWhiteSpace(url))
                throw new Exception($"Cloudinary upload failed: empty URL. Status={result.StatusCode}");

            return url;
        }
    }
}