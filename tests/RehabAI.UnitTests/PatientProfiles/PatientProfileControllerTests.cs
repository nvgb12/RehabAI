using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Contracts.Patients;
using RehabAI.Api.Controllers;
using RehabAI.Application.PatientProfiles;

namespace RehabAI.UnitTests.PatientProfiles;

public class PatientProfileControllerTests
{
    [Fact]
    public async Task UploadProfileImage_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var controller = CreateController(new FakePatientProfileService());

        var response = await controller.UploadProfileImage(
            new UploadPatientProfileImageRequest { File = CreateImageFile() },
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(response);
    }

    [Fact]
    public async Task UploadProfileImage_WhenValidImage_ReturnsProfileImageUrl()
    {
        var controller = CreateController(new FakePatientProfileService
        {
            UploadResult = new PatientProfileImageUploadResult(
                true,
                "Patient profile image uploaded successfully.",
                "/uploads/profile-images/avatar.png")
        }, Guid.NewGuid());

        var response = await controller.UploadProfileImage(
            new UploadPatientProfileImageRequest { File = CreateImageFile() },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response);
        Assert.Contains("avatar.png", ok.Value!.ToString());
    }

    [Fact]
    public async Task UploadProfileImage_WhenFileIsMissing_ReturnsBadRequest()
    {
        var controller = CreateController(new FakePatientProfileService(), Guid.NewGuid());

        var response = await controller.UploadProfileImage(
            new UploadPatientProfileImageRequest(),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response);
    }

    private static PatientsController CreateController(
        FakePatientProfileService patientProfileService,
        Guid? userId = null)
    {
        var controller = new PatientsController(patientProfileService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        if (userId.HasValue)
        {
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", userId.Value.ToString())],
                "TestAuth"));
        }

        return controller;
    }

    private static IFormFile CreateImageFile()
    {
        var stream = new MemoryStream([1, 2, 3]);

        return new FormFile(stream, 0, stream.Length, "file", "avatar.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
    }

    private sealed class FakePatientProfileService : IPatientProfileService
    {
        public PatientProfileImageUploadResult UploadResult { get; set; } = new(
            false,
            "Not used.",
            FailureReason: PatientProfileFailureReason.Validation);

        public Task<PatientProfileResponse?> GetProfileAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PatientProfileResponse?>(null);
        }

        public Task<PatientProfileResult> UpdateProfileAsync(
            Guid patientProfileId,
            UpdatePatientProfileCommand command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PatientProfileResult(false, "Not used."));
        }

        public Task<PatientProfileImageUploadResult> UploadProfileImageAsync(
            UploadPatientProfileImageCommand command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UploadResult);
        }
    }
}
