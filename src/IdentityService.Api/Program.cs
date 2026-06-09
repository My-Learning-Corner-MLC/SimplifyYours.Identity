using IdentityService.Api.Endpoints;
using IdentityService.Api.Middleware;
using IdentityService.Api.Observability;
using IdentityService.Api.Responses;
using IdentityService.Application;
using IdentityService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceObservability("identity-service");
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseFriendlyErrorResponses();
app.UseRequestLogging();
app.UseStaticFiles();
app.UseHostedSignInRequestValidation();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapPingEndpoints();
app.MapRazorPages();

app.Run();
