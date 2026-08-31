using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReclutamientoIA.Data;
using ReclutamientoIA.Models;
using ReclutamientoIA.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<
    ICurriculumService,
    CurriculumService
>();
builder.Services.AddScoped<
    IPdfTextoService,
    PdfTextoService
>();


builder.Services.AddHttpClient();

builder.Services.AddScoped<
    IInteligenciaArtificialService,
    OpenAIService
>();


builder.Services.AddDbContext<ReclutamientoDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ConexionSQL")
    )
);

// Configuración de usuarios, contraseñas y roles
builder.Services
    .AddIdentity<Usuario, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ReclutamientoDbContext>()


    .AddDefaultTokenProviders();
builder.Services.Configure<SecurityStampValidatorOptions>(
    options =>
    {
        options.ValidationInterval = TimeSpan.Zero;
    }
);

// Rutas utilizadas cuando el usuario no ha iniciado sesión
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Cuenta/Login";
    options.AccessDeniedPath = "/Cuenta/AccesoDenegado";
});
// Configuración del correo.
builder.Services
    .AddOptions<CorreoOptions>()
    .Bind(builder.Configuration.GetSection(CorreoOptions.Seccion))
    .ValidateDataAnnotations();

// Servicio de envío de correos.
builder.Services.AddScoped<ICorreoService, CorreoService>();

var app = builder.Build();

// Crear roles y primer administrador
using (var scope = app.Services.CreateScope())
{
    await InicializadorDatos.InicializarAsync(
        scope.ServiceProvider,
        builder.Configuration
    );
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Primero identifica al usuario y después revisa sus permisos
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();