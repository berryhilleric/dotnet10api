// Import custom services from the Api.Services namespace
using Api.Services;
// Import Azure Identity library for authentication with Azure services
using Azure.Identity;
// Import Cosmos DB SDK for database operations
using Microsoft.Azure.Cosmos;
// Import JWT Bearer authentication for token-based auth
using Microsoft.AspNetCore.Authentication.JwtBearer;
// Import Microsoft Identity Web for Azure AD integration
using Microsoft.Identity.Web;

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

// Read Cosmos DB account endpoint from configuration, throw error if missing
var cosmosAccountEndpoint = builder.Configuration["CosmosDb:AccountEndpoint"] ?? throw new InvalidOperationException("CosmosDb:AccountEndpoint not configured");
// Read Cosmos DB database name from configuration, throw error if missing
var databaseName = builder.Configuration["CosmosDb:DatabaseName"] ?? throw new InvalidOperationException("CosmosDb:DatabaseName not configured");
// Read Cosmos DB container name from configuration, throw error if missing
var containerName = builder.Configuration["CosmosDb:ContainerName"] ?? throw new InvalidOperationException("CosmosDb:ContainerName not configured");
// Read optional connection string from configuration (used for local development)
var connectionString = builder.Configuration["CosmosDb:ConnectionString"];

// Register CosmosClient as a singleton (one instance for the app lifetime)
builder.Services.AddSingleton<CosmosClient>(sp =>
{
    // Create Cosmos DB client options with custom serialization settings
    var cosmosClientOptions = new CosmosClientOptions
    {
        // Configure serializer options for JSON conversion
        SerializerOptions = new CosmosSerializationOptions
        {
            // Use camelCase for property names (e.g., "firstName" instead of "FirstName")
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        }
    };

    // Check if connection string is provided for local development
    if (!string.IsNullOrEmpty(connectionString))
    {
        // Use connection string for local development
        // Create client using connection string (less secure, for local dev only)
        return new CosmosClient(connectionString, cosmosClientOptions);
    }
    else
    {
        // Use DefaultAzureCredential for production (Managed Identity)
        // Create credential that uses Managed Identity in Azure
        var credential = new DefaultAzureCredential();
        // Create client using Azure credential (secure, for production)
        return new CosmosClient(cosmosAccountEndpoint, credential, cosmosClientOptions);
    }
});

// Register CosmosDbService as a singleton
builder.Services.AddSingleton<ICosmosDbService>(sp =>
{
    // Get the CosmosClient instance from dependency injection
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    // Create and return CosmosDbService with client, database, and container names
    return new CosmosDbService(cosmosClient, databaseName, containerName);
});

// Register BlobStorageService as a singleton
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

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
