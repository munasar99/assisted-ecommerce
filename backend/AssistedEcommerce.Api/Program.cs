using System.Text;
using AssistedEcommerce.Api.Authorization;
using AssistedEcommerce.Api.Constants;
using AssistedEcommerce.Api.Infrastructure;
using AssistedEcommerce.Api.Middleware;
using AssistedEcommerce.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Paste Atlas connection string in appsettings.Local.json (overrides other settings)
builder.Configuration.AddJsonFile(
    Path.Combine(builder.Environment.ContentRootPath, "appsettings.Local.json"),
    optional: true,
    reloadOnChange: true);

builder.Services.Configure<MongoDbSettings>(options =>
{
    builder.Configuration.GetSection(MongoDbSettings.SectionName).Bind(options);
    var atlasUri =
        Environment.GetEnvironmentVariable("MONGODB_URI")
        ?? Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
    if (!string.IsNullOrWhiteSpace(atlasUri))
        options.ConnectionString = atlasUri.Trim();
});
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<UploadSettings>(builder.Configuration.GetSection(UploadSettings.SectionName));
builder.Services.Configure<PricingSettings>(builder.Configuration.GetSection(PricingSettings.SectionName));
builder.Services.Configure<OrderFormSecuritySettings>(builder.Configuration.GetSection(OrderFormSecuritySettings.SectionName));
builder.Services.Configure<PaymentVerificationSettings>(builder.Configuration.GetSection(PaymentVerificationSettings.SectionName));
builder.Services.AddOptions<ResendSettings>()
    .Bind(builder.Configuration.GetSection(ResendSettings.SectionName))
    .PostConfigure(o =>
    {
        var key = Environment.GetEnvironmentVariable("RESEND_API_KEY");
        if (!string.IsNullOrWhiteSpace(key))
            o.ApiKey = key.Trim();
        if (!string.IsNullOrWhiteSpace(o.ApiKey) && !o.Enabled)
            o.Enabled = true;
    });

builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<IIdGeneratorService, IdGeneratorService>();
builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddSingleton<IMongoDbConnectionChecker, MongoDbConnectionChecker>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton<IPaymentScreenshotVerifier, PaymentScreenshotVerifier>();
builder.Services.AddHostedService<PaymentVerificationStartupCheck>();
builder.Services.AddHostedService<ResendStartupCheck>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddHttpClient<IEmailService, ResendEmailService>((sp, client) =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResendSettings>>().Value;
    var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
        ? "https://api.resend.com/"
        : settings.BaseUrl.TrimEnd('/') + "/";
    client.BaseAddress = new Uri(baseUrl);
});
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.AddHttpContextAccessor();

var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            RoleClaimType = "role"
        };
    });
builder.Services.AddSingleton<IAuthorizationHandler, AdminOrDevelopmentHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOrDevelopment", policy =>
        policy.Requirements.Add(new AdminOrDevelopmentRequirement()));
});

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Skip;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Assisted E-commerce API",
        Version = "v1",
        Description = "Backend API for Assisted E-commerce (Somalia market)"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var mongoCfg = builder.Configuration.GetSection(MongoDbSettings.SectionName).Get<MongoDbSettings>()!;

    var conn = mongoCfg.ConnectionString ?? "";
    var target = conn.Contains("mongodb.net", StringComparison.OrdinalIgnoreCase)
        ? "MongoDB Atlas (online)"
        : "MongoDB local";
    logger.LogInformation("Database target: {Target}, name={Database}", target, mongoCfg.DatabaseName);

    try
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
        logger.LogInformation("MongoDB connected OK — xogta waa la keydin karaa.");

        var cloudinary = builder.Configuration.GetSection(CloudinarySettings.SectionName).Get<CloudinarySettings>();
        if (cloudinary is { IsConfigured: true })
            logger.LogInformation("Image storage: Cloudinary cloud={Cloud}, folder={Folder}", cloudinary.CloudName, cloudinary.Folder);
        else
            logger.LogWarning(
                "Image storage: local /uploads (Cloudinary ma dhameystirna — geli CloudName + ApiSecret appsettings.Local.json).");

        var resend = builder.Configuration.GetSection(ResendSettings.SectionName).Get<ResendSettings>();
        if (resend is { IsConfigured: true })
            logger.LogInformation("Email: Resend enabled, from={From}", resend.FromEmail);
        else
            logger.LogWarning("Email: Resend disabled — geli Resend:ApiKey appsettings.Local.json ama RESEND_API_KEY.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex,
            "STARTUP ABORTED: Cannot connect to MongoDB. Database='{Database}'. " +
            "Hubi Atlas password (assistedecommerce / munasar12) iyo IP Access 0.0.0.0/0.",
            mongoCfg.DatabaseName);
        throw;
    }
}

var uploadRoot = Path.Combine(app.Environment.ContentRootPath, builder.Configuration["Uploads:RootPath"] ?? "uploads");
Directory.CreateDirectory(uploadRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadRoot),
    RequestPath = "/uploads"
});

var wwwroot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(wwwroot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(wwwroot),
    RequestPath = ""
});

app.MapGet("/api-kbaro", () => Results.Redirect("/api-kbaro.html")).ExcludeFromDescription();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.RoutePrefix = "swagger");
}

// HTTP-only dev profile: skip HTTPS redirect (avoids warning when no https port)
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Root URL → Swagger (browser opened at http://localhost:5298/)
app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

app.MapGet("/api", () => Results.Ok(new
{
    success = true,
    message = "Assisted E-commerce API",
    database = builder.Configuration["MongoDb:DatabaseName"],
    links = new
    {
        apiKbaro = "/api-kbaro",
        swagger = "/swagger",
        health = "/api/health",
        mongodbTest = "/api/health/mongodb",
        deliveryZones = "/api/delivery/zones/active"
    }
})).ExcludeFromDescription();

app.Run();
