using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace Subscriptions.Api.OpenApi;


public sealed class SecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var parameter in operation.Parameters)
        {
            switch (parameter.Name)
            {
                case "Idempotency-Key":
                    parameter.Required = true;
                    parameter.Schema.MinLength = 1;
                    parameter.Schema.MaxLength = 128;
                    parameter.Description = "Один ключ пользователя. Повтор в течение 24 часов: тот же ответ 201; другие реквизиты: 409.";
                    break;
                case "from":
                case "to":
                    parameter.Required = true;
                    parameter.Schema.Format = "date";
                    parameter.Description = "Период [from,to), from < to.";
                    break;
                case "page":
                case "pageSize":
                    parameter.Schema.Type = "integer";
                    parameter.Schema.Minimum = 1;
                    parameter.Schema.Default = new OpenApiInteger(parameter.Name == "page" ? 1 : 20);
                    if (parameter.Name == "pageSize") parameter.Schema.Maximum = 100;
                    break;
                case "status":
                    parameter.Schema.Enum = new List<IOpenApiAny> { new OpenApiString("active"), new OpenApiString("paused"), new OpenApiString("archived") };
                    break;
            }
        }
        var attributes = context.MethodInfo.GetCustomAttributes(true)
            .Concat(context.MethodInfo.DeclaringType!.GetCustomAttributes(true));
        if (attributes.OfType<AllowAnonymousAttribute>().Any()) return;
        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
            }
        };
    }
}
