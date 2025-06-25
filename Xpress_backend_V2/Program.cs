using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql; // <-- Added this using statement for the fix
using System.Text;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models.Configuration;
using Xpress_backend_V2.Repositories;
using Xpress_backend_V2.Repository;
using Xpress_backend_V2.Services;
using Xpress_backend_V2.Services.Interface;


var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection("ApplicationSettings"));

// Add services to the container.
builder.Services.AddControllers();


// --- START: Npgsql 8.0 JSONB FIX ---
// The original AddDbContext call is replaced with this block.
// This is required to opt-in to dynamic JSON mapping for jsonb columns.

// 1. Get the connection string from your configuration.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Create a data source builder and enable dynamic JSON mapping.
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson(); // <-- This is the line that fixes the error

// 3. Build the data source.
var dataSource = dataSourceBuilder.Build();

// 4. Register your DbContext to use the new, configured data source.
builder.Services.AddDbContext<ApiDbContext>(options =>
    options.UseNpgsql(dataSource)
);
// --- END: Npgsql 8.0 JSONB FIX ---


// Register services
builder.Services.AddScoped<ITravelRequestServices, TravelRequestRepository>();
builder.Services.AddScoped<ITicketOptionServices, TicketOptionRepository>();
builder.Services.AddScoped<IUserServices, UserRepository>();
builder.Services.AddScoped<IRMTServices, RMTRepository>();
builder.Services.AddScoped<ITravelModeServices, TravelModeRepository>();

builder.Services.AddScoped<IAirlineReportRepository, AirlineReportRepository>();

builder.Services.AddScoped<IRequestStatusServices, RequestStatusRepository>();
builder.Services.AddScoped<IUserNotificationServices, UserNotificationRepository>();
builder.Services.AddScoped<IAuditLogServices, AuditLogRepository>();
builder.Services.AddScoped<IProjectRoleService, ProjectRoleService>();
builder.Services.AddScoped<ICalendarTravelRequestRepository, CalendarTravelRequestRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITravelRequestStatsRepository, TravelRequestStatsRepository>();
builder.Services.AddScoped<IDocumentService, DocumentRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();

builder.Services.AddScoped<IAuditLogHandlerService, AuditLogHandlerService>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IProcessingTimeRepository, ProcessingTimeRepository>();
builder.Services.AddScoped<IDocumentStatusRepository, DocumentStatusRepository>();
builder.Services.AddScoped<ITravelAgencyStatRepository, TravelAgencyStatRepository>();
builder.Services.AddScoped<ITravelRequestRepo, TravelRequestRepo>();

builder.Services.AddAutoMapper(typeof(Program));

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

builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "JWTWebApplication", Version = "v1" });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

//Jwt 
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = false,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]))
    };
});

builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

// Apply CORS policy
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();