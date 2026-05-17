using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Contracts.Doctors;
using RehabAI.Api.Contracts.MedicalServices;
using RehabAI.Application.Doctors;
using RehabAI.Application.MedicalServices;

namespace RehabAI.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController(
    IDoctorService doctorService,
    IMedicalServiceManager medicalServiceManager,
    IHostEnvironment hostEnvironment) : ControllerBase
{
    [HttpPost("doctors")]
    public async Task<IActionResult> CreateDoctor(CreateDoctorRequest request, CancellationToken cancellationToken)
    {
        var result = await doctorService.CreateDoctorAsync(
            new CreateDoctorCommand(
                request.FullName,
                request.Email,
                request.PhoneNumber,
                request.SpecialtyId,
                request.Bio,
                request.YearsOfExperience),
            cancellationToken);

        if (!result.Succeeded)
        {
            return result.FailureReason switch
            {
                CreateDoctorFailureReason.DuplicateEmail => Conflict(new { result.Message }),
                CreateDoctorFailureReason.MissingDoctorRole => StatusCode(StatusCodes.Status500InternalServerError, new { result.Message }),
                CreateDoctorFailureReason.SpecialtyNotFound => BadRequest(new { result.Message }),
                CreateDoctorFailureReason.EmailDeliveryFailed => StatusCode(
                    StatusCodes.Status502BadGateway,
                    BuildCreateDoctorResponse(result)),
                _ => BadRequest(new { result.Message })
            };
        }

        return Ok(BuildCreateDoctorResponse(result));
    }

    [HttpPost("medical-services")]
    public async Task<IActionResult> CreateMedicalService(
        CreateMedicalServiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await medicalServiceManager.CreateAsync(ToCreateCommand(request), cancellationToken);

        if (!result.Succeeded)
        {
            return ToMedicalServiceErrorResponse(result);
        }

        return CreatedAtAction(
            nameof(MedicalServicesController.GetMedicalService),
            "MedicalServices",
            new { id = result.MedicalService!.Id },
            new
            {
                message = result.Message,
                medicalService = result.MedicalService
            });
    }

    [HttpPut("medical-services/{id:guid}")]
    public async Task<IActionResult> UpdateMedicalService(
        Guid id,
        UpsertMedicalServiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await medicalServiceManager.UpdateAsync(id, ToCommand(request), cancellationToken);

        if (!result.Succeeded)
        {
            return ToMedicalServiceErrorResponse(result);
        }

        return Ok(new
        {
            message = result.Message,
            medicalService = result.MedicalService
        });
    }

    [HttpDelete("medical-services/{id:guid}")]
    public async Task<IActionResult> DeleteMedicalService(Guid id, CancellationToken cancellationToken)
    {
        var result = await medicalServiceManager.SoftDeleteAsync(id, cancellationToken);

        if (!result.Succeeded)
        {
            return ToMedicalServiceErrorResponse(result);
        }

        return Ok(new { message = result.Message });
    }

    [HttpGet("users")]
    public IActionResult ManageUsers() => Ok(Array.Empty<object>());

    [HttpGet("reports")]
    public IActionResult ViewReports() => Ok(new { message = "UC-19 scaffolded: reports." });

    [HttpGet("orders")]
    public IActionResult ManageOrdersAdmin() => Ok(new { message = "UC-23 scaffolded: admin order management." });

    [HttpGet("subscriptions")]
    public IActionResult ManageSubscriptionsAdmin() => Ok(new { message = "UC-27 scaffolded: admin subscription management." });

    [HttpGet("payouts")]
    public IActionResult ManagePayouts() => Ok(new { message = "UC-30 scaffolded: payout management." });

    private object BuildCreateDoctorResponse(CreateDoctorResult result)
    {
        var response = new Dictionary<string, object?>
        {
            ["message"] = result.Message,
            ["userId"] = result.UserId,
            ["doctorProfileId"] = result.DoctorProfileId,
            ["email"] = result.Email
        };

        if (hostEnvironment.IsDevelopment())
        {
            response["invitationToken"] = result.InvitationToken;
            response["passwordSetupUrl"] = result.PasswordSetupUrl;
        }

        return response;
    }

    private static UpsertMedicalServiceCommand ToCommand(UpsertMedicalServiceRequest request)
    {
        return new UpsertMedicalServiceCommand(
            request.Name,
            request.Description,
            request.DurationMinutes,
            request.Price,
            request.Currency,
            request.IsActive,
            request.NoShowFeeEnabled,
            request.NoShowFeeAmount);
    }

    private static UpsertMedicalServiceCommand ToCreateCommand(CreateMedicalServiceRequest request)
    {
        return new UpsertMedicalServiceCommand(
            request.Name,
            request.Description,
            request.DurationMinutes,
            request.Price,
            request.Currency,
            request.IsActive ?? true,
            request.NoShowFeeEnabled,
            request.NoShowFeeAmount);
    }

    private IActionResult ToMedicalServiceErrorResponse(MedicalServiceResult result)
    {
        return result.FailureReason switch
        {
            MedicalServiceFailureReason.NotFound => NotFound(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }
}
