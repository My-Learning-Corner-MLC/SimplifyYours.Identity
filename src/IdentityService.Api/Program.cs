using IdentityService.Api.Endpoints;
using IdentityService.Api.Middleware;
using IdentityService.Application;
using IdentityService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseStaticFiles();
app.UseHostedSignInRequestValidation();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapPingEndpoints();
app.MapRazorPages();

app.Run();
