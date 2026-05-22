using Microsoft.AspNetCore.Hosting;
using RehabAI.Application.Doctors;

namespace RehabAI.Infrastructure.Doctors;

public sealed class LocalDoctorAvatarStorage(IWebHostEnvironment environment) : IDoctorAvatarStorage
{
    private const string PublicUploadPath = "/uploads/doctor-avatars";

    public async Task<string> SaveAsync(
        Stream content,
        string fileExtension,
        CancellationToken cancellationToken = default)
    {
        var webRootPath = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
        var uploadDirectory = Path.Combine(webRootPath, "uploads", "doctor-avatars");
        Directory.CreateDirectory(uploadDirectory);

        var safeExtension = fileExtension.ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(uploadDirectory, fileName);

        await using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return $"{PublicUploadPath}/{fileName}";
    }
}
