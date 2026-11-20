using Serilog;
using TaskManagement.API.Extensions;
using TaskManagement.API.Middleware;
using Microsoft.OpenApi;
using TaskManagement.API.Data; // Required for DbSeeder

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Async(a => a.File(
        "Logs/log-.txt",
        rollingInterval: RollingInterval.Day))
    .CreateLogger();

try
{
    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // 2. OpenAPI + Swagger UI with JWT Auth Button
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token ONLY (Do not type 'Bearer ').\n\nExample: eyJhbGciOiJIUzI1NiIsInR5cCI6..."
        });

        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        });
    });

    // 3. Application Services
    builder.Services.AddDatabase(builder.Configuration);
    builder.Services.AddJwtAuth(builder.Configuration);
    builder.Services.AddAppServices();

    // 4. CORS Policy
    builder.Services.AddCors(o => o.AddPolicy("AllowAll", p =>
        p.WithOrigins(
            "http://localhost:5173",
            "https://localhost:7123",
            "http://localhost:5253"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()));

    var app = builder.Build();

    // --- 5. SEED THE DATABASE ON STARTUP ---
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DbSeeder.SeedAdminAsync(dbContext);
    }
    // ---------------------------------------

    // 6. Middleware Pipeline
    app.UseMiddleware<GlobalExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}