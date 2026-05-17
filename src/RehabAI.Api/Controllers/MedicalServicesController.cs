using Microsoft.AspNetCore.Mvc;
using RehabAI.Application.MedicalServices;

namespace RehabAI.Api.Controllers;

[ApiController]
[Route("api/medical-services")]
public class MedicalServicesController(IMedicalServiceManager medicalServiceManager) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMedicalServices(CancellationToken cancellationToken)
    {
        var services = await medicalServiceManager.GetActiveMedicalServicesAsync(cancellationToken);

        return Ok(services);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMedicalService(Guid id, CancellationToken cancellationToken)
    {
        var service = await medicalServiceManager.GetActiveMedicalServiceByIdAsync(id, cancellationToken);

        return service is null ? NotFound(new { message = "Medical service was not found." }) : Ok(service);
    }
}
