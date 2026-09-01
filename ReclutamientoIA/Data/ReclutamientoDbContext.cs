using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ReclutamientoIA.Models;

namespace ReclutamientoIA.Data
{
    public class ReclutamientoDbContext
        : IdentityDbContext<Usuario>
    {
        public ReclutamientoDbContext(
            DbContextOptions<ReclutamientoDbContext> options)
            : base(options)
        {
        }

        public DbSet<Vacante> Vacantes { get; set; }
        public DbSet<Candidato> Candidatos { get; set; }
        public DbSet<AnalisisIA> AnalisisIA { get; set; }
        public DbSet<ConfiguracionIA> ConfiguracionesIA { get; set; }

        public DbSet<PromptIA> PromptsIA { get; set; }

        public DbSet<Entrevista> Entrevistas { get; set; }

        public DbSet<CorreoProcesado> CorreosProcesados
        {
            get;
            set;
        }
    }
}