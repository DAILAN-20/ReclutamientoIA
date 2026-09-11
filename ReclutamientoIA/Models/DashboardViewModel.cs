namespace ReclutamientoIA.Models;

public class DashboardViewModel
{
    public string NombreUsuario { get; set; } = "Usuario";

    public int VacantesActivas { get; set; }

    public int CurriculumsRegistrados { get; set; }

    public int AnalizadosIA { get; set; }

    public int Preseleccionados { get; set; }

    public int EntrevistasPendientes { get; set; }

    public int CandidatosAprobados { get; set; }

    public List<VacanteDashboardViewModel> Vacantes { get; set; } = new();

    public List<CandidatoDestacadoViewModel> CandidatosDestacados { get; set; } = new();
}

public class VacanteDashboardViewModel
{
    public int IdVacante { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public int CantidadCandidatos { get; set; }

    public int Preseleccionados { get; set; }

    public int MayorPuntuacion { get; set; }
}

public class CandidatoDestacadoViewModel
{
    public int IdAnalisis { get; set; }

    public int IdVacante { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public string Vacante { get; set; } = string.Empty;

    public int Puntuacion { get; set; }

    public string Estado { get; set; } = string.Empty;
}