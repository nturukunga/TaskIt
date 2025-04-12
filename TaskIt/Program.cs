using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TaskIt.Data;
using TaskIt.Middleware;
using TaskIt.Models;
using TaskIt.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/taskit-.log", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
string connectionString;
string? databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

// Parse DATABASE_URL if available (from environment variables)
if (!string.IsNullOrEmpty(databaseUrl))
{
    // Convert from postgresql:// URL format to Npgsql connection string
    try {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";
        
        // Add SSL mode if in the query string
        bool useSsl = uri.Query.Contains("sslmode=require");
        
        connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";
        if (useSsl)
        {
            connectionString += ";SSL Mode=Require;Trust Server Certificate=true";
        }
        
        Log.Information($"Using DATABASE_URL environment variable for database connection");
    }
    catch (Exception ex)
    {
        Log.Warning($"Failed to parse DATABASE_URL: {ex.Message}. Falling back to individual connection parameters.");
        // Fall back to individual environment variables
        string envHost = Environment.GetEnvironmentVariable("PGHOST");
        string envPort = Environment.GetEnvironmentVariable("PGPORT");
        string envDatabase = Environment.GetEnvironmentVariable("PGDATABASE");
        string envUser = Environment.GetEnvironmentVariable("PGUSER");
        string envPassword = Environment.GetEnvironmentVariable("PGPASSWORD");

        // Only use environment variables if all necessary ones are present
        if (!string.IsNullOrEmpty(envHost) && !string.IsNullOrEmpty(envDatabase) && !string.IsNullOrEmpty(envUser))
        {
            string port = !string.IsNullOrEmpty(envPort) ? envPort : "5432";
            connectionString = $"Host={envHost};Port={port};Database={envDatabase};Username={envUser};Password={envPassword ?? ""}";
        }
        else
        {
            // If environment variables are missing, use an empty string (will be replaced with appsettings value)
            connectionString = "";
        }
    }
}
else
{
    // Use individual environment variables
    string envHost = Environment.GetEnvironmentVariable("PGHOST");
    string envPort = Environment.GetEnvironmentVariable("PGPORT");
    string envDatabase = Environment.GetEnvironmentVariable("PGDATABASE");
    string envUser = Environment.GetEnvironmentVariable("PGUSER");
    string envPassword = Environment.GetEnvironmentVariable("PGPASSWORD");

    // Only use environment variables if all necessary ones are present
    if (!string.IsNullOrEmpty(envHost) && !string.IsNullOrEmpty(envDatabase) && !string.IsNullOrEmpty(envUser))
    {
        string port = !string.IsNullOrEmpty(envPort) ? envPort : "5432";
        connectionString = $"Host={envHost};Port={port};Database={envDatabase};Username={envUser};Password={envPassword ?? ""}";
    }
    else
    {
        // If environment variables are missing, don't build a partial connection string
        connectionString = "";
    }
}

// Override the connection string from appsettings only if environment variables are properly configured
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Log.Information("Using connection string from appsettings.json");
}

// Log the connection string setup (without the password)
string sanitizedConnectionString = connectionString.Contains("Password=") ? 
    connectionString.Substring(0, connectionString.IndexOf("Password=")) + "Password=***" : 
    connectionString;
Log.Information($"Database connection configured: {sanitizedConnectionString}");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// Register application services
builder.Services.AddScoped<INotificationService, NotificationService>();

// Configure session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Create database and run migrations on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        Log.Information("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred during database migration");
    }
}

app.Run();
