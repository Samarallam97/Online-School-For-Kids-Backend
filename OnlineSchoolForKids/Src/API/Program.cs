using API.Middleware;
using Application;
using Application.Mapping;
using Infrastructure;
using Infrastructure.Data.Seeding;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

#region Swagger
builder.Services.AddSwaggerGen(c =>
{
c.SwaggerDoc("v1", new OpenApiInfo
{
Title = "EduPlatform API",
Version = "v1",
Description = "Educational platform API with JWT authentication"
});

    // Add JWT Authentication to Swagger
c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
Name = "Authorization",
In = ParameterLocation.Header,
Type = SecuritySchemeType.ApiKey,
Scheme = "Bearer"
});

c.AddSecurityRequirement(new OpenApiSecurityRequirement
{
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
});
});

#endregion

#region Add Application and Infrastructure DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

#endregion

builder.Services.AddAutoMapper(typeof(CourseMappingProfile).Assembly);

#region CORS
builder.Services.AddCors(options =>
{
options.AddPolicy("AllowFrontend", policy =>
{
policy.WithOrigins(
        builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:8080" })
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials()
 .WithExposedHeaders("Content-Disposition");
});
});
#endregion

#region Files
builder.Services.Configure<FormOptions>(options =>
{
options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB max
options.ValueLengthLimit = int.MaxValue;
options.MultipartHeadersLengthLimit = int.MaxValue;
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
}); 
#endregion


// Health checks
builder.Services.AddHealthChecks();


var app = builder.Build();

app.UseCors("AllowFrontend");


// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ma'man API V1");
    });
}

// Custom middleware
app.UseGlobalExceptionHandler();
app.UseRateLimiting(requestLimit: 100, timeWindowSeconds: 60);

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Cleanup rate limiting cache periodically
var cleanupTimer = new Timer(_ =>
{
    RateLimitingMiddleware.CleanupOldEntries();
}, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));

await SuperAdminSeeder.SeedAsync(app);

app.Run();