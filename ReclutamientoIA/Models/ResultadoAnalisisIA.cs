using System.Text.Json.Serialization;

namespace ReclutamientoIA.Models
{
    public class ResultadoAnalisisIA
    {
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("correo")]
        public string Correo { get; set; } = string.Empty;

        [JsonPropertyName("telefono")]
        public string Telefono { get; set; } = string.Empty;

        [JsonPropertyName("puntuacion")]
        public int Puntuacion { get; set; }

        [JsonPropertyName("resumen")]
        public string Resumen { get; set; } = string.Empty;

        [JsonPropertyName("fortalezas")]
        public string Fortalezas { get; set; } = string.Empty;

        [JsonPropertyName("debilidades")]
        public string Debilidades { get; set; } = string.Empty;

        [JsonPropertyName("recomendacion")]
        public string Recomendacion { get; set; } =
            string.Empty;
    }
}