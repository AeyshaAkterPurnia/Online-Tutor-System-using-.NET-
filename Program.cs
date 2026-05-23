using DBConnection.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Retrieve the connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add services to the container.
builder.Services.AddControllersWithViews();

// --- SWAGGER SETUP (PART 1: SERVICES) ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// ----------------------------------------

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session expires after 30 minutes
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// FIX 1: Add this so your _Layout.cshtml can read session strings directly
builder.Services.AddHttpContextAccessor();

// STEP 4 MERGED: Register DbContext with SQL Server Connection String
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
// CHANGED: Adjusted to keep UseSwagger working cleanly during development mode testing
if (app.Environment.IsDevelopment())
{
    // --- SWAGGER SETUP (PART 2: MIDDLEWARE) ---
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "University System API v1");
    });
    // ------------------------------------------
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// FIX 2: Enable session state processing (MUST be placed after UseRouting and before UseAuthorization)
app.UseSession();

app.UseAuthorization();

// --- ROUTING SETUP ---
// 1. Map traditional MVC paths (/Coupons/Coupons)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Registration}/{action=Login}/{id?}");

// 2. Map structural attributes paths (/api/CouponsApi) so Swagger UI engine discovers them
app.MapControllers();
// ---------------------

// AUTOMATIC DATABASE & TABLE CREATION ON STARTUP
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();

    // Add this line to actually create the DB and tables if they don't exist!
    context.Database.EnsureCreated();
    // Seed default data if the table is empty
    if (!context.Courses.Any())
    {
        context.Courses.Add(new DBConnection.Models.Course
        {
            GroupID = 1,
            Subject = "Computer Science",
            CreatedBy = 2,
            CreatedAt = DateTime.Now
        });
        context.SaveChanges();
    }
}

app.Run();