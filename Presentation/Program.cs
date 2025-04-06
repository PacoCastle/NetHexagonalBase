using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "your-issuer",
            ValidAudience = "your-audience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-secret-key"))
        };
    });

builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<KafkaService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    

    var kafkaUrl = configuration.GetValue<string>("Kafka:BrokerUrl") ?? "localhost:9092"; // Updated URL
    var logger = sp.GetRequiredService<ILogger<KafkaService>>();
    return new KafkaService(kafkaUrl, logger);
});

var app = builder.Build();

// Start Kafka Consumer in a background task
var cancellationTokenSource = new CancellationTokenSource();
var kafkaService = app.Services.GetRequiredService<KafkaService>();

app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        try
        {
            app.Logger.LogInformation("Starting Kafka consumer...");
            await kafkaService.ConsumeMessagesAsync("test");
        }
        catch (Exception ex)
        {
            app.Logger.LogError($"Kafka consumer failed: {ex.Message}");
        }
    });
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    app.Logger.LogInformation("Stopping Kafka consumer...");
    cancellationTokenSource.Cancel();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
