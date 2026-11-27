using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using Swashbuckle.AspNetCore.SwaggerGen;
using Subscriptions.Business.Contracts;
namespace Subscriptions.Api.OpenApi;


public sealed class ContractSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var type = context.Type;
        if (type != typeof(AuthInput) && type != typeof(SubscriptionInput) && type != typeof(PaymentInput))
            return;
        schema.AdditionalPropertiesAllowed = false;
        foreach (var pair in schema.Properties)
        {
            schema.Required.Add(pair.Key);
            pair.Value.Nullable = false;
            var property = pair.Value;
            switch (pair.Key)
            {
                case "email": property.MaxLength = 254; property.Format = "email"; break;
                case "password": property.MinLength = 12; property.MaxLength = 128; property.Format = "password"; break;
                case "name": property.MinLength = 1; property.MaxLength = 100; break;
                case "category": property.MinLength = 1; property.MaxLength = 50; break;
                case "price":
                case "amount": property.Pattern = @"^[0-9]{1,9}(\.[0-9]{1,2})?$"; property.Description = "0.01–999999999.99"; break;
                case "currency": property.Enum = new List<IOpenApiAny> { new OpenApiString("RUB"), new OpenApiString("USD"), new OpenApiString("EUR") }; break;
                case "frequency": property.Enum = new List<IOpenApiAny> { new OpenApiString("monthly"), new OpenApiString("yearly") }; break;
                case "status": property.Enum = new List<IOpenApiAny> { new OpenApiString("active"), new OpenApiString("paused"), new OpenApiString("archived") }; break;
                case "paidOn":
                case "nextPaymentDate": property.Format = "date"; property.Pattern = @"^\d{4}-\d{2}-\d{2}$"; break;
            }
        }
    }
}
