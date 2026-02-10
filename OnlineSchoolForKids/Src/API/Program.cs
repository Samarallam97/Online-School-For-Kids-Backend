using API.Middleware;
using Application;
using Application.Commands;
using Application.Mapping;
using Application.Queries;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using CourseEntity=Domain.Entities.Course;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger with JWT support
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

// Add Application and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<MongoDbContext>();


builder.Services.AddScoped<IMongoCollection<CourseEntity>>(sp =>
{
    var context = sp.GetRequiredService<MongoDbContext>();
    return context.Courses;
});

builder.Services.AddScoped<IMongoCollection<Wishlist>>(sp =>
{
    var context = sp.GetRequiredService<MongoDbContext>();
    return context.Wishlists;
});
builder.Services.AddScoped<IMongoCollection<User>>(sp =>
{
    var context = sp.GetRequiredService<MongoDbContext>();
    return context.Users;
});

// Add this
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(CreateCourseCommand).Assembly);
});
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(GetCourseByIdQuery).Assembly);
});
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(GetCoursesQuery).Assembly);
});
builder.Services.AddAutoMapper(typeof(CourseMappingProfile).Assembly);
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));




builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:8080" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Health checks
builder.Services.AddHealthChecks();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var mongoContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    await mongoContext.SeedDataAsync();
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EduPlatform API V1");
    });
}

// Custom middleware
app.UseGlobalExceptionHandler();
app.UseRateLimiting(requestLimit: 100, timeWindowSeconds: 60);

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

// CRITICAL: Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Cleanup rate limiting cache periodically
var cleanupTimer = new System.Threading.Timer(_ =>
{
    RateLimitingMiddleware.CleanupOldEntries();
}, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));

app.Run();