using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mappy.Hubs;   // Our SignalR hub (see below)
using Mappy;      // Our VPService background service
using Mappy.Services;

var builder = WebApplication.CreateBuilder(args);

// Register SignalR.
builder.Services.AddSignalR();

// Register the VPService as a hosted background service.
builder.Services.AddHostedService<VPService>();
builder.Services.Configure<OverlayTokenOptions>(builder.Configuration.GetSection("OverlayToken"));
builder.Services.AddSingleton<OverlayTokenService>();

// Optionally add Swagger (for any API endpoints you might add later).
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var overlayTokenOptions = builder.Configuration.GetSection("OverlayToken").Get<OverlayTokenOptions>() ?? new OverlayTokenOptions();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = OverlayTokenService.BuildTokenValidationParameters(overlayTokenOptions);
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/locationHub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

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

app.UseAuthentication();
app.UseAuthorization();

// Map the SignalR hub endpoint.
app.MapHub<LocationHub>("/locationHub");

app.Run();
