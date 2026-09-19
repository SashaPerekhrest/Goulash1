using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Portfolio.Api.Conventions;
using Microsoft.OpenApi.Models;
using Portfolio.Api.Options;
using Portfolio.Application.DTOs.Common;
using Portfolio.Application.Interfaces;
using Portfolio.Infrastructure.Data;
using Portfolio.Infrastructure.Security;
using Portfolio.Infrastructure.Seed;
using Portfolio.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<PortfolioDbContext>(options =>
    options.UseNpgsql(connectionString));

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT settings are not configured.");

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer)
    || string.IsNullOrWhiteSpace(jwtOptions.Audience)
    || string.IsNullOrWhiteSpace(jwtOptions.Secret)
    || jwtOptions.Secret.Length < 32
    || jwtOptions.LifetimeMinutes <= 0)
{
    throw new InvalidOperationException("JWT settings are invalid.");
}

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.AddSingleton<IAmazonS3>(serviceProvider =>
{
    var storageOptions = serviceProvider
        .GetRequiredService<IConfiguration>()
        .GetSection(StorageOptions.SectionName)
        .Get<StorageOptions>()
        ?? throw new InvalidOperationException("Storage settings are not configured.");

    if (string.IsNullOrWhiteSpace(storageOptions.Endpoint)
        || string.IsNullOrWhiteSpace(storageOptions.ResolvedBucketName)
        || string.IsNullOrWhiteSpace(storageOptions.PublicBaseUrl)
        || string.IsNullOrWhiteSpace(storageOptions.Region))
    {
        throw new InvalidOperationException("Storage settings are not configured.");
    }

    var endpoint = new Uri(storageOptions.Endpoint);
    var config = new AmazonS3Config
    {
        ServiceURL = storageOptions.Endpoint,
        ForcePathStyle = storageOptions.ForcePathStyle,
        AuthenticationRegion = storageOptions.Region,
        UseHttp = endpoint.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
    };

    AWSCredentials credentials = string.IsNullOrWhiteSpace(storageOptions.AccessKey)
        || string.IsNullOrWhiteSpace(storageOptions.SecretKey)
            ? new AnonymousAWSCredentials()
            : new BasicAWSCredentials(storageOptions.AccessKey, storageOptions.SecretKey);

    return new AmazonS3Client(credentials, config);
});
builder.Services.AddScoped<IFileStorageService, S3FileStorageService>();

builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new AdminApiAuthorizeConvention());
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(
        namingPolicy: null,
        allowIntegerValues: false));
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(modelState => modelState.Value?.Errors.Count > 0)
            .ToDictionary(
                modelState => modelState.Key,
                modelState => modelState.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Invalid value."
                        : error.ErrorMessage)
                    .ToArray());

        return new BadRequestObjectResult(new ErrorResponse("Validation failed.", errors));
    };
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var response = new ErrorResponse("Unauthorized.");
                await context.Response.WriteAsync(JsonSerializer.Serialize(
                    response,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a JWT access token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontends", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? ["http://localhost:3000", "http://localhost:5173"];

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");

        await dbContext.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(dbContext, app.Configuration, logger);
    }
}

app.UseCors("LocalFrontends");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "Portfolio.Api", status = "running" }))
    .WithName("Root");

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health");

app.MapControllers();

app.Run();
