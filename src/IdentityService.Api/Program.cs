using IdentityService.Api.Endpoints;
using IdentityService.Application.Ping;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPingService, PingService>();

var app = builder.Build();

app.MapPingEndpoints();

app.Run();
