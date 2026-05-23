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

// Railway/Render: copy MONGODB_URI into ASP.NET config key before host builds configuration.
foreach (var envKey in new[] { "MONGODB_URI", "MONGODB_CONNECTION_STRING", "MongoDb__ConnectionString" })
{
    var raw = Environment.GetEnvironmentVariable(envKey);
    if (string.IsNullOrWhiteSpace(raw) || raw.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        continue;
    Environment.SetEnvironmentVariable("MongoDb__ConnectionString", raw.Trim());
    break;
}

var builder = WebApplication.CreateBuilder(args);

var cloudPort = Environment.GetEnvironmentVariable("PORT")?.Trim();
var isCloudHost = !string.IsNullOrWhiteSpace(cloudPort);
if (isCloudHost)
{
    var listenUrl = $"http://0.0.0.0:{cloudPort}";
    builder.WebHost.UseUrls(listenUrl);
    Environment.SetEnvironmentVariable("ASPNETCORE_URLS", listenUrl);
}

// Paste Atlas connection string in appsettings.Local.json (overrides other settings)
builder.Configuration.AddJsonFile(
    Path.Combine(builder.Environment.ContentRootPath, "appsettings.Local.json"),
    optional: true,
    reloadOnChange: true);

builder.Services.Configure<MongoDbSettings>(options =>
{
    builder.Configuration.GetSection(MongoDbSettings.SectionName).Bind(options);
    var mongoUri = ConfigEnvironment.GetMongoConnectionString(builder.Configuration);
    if (!string.IsNullOrWhiteSpace(mongoUri))
        options.ConnectionString = mongoUri;
});
builder.Services.Configure<JwtSettings>(options =>
{
    builder.Configuration.GetSection(JwtSettings.SectionName).Bind(options);
    var jwtKey = Environment.GetEnvironmentVariable("Jwt__Key") ?? Environment.GetEnvironmentVariable("JWT_KEY");
    if (!string.IsNullOrWhiteSpace(jwtKey))
        options.Key = jwtKey.Trim();
});
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
    var mongoCfg = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbSettings>>().Value;
    var mongoTarget = ConfigEnvironment.DescribeMongoTarget(mongoCfg.ConnectionString);
    logger.LogInformation(
        "Database target: {Target}, name={Database}, envMongoVarSet={EnvSet}",
        mongoTarget, mongoCfg.DatabaseName, ConfigEnvironment.HasMongoEnvVar());
    if (mongoTarget is "localhost-default" or "not-set")
        logger.LogWarning(
            "MongoDB Atlas ma configured Railway. Geli MONGODB_URI ama MongoDb__ConnectionString kadib redeploy.");

    void LogStorageAndEmail()
    {
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

    if (isCloudHost || app.Environment.IsProduction())
    {
        LogStorageAndEmail();
        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        _ = Task.Run(async () =>
        {
            await Task.Delay(500, lifetime.ApplicationStopping);
            using var bgScope = app.Services.CreateScope();
            var bgLogger = bgScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            if (!ConfigEnvironment.HasMongoEnvVar())
            {
                bgLogger.LogWarning("MongoDB seed skipped — MONGODB_URI ma jiro container-ka.");
                return;
            }

            try
            {
                await bgScope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync(lifetime.ApplicationStopping);
                bgLogger.LogInformation("MongoDB connected OK — xogta waa la keydin karaa.");
            }
            catch (Exception ex) when (!lifetime.ApplicationStopping.IsCancellationRequested)
            {
                bgLogger.LogCritical(ex,
                    "MongoDB seed failed in background. Database='{Database}'. " +
                    "Hubi Railway variables MongoDb__ConnectionString iyo Atlas IP 0.0.0.0/0.",
                    mongoCfg.DatabaseName);
            }
        }, lifetime.ApplicationStopping);
    }
    else
    {
        try
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
            logger.LogInformation("MongoDB connected OK — xogta waa la keydin karaa.");
            LogStorageAndEmail();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex,
                "STARTUP ABORTED: Cannot connect to MongoDB. Database='{Database}'. " +
                "Hubi Atlas password iyo IP Access 0.0.0.0/0.",
                mongoCfg.DatabaseName);
            throw;
        }
    }
}

// Respond before HTTPS/CORS/auth so Railway/Render healthchecks always get 200.
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    if (path is "/api/health" or "/api/health/config" or "/api/health/env" or "/" or "/health")
    {
        if (path.Equals("/api/health/env", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new
            {
                success = true,
                MONGODB_URI_set = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MONGODB_URI")),
                MongoDb__ConnectionString_set = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MongoDb__ConnectionString")),
                PORT = Environment.GetEnvironmentVariable("PORT"),
                hint = "Haddii labaduba false yihiin, Railway variables ma gaarin container-ka."
            });
            return;
        }

        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(new
        {
            success = true,
            status = "healthy",
            timestamp = DateTime.UtcNow
        });
        return;
    }

    await next();
});

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

// Cloud hosts (Railway PORT) terminate TLS at the edge — HTTP only inside the container.
if (!app.Environment.IsDevelopment() && !isCloudHost)
    app.UseHttpsRedirection();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Root: 200 for platform healthchecks (Railway default "/"); Swagger only in Development.
app.MapGet("/", () => app.Environment.IsDevelopment()
        ? Results.Redirect("/swagger")
        : Results.Ok(new { success = true, status = "running", health = "/api/health" }))
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
