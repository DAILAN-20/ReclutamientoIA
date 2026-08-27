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
    }
}