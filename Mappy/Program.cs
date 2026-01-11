using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mappy.Hubs;   // Our SignalR hub (see below)
using Mappy;      // Our VPService background service
using Mappy.Services;

var builder = WebApplication.CreateBuilder(args);

// Register SignalR.
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<OverlayTokenService>();

// Register the VPService as a hosted background service.
builder.Services.AddHostedService<VPService>();

// Optionally add Swagger (for any API endpoints you might add later).
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable HTTPS redirection.
app.UseHttpsRedirection();

// Enable default files and static files (this will serve wwwroot/index.html as the default page).
app.UseDefaultFiles();
app.UseStaticFiles();

// Map the SignalR hub endpoint.
app.MapHub<LocationHub>("/locationHub");

app.Run();
