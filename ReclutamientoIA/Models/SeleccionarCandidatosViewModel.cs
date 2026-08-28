using System.ComponentModel.DataAnnotations;

namespace ReclutamientoIA.Models
{
    public class SeleccionarCandidatosViewModel
    {
        public int IdVacante { get; set; }

        public string TituloVacante { get; set; } =
            string.Empty;

        [Required(ErrorMessage = "Seleccione al menos un candidato")]
        [MinLength(
            1,
            ErrorMessage = "Seleccione al menos un candidato"
        )]
        public List<int> CandidatosSeleccionados { get; set; } =
            new();

        public List<CandidatoParaAnalisisViewModel> Candidatos
        {
            get;
            set;
        } = new();
    }

    public class CandidatoParaAnalisisViewModel
    {
        public int IdCandidato { get; set; }

        public string NombreCompleto { get; set; } =
            string.Empty;

        public string Correo { get; set; } =
            string.Empty;

        public string NombreArchivo { get; set; } =
            string.Empty;

        public DateTime FechaRegistro { get; set; }
    }
}