using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using Swashbuckle.AspNetCore.SwaggerGen;
using Subscriptions.Business.Contracts;
using Subscriptions.Api.Contracts;
namespace Subscriptions.Api.OpenApi;


public sealed class ContractSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var type = context.Type;
        var input = type == typeof(AuthInput) || type == typeof(SubscriptionInput) || type == typeof(PaymentInput);
        if (type.Namespace != typeof(AuthInput).Namespace && type.Namespace != typeof(ErrorResponse).Namespace)
            return;
        if (input) schema.AdditionalPropertiesAllowed = false;
        foreach (var pair in schema.Properties)
        {
            if (pair.Key != "errors") schema.Required.Add(pair.Key);
            pair.Value.Nullable = false;
            var property = pair.Value;
            switch (pair.Key)
            {
                case "email": property.MaxLength = 254; property.Format = "email"; break;
                case "password": property.MinLength = 12; property.MaxLength = 128; property.Format = "password"; break;
                case "name": property.MinLength = 1; property.MaxLength = 100; break;
                case "price":
                case "amount":
                    property.Pattern = input ? @"^(?!0+(?:\.0{1,2})?$)[0-9]{1,9}(?:\.[0-9]{1,2})?$" : @"^[0-9]+\.[0-9]{2}$";
                    property.Description = input ? "0.01–999999999.99 рубля" : "Сумма в рублях";
                    break;
                case "total": property.Pattern = @"^[0-9]+\.[0-9]{2}$"; property.Description = "Сумма в рублях"; break;
                case "accessToken": property.Pattern = "^[0-9A-Fa-f]{64}$"; break;
                case "tokenType": property.Enum = new List<IOpenApiAny> { new OpenApiString("Bearer") }; break;
                case "page": property.Minimum = 1; break;
                case "pageSize": property.Minimum = 1; property.Maximum = 100; break;
                case "totalCount": property.Minimum = 0; break;
                case "frequency": property.Enum = new List<IOpenApiAny> { new OpenApiString("monthly"), new OpenApiString("yearly") }; break;
                case "status":
                    property.Enum = type == typeof(ReadyResponse)
                    ? new List<IOpenApiAny> { new OpenApiString("ready") }
                    : new List<IOpenApiAny> { new OpenApiString("active"), new OpenApiString("paused"), new OpenApiString("archived") }; break;
                case "paidOn":
                case "nextPaymentDate": property.Format = "date"; property.Pattern = @"^\d{4}-\d{2}-\d{2}$"; break;
            }
        }
    }
}
