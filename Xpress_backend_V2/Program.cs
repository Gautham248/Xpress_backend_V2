using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Repository;
using Xpress_backend_V2.Services;
using XPRESS_V1_Backend.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register DbContext
builder.Services.AddDbContext<ApiDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services
builder.Services.AddScoped<ITravelRequestServices, TravelRequestRepository>();
builder.Services.AddScoped<ITicketOptionServices, TicketOptionRepository>();
builder.Services.AddScoped<IUserServices, UserRepository>();
builder.Services.AddScoped<IRMTServices, RMTRepository>();
builder.Services.AddScoped<ITravelModeServices, TravelModeRepository>();
builder.Services.AddScoped<IAirlineServices, AirlineRepository>();
builder.Services.AddScoped<IRequestStatusServices, RequestStatusRepository>();
builder.Services.AddScoped<INotificationServices, NotificationRepository>();
builder.Services.AddScoped<IUserNotificationServices, UserNotificationRepository>();
builder.Services.AddScoped<IAuditLogServices, AuditLogRepository>();
//builder.Services.AddScoped<IAadharDocServices, AadharDocRepository>();
//builder.Services.AddScoped<IPassportDocServices, PassportDocRepository>();
//builder.Services.AddScoped<IVisaDocServices, VisaDocRepository>();
builder.Services.AddScoped<IProjectRoleService, ProjectRoleService>();
builder.Services.AddScoped<IDocumentService, DocumentRepository>();

// Configure HttpClient for RmtDataSyncService
builder.Services.AddHttpClient<RmtDataSyncService>(client =>
{
    client.BaseAddress = new Uri("https://api-rmtool.experionglobal.dev/");
    // Add headers or authentication if needed
    // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "your-token");
});

// Register the RmtDataSyncService as a hosted service
builder.Services.AddHostedService<RmtDataSyncService>();

builder.Services.AddScoped<IDocumentService, DocumentRepository>();
builder.Services.AddAutoMapper(typeof(Program)); // If using AutoMapper


// Add CORS policy to allow all frontends
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

// Configure Swagger/OpenAPI
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

// Apply CORS policy
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();