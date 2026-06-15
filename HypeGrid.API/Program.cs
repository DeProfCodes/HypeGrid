using HypeGrid.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// SERVICE REGISTRATION
// ============================================================================
builder.Services.AddCoreServices();
builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddEmailServices(builder.Configuration);
builder.Services.AddAssetStorage(builder.Configuration);
builder.Services.AddCustomCors();

var app = builder.Build();

// ============================================================================
// STARTUP: MIGRATE + SEED
// ============================================================================
await app.MigrateAndSeedAsync();

// ============================================================================
// HTTP PIPELINE
// ============================================================================
app.ConfigureMiddleware();

app.Run();
