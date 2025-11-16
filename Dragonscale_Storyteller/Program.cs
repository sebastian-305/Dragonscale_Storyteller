using Dragonscale_Storyteller.Configuration;
using Dragonscale_Storyteller.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure logging levels
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);
builder.Logging.AddFilter("Dragonscale_Storyteller", LogLevel.Information);

// Add services to the container.
builder.Services.AddControllers();

// Configure Nebius AI settings
builder.Services.Configure<NebiusAiConfiguration>(
    builder.Configuration.GetSection("NebiusAi"));

// Configure Storage settings
builder.Services.Configure<StorageConfiguration>(
    builder.Configuration.GetSection("Storage"));

// Add Memory Cache for story storage
builder.Services.AddMemoryCache();

// Register application services
builder.Services.AddScoped<IPdfProcessorService, PdfProcessorService>();
builder.Services.AddScoped<IAiService, NebiusAiService>();
builder.Services.AddScoped<IPdfGeneratorService, PdfGeneratorService>();
builder.Services.AddScoped<IStoryStorageService, StoryStorageService>();
builder.Services.AddScoped<IStoryService, StoryService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

// Add request logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var requestId = Guid.NewGuid().ToString("N")[..8];
    
    using (logger.BeginScope(new Dictionary<string, object>
    {
        ["RequestId"] = requestId,
        ["RequestPath"] = context.Request.Path,
        ["RequestMethod"] = context.Request.Method
    }))
    {
        var startTime = DateTime.UtcNow;
        logger.LogInformation("Request started: {Method} {Path}", 
            context.Request.Method, context.Request.Path);
        
        try
        {
            await next();
            
            var duration = DateTime.UtcNow - startTime;
            logger.LogInformation("Request completed: {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            logger.LogError(ex, "Request failed: {Method} {Path} - Duration: {Duration}ms - Error: {ErrorMessage}",
                context.Request.Method, context.Request.Path, duration.TotalMilliseconds, ex.Message);
            throw;
        }
    }
});

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
