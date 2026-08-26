var builder = WebApplication.CreateBuilder(args);

// Frontend (UI) track: Razor/MVC view layer for the LeaveMate portal.
// The API backend lives in the "LEAVEMATE BACKEND" project and is consumed separately.
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
