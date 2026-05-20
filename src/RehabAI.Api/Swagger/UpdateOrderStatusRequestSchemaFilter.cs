using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using RehabAI.Api.Contracts.Orders;
using RehabAI.Application.Orders;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RehabAI.Api.Swagger;

public sealed class UpdateOrderStatusRequestSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(UpdateOrderStatusRequest))
        {
            return;
        }

        if (!schema.Properties.TryGetValue("status", out var statusSchema))
        {
            return;
        }

        statusSchema.Type = "string";
        statusSchema.Format = null;
        statusSchema.Description =
            $"Allowed values: {OrderStatusCatalog.AdminUpdateStatusValuesText}. " +
            "Use Completed for orders that have reached the final delivered/completed state.";
        statusSchema.Enum = OrderStatusCatalog.AdminUpdateStatusNames
            .Select(status => (IOpenApiAny)new OpenApiString(status))
            .ToList();
        statusSchema.Example = new OpenApiString("Completed");

        schema.Example = new OpenApiObject
        {
            ["status"] = new OpenApiString("Completed")
        };
    }
}
