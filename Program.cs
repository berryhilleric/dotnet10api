using Api.Services;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader());
});


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Add Authorization policies
builder.Services.AddAuthorization(options =>
{
    // Default policy requires authentication
    options.AddPolicy("RequireAuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser());

    // Example: Role-based policy
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Admin"));
});

var cosmosAccountEndpoint = builder.Configuration["CosmosDb:AccountEndpoint"] ?? throw new InvalidOperationException("CosmosDb:AccountEndpoint not configured");
var databaseName = builder.Configuration["CosmosDb:DatabaseName"] ?? throw new InvalidOperationException("CosmosDb:DatabaseName not configured");
var containerName = builder.Configuration["CosmosDb:ContainerName"] ?? throw new InvalidOperationException("CosmosDb:ContainerName not configured");
var connectionString = builder.Configuration["CosmosDb:ConnectionString"];

builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var cosmosClientOptions = new CosmosClientOptions
    {
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        }
    };

    if (!string.IsNullOrEmpty(connectionString))
    {
        // Use connection string for local development
        return new CosmosClient(connectionString, cosmosClientOptions);
    }
    else
    {
        // Use DefaultAzureCredential for production (Managed Identity)
        var credential = new DefaultAzureCredential();
        return new CosmosClient(cosmosAccountEndpoint, credential, cosmosClientOptions);
    }
});

builder.Services.AddSingleton<ICosmosDbService>(sp =>
{
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    return new CosmosDbService(cosmosClient, databaseName, containerName);
});

// Configure CORS for React app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "https://victorious-wave-0202d810f.1.azurestaticapps.net"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();
app.UseCors("AllowAll");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");

// Authentication & Authorization MUST be in this order
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
