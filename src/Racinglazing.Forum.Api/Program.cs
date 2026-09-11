using Microsoft.EntityFrameworkCore;
using Racinglazing.Forum.Api.Common;
using Racinglazing.Forum.Api.Identity;
using Racinglazing.Forum.Application;
using Racinglazing.Forum.Application.Abstractions.Identity;
using Racinglazing.Forum.Infrastructure;
using Racinglazing.Forum.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- Composition root ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var authConfigured =
    !string.IsNullOrWhiteSpace(builder.Configuration["ForumAuth:Authority"]) ||
    !string.IsNullOrWhiteSpace(builder.Configuration["ForumAuth:Issuer"]);
builder.Services.AddForumAuthentication(builder.Configuration);

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

builder.Services.AddControllers(options =>
{
    // FluentValidation for request bodies, emitting the contract's error envelope.
    options.Filters.Add<FluentValidationActionFilter>();
});

// Emit model-binding failures in the contract's { error } envelope too.
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = string.Join(" ", context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m)));
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
            new ErrorEnvelope(new ErrorBody("VALIDATION_ERROR",
                string.IsNullOrWhiteSpace(message) ? "The request is invalid." : message)));
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

if (authConfigured)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseSwagger(options => options.RouteTemplate = "openapi/{documentName}.json");
app.MapScalarApiReference(options => options
    .WithTitle("RacingVacing ForumService")
    .WithOpenApiRoutePattern("/openapi/{documentName}.json"));

app.MapControllers();
// app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).WithTags("System");
app.MapHealthChecks("/health/live", new()
{
    Predicate = _ => false
    });

app.MapHealthChecks("/health/ready");
await MigrateAndSeedAsync(app);

app.Run();

// Applies pending migrations and seeds the MVP taxonomy on startup when enabled.
static async Task MigrateAndSeedAsync(WebApplication app)
{
    if (!app.Configuration.GetValue("ForumDatabase:AutoMigrate", true)) return;

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ForumDbContext>();
    await db.Database.MigrateAsync();
    await ForumDbSeeder.SeedAsync(db);
}

// Exposed for integration testing.
public partial class Program;
