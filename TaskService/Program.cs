using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Exceptions;
using Shared.Extensions;
using Shared.Options;
using TaskService.Models.Db;
using TaskService.Options;
using TaskService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddOptions<AuthServiceClientOptions>()
    .Bind(builder.Configuration.GetSection(AuthServiceClientOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "Services:AuthService:BaseUrl is invalid.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "Services:AuthService:ApiKey is required.")
    .ValidateOnStart();

builder.Services.AddOptions<NotificationClientOptions>()
    .Bind(builder.Configuration.GetSection(NotificationClientOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "Services:NotificationService:BaseUrl is invalid.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "Services:NotificationService:ApiKey is required.")
    .ValidateOnStart();

builder.Services.AddOptions<OutboxOptions>()
    .Bind(builder.Configuration.GetSection(OutboxOptions.SectionName))
    .Validate(
        options => options.BatchSize > 0 && options.MaxAttempts > 0 && options.PollIntervalSeconds > 0,
        "Outbox settings must be positive.")
    .ValidateOnStart();

builder.Services.AddMemoryCache();
builder.Services.AddScoped<IUserTaskService, UserTaskService>();
builder.Services.AddSingleton<ITaskSorter, TaskSortHelper>();
builder.Services.AddHostedService<OutboxWorker>();

builder.Services.AddHttpClient<IUserDirectoryClient, UserDirectoryClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<AuthServiceClientOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

builder.Services.AddHttpClient<INotificationConnection, NotificationConnection>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<NotificationClientOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<TaskDbContext>()
    .AddCheck<OutboxHealthCheck>("outbox");

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.MigrateDatabaseAsync<TaskDbContext>(app.Logger);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program { }
