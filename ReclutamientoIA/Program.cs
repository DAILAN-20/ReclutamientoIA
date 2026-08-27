using Microsoft.EntityFrameworkCore;
using ReclutamientoIA.Data;

var builder = WebApplication.CreateBuilder(args);

// Agregar los controladores y las vistas
builder.Services.AddControllersWithViews();

// Conectar Entity Framework Core con SQL Server
builder.Services.AddDbContext<ReclutamientoDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ConexionSQL")
    )
);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();