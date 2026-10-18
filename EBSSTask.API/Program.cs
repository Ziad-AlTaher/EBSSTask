using EBSSTask.API.Hubs;
using EBSSTask.API.Services;
using EBSSTask.Application;
using EBSSTask.Application.Common.Options;
using EBSSTask.Application.Reports.Interfaces;
using EBSSTask.Infrastructure;
using FluentValidation.AspNetCore;
using QuestPDF.Infrastructure;

// Configure QuestPDF license and fonts at startup
QuestPDF.Settings.License = LicenseType.Community;
QuestPDF.Settings.UseSystemFonts = true;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add CORS support for frontend (supports file:/// origin: null as well as localhost)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// FluentValidation automatic model validation
builder.Services.AddFluentValidationAutoValidation();

// Bind Report Generation options
builder.Services.Configure<ReportGenerationOptions>(
    builder.Configuration.GetSection(ReportGenerationOptions.SectionName));

// Clean Architecture Services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// SignalR Real-Time Notifications
builder.Services.AddSignalR();
builder.Services.AddScoped<IReportNotificationService, SignalRReportNotificationService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();
app.MapHub<ReportHub>(ReportHub.HubUrl);

app.Run();
