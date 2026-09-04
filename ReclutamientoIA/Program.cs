using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReclutamientoIA.Data;
using ReclutamientoIA.Models;
using ReclutamientoIA.Services;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// MVC
// =====================================================

builder.Services.AddControllersWithViews();

// =====================================================
// BASE DE DATOS
// =====================================================

builder.Services.AddDbContext<ReclutamientoDbContext>(
    options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString(
                "ConexionSQL"
            )
        )
);

// =====================================================
// PROTECCIÓN DE DATOS
// =====================================================

// Se utiliza para cifrar y descifrar la clave API
// almacenada en la base de datos.
builder.Services.AddDataProtection();

// =====================================================
// IDENTITY, USUARIOS Y ROLES
// =====================================================

builder.Services
    .AddIdentity<Usuario, IdentityRole>(
        options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireDigit = true;

            options.Password
                .RequireNonAlphanumeric = false;

            options.User.RequireUniqueEmail = true;
        }
    )
    .AddEntityFrameworkStores<
        ReclutamientoDbContext
    >()
    .AddDefaultTokenProviders();

builder.Services.Configure<
    SecurityStampValidatorOptions
>(
    options =>
    {
        options.ValidationInterval =
            TimeSpan.Zero;
    }
);

builder.Services.ConfigureApplicationCookie(
    options =>
    {
        options.LoginPath =
            "/Cuenta/Login";

        options.AccessDeniedPath =
            "/Cuenta/AccesoDenegado";
    }
);

// =====================================================
// HTTP CLIENT
// =====================================================

// Utilizado para conectarse con OpenAI,
// Gemini y otros servicios externos.
builder.Services.AddHttpClient();

// =====================================================
// SERVICIOS DE CURRÍCULUMS
// =====================================================

builder.Services.AddScoped<
    ICurriculumService,
    CurriculumService
>();

builder.Services.AddScoped<
    IPdfTextoService,
    PdfTextoService
>();

builder.Services.AddScoped<
    ICorreoCurriculumsService,
    CorreoCurriculumsService
>();

// =====================================================
// SERVICIO DE INTELIGENCIA ARTIFICIAL
// =====================================================

// OpenAIService seleccionará internamente
// OpenAI o Gemini según la configuración guardada.
builder.Services.AddScoped<
    IInteligenciaArtificialService,
    OpenAIService
>();

// =====================================================
// GOOGLE CALENDAR
// =====================================================

builder.Services.Configure<
    GoogleCalendarOptions
>(
    builder.Configuration.GetSection(
        GoogleCalendarOptions.Seccion
    )
);

builder.Services.AddScoped<
    IGoogleCalendarService,
    GoogleCalendarService
>();

// =====================================================
// CORREO
// =====================================================

builder.Services
    .AddOptions<CorreoOptions>()
    .Bind(
        builder.Configuration.GetSection(
            CorreoOptions.Seccion
        )
    )
    .ValidateDataAnnotations();

builder.Services.AddScoped<
    ICorreoService,
    CorreoService
>();

// =====================================================
// CONSTRUIR APLICACIÓN
// =====================================================

var app = builder.Build();

// =====================================================
// CREAR DATOS INICIALES
// =====================================================

using (var scope = app.Services.CreateScope())
{
    await InicializadorDatos.InicializarAsync(
        scope.ServiceProvider,
        builder.Configuration
    );
}

// =====================================================
// MANEJO DE ERRORES
// =====================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Home/Error"
    );

    app.UseHsts();
}

// =====================================================
// MIDDLEWARE
// =====================================================

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();

// =====================================================
// RUTA PRINCIPAL
// =====================================================

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}"
)
.WithStaticAssets();

app.Run();