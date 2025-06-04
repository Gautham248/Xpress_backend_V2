    using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Repository;
using Xpress_backend_V2.Services;

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
builder.Services.AddScoped<IAadharDocServices, AadharDocRepository>();
builder.Services.AddScoped<IPassportDocServices, PassportDocRepository>();
builder.Services.AddScoped<IVisaDocServices, VisaDocRepository>();
builder.Services.AddScoped<IProjectRoleService, ProjectRoleService>();



builder.Services.AddAutoMapper(typeof(Program));

// For CORS error resolve
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5030", "http://localhost:5173") // Add React app ports
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

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