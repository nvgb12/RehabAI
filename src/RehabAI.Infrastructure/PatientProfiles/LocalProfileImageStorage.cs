using Microsoft.AspNetCore.Hosting;
using RehabAI.Application.PatientProfiles;

namespace RehabAI.Infrastructure.PatientProfiles;

public sealed class LocalProfileImageStorage(IWebHostEnvironment environment) : IProfileImageStorage
{
    private const string PublicUploadPath = "/uploads/profile-images";

    public async Task<string> SaveAsync(
        Stream content,
        string fileExtension,
        CancellationToken cancellationToken = default)
    {
        var webRootPath = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
        var uploadDirectory = Path.Combine(webRootPath, "uploads", "profile-images");
        Directory.CreateDirectory(uploadDirectory);

        var safeExtension = fileExtension.ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(uploadDirectory, fileName);

        await using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return $"{PublicUploadPath}/{fileName}";
    }
}
