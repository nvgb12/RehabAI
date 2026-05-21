using Microsoft.AspNetCore.Mvc;
using RehabAI.Application.Lookups;

namespace RehabAI.Api.Controllers;

[ApiController]
[Route("api")]
public class LookupsController(ILookupService lookupService) : ControllerBase
{
    [HttpGet("specialties")]
    public async Task<IActionResult> GetSpecialties(CancellationToken cancellationToken)
    {
        var specialties = await lookupService.GetSpecialtiesAsync(cancellationToken);

        return Ok(specialties);
    }

    [HttpGet("product-categories")]
    public async Task<IActionResult> GetProductCategories(CancellationToken cancellationToken)
    {
        var categories = await lookupService.GetProductCategoriesAsync(cancellationToken);

        return Ok(categories);
    }
}
