using HardwareMonitor.Controllers;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Add services to the container.

var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();

// Configure the CORS policy
if (allowedOrigins != null)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", builder =>
        {
            builder.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
        });
    });
}

builder.Services.AddControllers();

var app = builder.Build();

app.Lifetime.ApplicationStopping.Register(() =>
{
    if (AMDGPUController.ADLXHelperInitialized)
    {
        AMDGPUController.adlxHelper.Terminate();
        AMDGPUController.adlxHelper.Dispose();
    }
});

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseCors("CorsPolicy");

app.Run();
