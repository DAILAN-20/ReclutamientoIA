using Microsoft.EntityFrameworkCore;
using ReclutamientoIA.Models;

namespace ReclutamientoIA.Data
{
    public class ReclutamientoDbContext : DbContext
    {
        public ReclutamientoDbContext(
            DbContextOptions<ReclutamientoDbContext> options)
            : base(options)
        {
        }

        public DbSet<Vacante> Vacantes { get; set; }
    }
}