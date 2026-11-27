using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using Subscriptions.Api.Authentication;
using Subscriptions.Api.Contracts;
using Subscriptions.Api.Infrastructure;
using Subscriptions.Api.Middleware;
using Subscriptions.Api.OpenApi;
using Subscriptions.Business.Abstractions;
using Subscriptions.Business.Services;
using Subscriptions.DataAccess;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("Subscriptions") ??
    $"Data Source={Path.Combine(builder.Environment.ContentRootPath, "subscriptions-v2.db")};Foreign Keys=True;Default Timeout=30";
builder.Services.AddDataAccess(connection);
builder.Services.AddSingleton<IClock, Subscriptions.Api.Infrastructure.SystemClock>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddAuthentication("Bearer").AddScheme<AuthenticationSchemeOptions, BearerHandler>("Bearer", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var fields = context.ModelState.Where(x => x.Value!.Errors.Count > 0)
            .ToDictionary(x => x.Key, _ => new[] { "Неверный формат или отсутствует обязательное поле." });
        return new BadRequestObjectResult(new ErrorResponse("validation_error", "Некорректный запрос.",
            context.HttpContext.TraceIdentifier, fields));
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Subscriptions API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "Токен из POST /api/v1/auth/login."
    });
    options.SchemaFilter<ContractSchemaFilter>();
    options.OperationFilter<SecurityOperationFilter>();
});
var app = builder.Build();
app.UseMiddleware<ErrorMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
await using (var scope = app.Services.CreateAsyncScope())
{
    if (app.Configuration.GetValue("Database:ApplyMigrations", true))
        await scope.ServiceProvider.GetRequiredService<SubscriptionsDbContext>().Database.MigrateAsync();
}
app.Run();
