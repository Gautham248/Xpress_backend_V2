using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Repositories;

//using Xpress_backend_V2.Repositories;
using Xpress_backend_V2.Models.Configuration;
using Xpress_backend_V2.Repository;
using Xpress_backend_V2.Services;
using Xpress_backend_V2.Services.Interface;


var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection("ApplicationSettings"));

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

builder.Services.AddScoped<IAirlineReportRepository, AirlineReportRepository>();

builder.Services.AddScoped<IRequestStatusServices, RequestStatusRepository>();
builder.Services.AddScoped<IUserNotificationServices, UserNotificationRepository>();
builder.Services.AddScoped<IAuditLogServices, AuditLogRepository>();
//builder.Services.AddScoped<IAadharDocServices, AadharDocRepository>();
//builder.Services.AddScoped<IPassportDocServices, PassportDocRepository>();
//builder.Services.AddScoped<IVisaDocServices, VisaDocRepository>();
builder.Services.AddScoped<IProjectRoleService, ProjectRoleService>();
builder.Services.AddScoped<ICalendarTravelRequestRepository,CalendarTravelRequestRepository>();
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

// For CORS error resolve
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowReactApp",
//        policy =>
//        {
//            policy.WithOrigins("http://localhost:5030", "http://localhost:5173") // Add React app ports
//                  .AllowAnyHeader()
//                  .AllowAnyMethod();
//        });
//});

// Register the RmtDataSyncService as a hosted service
//builder.Services.AddHostedService<RmtDataSyncService>();

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
//builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "JWTWebApplication", Version = "v1" });
    //option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    //{
    //    In = ParameterLocation.Header,
    //    Description = "Please enter a valid token",
    //    Name = "Authorization",
    //    Type = SecuritySchemeType.Http,
    //    BearerFormat = "JWT",
    //    Scheme = "Bearer"
    //});
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