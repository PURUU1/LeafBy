using LeafBy.Data;
using LeafBy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
    //options.UseSqlServer(connectionString));
    
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication()
   .AddGoogle(options =>
   {
       options.ClientId = builder.Configuration["Google:ClientId"];
       options.ClientSecret = builder.Configuration["Google:ClientSecret"];
       options.CallbackPath = "/signin-google";
   });
builder.Services.AddHttpClient();
builder.Services.AddScoped<LeafBy.Data.PerenualApiService>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Inside Program.cs
//builder.Services.AddStackExchangeRedisCache(options =>
//{
//    options.Configuration = "localhost:6379"; // Your Redis connection string
//    options.InstanceName = "LeafByLab_"; // Prefix for your cache keys
//}); 
builder.Services.AddMemoryCache();

//builder.Services.AddDistributedMemoryCache();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true; // This exposes the real C# error to your browser!
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSession();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

//// ── Seeding ────────────────────────────────────────────────────────────────────
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    try
//    {
//        var context = services.GetRequiredService<ApplicationDbContext>();
//        DbInitializer.Initialize(context);          // your existing seeder
//    }
//    catch (Exception ex)
//    {
//        var logger = services.GetRequiredService<ILogger<Program>>();
//        logger.LogError(ex, "An error occurred while seeding the database.");
//    }
//}

// ── Plant Catalog Seeder (downloads images + inserts plants) ──────────────────
//await PlantCatalogSeeder.SeedAsync(app.Services);


app.MapHub<LeafBy.Hubs.ChatHub>("/chatHub");
// Add this in Program.cs before app.Run()
app.MapGet("/health", () => Results.Ok("Website is awake!"));
app.Run();
