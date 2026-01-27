// Import custom services from the Api.Services namespace
using Api.Services;
// Import Azure Identity library for authentication with Azure services
using Azure.Identity;
// Import JWT Bearer authentication for token-based auth
using Microsoft.AspNetCore.Authentication.JwtBearer;
// Import Microsoft Identity Web for Azure AD integration
using Microsoft.Identity.Web;
// Import Entity Framework Core for database operations
using Microsoft.EntityFrameworkCore;
// Import the DbContext for database access
using Api.Data;

// Create a web application builder with command-line arguments
var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// Register OpenAPI services for API documentation
builder.Services.AddOpenApi();
// Add API explorer to discover endpoints for Swagger
builder.Services.AddEndpointsApiExplorer();
// Add Swagger generator for interactive API documentation
builder.Services.AddSwaggerGen();
// Register MVC controllers to handle HTTP requests
builder.Services.AddControllers();
// Add HttpContextAccessor to access HTTP context in services
builder.Services.AddHttpContextAccessor();
// Register CurrentUserService as scoped (one instance per request)
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Configure JWT Bearer authentication as the default authentication scheme
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    // Add Microsoft Identity Web API authentication for Azure AD
    .AddMicrosoftIdentityWebApi(options =>
    {
        // Bind Azure AD configuration from appsettings.json to options
        builder.Configuration.Bind("AzureAd", options);

        // Accept ClientId as valid audience (matches your JWT token's aud claim)
        // Configure which audience values are accepted in JWT tokens
        options.TokenValidationParameters.ValidAudiences = new[]
        {
            // Accept tokens with ClientId as audience
            builder.Configuration["AzureAd:ClientId"],
            // Accept tokens with api://ClientId format as audience
            $"api://{builder.Configuration["AzureAd:ClientId"]}"
        };
    },
    // Configure Microsoft Identity options by binding Azure AD settings
    options => { builder.Configuration.Bind("AzureAd", options); });

// Add Authorization policies
// Configure authorization policies for controlling access to resources
builder.Services.AddAuthorization(options =>
{
    // Default policy requires authentication
    // Create a policy that requires the user to be authenticated
    options.AddPolicy("RequireAuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser());

    // Example: Role-based policy
    // Create a policy that requires the user to have the "Admin" role
    // to enforce this policy, 
    // [Authorize(Policy = "RequireAdminRole")] would need to be added to controllers or actions
    // and roles would need to be assigned in Azure AD
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Admin"));
});

// Read Cosmos DB configuration for EF Core
var cosmosAccountEndpoint = builder.Configuration["CosmosDb:AccountEndpoint"] ?? throw new InvalidOperationException("CosmosDb:AccountEndpoint not configured");
var databaseName = builder.Configuration["CosmosDb:DatabaseName"] ?? throw new InvalidOperationException("CosmosDb:DatabaseName not configured");
var connectionString = builder.Configuration["CosmosDb:ConnectionString"];

// Register BlobStorageService as a singleton
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

// Configure Entity Framework Core with Cosmos DB
// Register DbContext with Cosmos DB provider using existing configuration
builder.Services.AddDbContext<ApiDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
    {
        // Use connection string for local development
        options.UseCosmos(connectionString, databaseName);
    }
    else
    {
        // Use account endpoint and DefaultAzureCredential for production
        options.UseCosmos(cosmosAccountEndpoint, new DefaultAzureCredential(), databaseName);
    }
});

// Configure CORS for React app
// Add Cross-Origin Resource Sharing configuration
builder.Services.AddCors(options =>
{
    // Create a named CORS policy for the React application
    options.AddPolicy("AllowReactApp", policy =>
    {
        // Specify allowed origins (where requests can come from)
        policy.WithOrigins(
            // Allow requests from local Vite dev server
            "http://localhost:5173",
            // Allow requests from deployed Azure Static Web App
            "https://victorious-wave-0202d810f.1.azurestaticapps.net"
        )
        // Allow any HTTP headers in requests
        .AllowAnyHeader()
        // Allow any HTTP methods (GET, POST, PUT, DELETE, etc.)
        .AllowAnyMethod()
        // Allow credentials (cookies, authorization headers) to be sent
        .AllowCredentials();
    });
});

// Build the web application from the configured builder
var app = builder.Build();

// Ensure Cosmos DB database and container exist (EF Core for Cosmos DB)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
// Enable Swagger in all environments for testing
// Map OpenAPI endpoint for API schema
app.MapOpenApi();
// Enable Swagger middleware for API documentation JSON
app.UseSwagger();
// Enable Swagger UI for interactive API testing
app.UseSwaggerUI();

// Redirect all HTTP requests to HTTPS for secure communication
app.UseHttpsRedirection();
// Apply the CORS policy to allow requests from React app
app.UseCors("AllowReactApp");

// Authentication & Authorization MUST be in this order
// Enable authentication middleware to validate JWT tokens
app.UseAuthentication();
// Enable authorization middleware to check user permissions
app.UseAuthorization();
// Map controller endpoints to handle HTTP requests
app.MapControllers();
// Start the web application and listen for requests
app.Run();
