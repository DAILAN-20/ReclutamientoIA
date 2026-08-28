using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ReclutamientoIA.Models;

namespace ReclutamientoIA.Services
{
    public interface IInteligenciaArtificialService
    {
        Task<ResultadoAnalisisIA> AnalizarAsync(
            Vacante vacante,
            string textoCurriculum,
            ConfiguracionIA configuracion,
            PromptIA prompt
        );
    }

    public class OpenAIService
        : IInteligenciaArtificialService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public OpenAIService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<ResultadoAnalisisIA> AnalizarAsync(
            Vacante vacante,
            string textoCurriculum,
            ConfiguracionIA configuracion,
            PromptIA prompt)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "La clave API de OpenAI no está configurada."
                );
            }

            var textoLimitado = textoCurriculum.Length > 60000
                ? textoCurriculum[..60000]
                : textoCurriculum;

            var instrucciones = $"""
                {prompt.Contenido}

                VACANTE:
                {vacante.Titulo}

                DESCRIPCIÓN:
                {vacante.Descripcion}

                REQUISITOS:
                {vacante.Requisitos}

                CURRÍCULUM:
                {textoLimitado}
                """;

            var esquema = new
            {
                type = "object",
                properties = new
                {
                    nombre = new { type = "string" },
                    correo = new { type = "string" },
                    telefono = new { type = "string" },

                    puntuacion = new
                    {
                        type = "integer",
                        minimum = 0,
                        maximum = 100
                    },

                    resumen = new { type = "string" },
                    fortalezas = new { type = "string" },
                    debilidades = new { type = "string" },
                    recomendacion = new { type = "string" }
                },

                required = new[]
                {
                    "nombre",
                    "correo",
                    "telefono",
                    "puntuacion",
                    "resumen",
                    "fortalezas",
                    "debilidades",
                    "recomendacion"
                },

                additionalProperties = false
            };

            var solicitud = new
            {
                model = configuracion.Modelo,

                input = new[]
                {
                    new
                    {
                        role = "user",
                        content = new[]
                        {
                            new
                            {
                                type = "input_text",
                                text = instrucciones
                            }
                        }
                    }
                },

                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "analisis_curriculum",
                        strict = true,
                        schema = esquema
                    }
                }
            };

            var cliente = _httpClientFactory.CreateClient();

            using var mensaje = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.openai.com/v1/responses"
            );

            mensaje.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    apiKey
                );

            mensaje.Content = new StringContent(
                JsonSerializer.Serialize(solicitud),
                Encoding.UTF8,
                "application/json"
            );

            using var respuesta =
                await cliente.SendAsync(mensaje);

            var contenido =
                await respuesta.Content.ReadAsStringAsync();

            if (!respuesta.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"OpenAI devolvió el estado {(int)respuesta.StatusCode}."
                );
            }

            using var documento = JsonDocument.Parse(contenido);

            string? textoJson = null;

            foreach (var salida in
                     documento.RootElement
                         .GetProperty("output")
                         .EnumerateArray())
            {
                if (!salida.TryGetProperty(
                        "content",
                        out var bloques))
                {
                    continue;
                }

                foreach (var bloque in bloques.EnumerateArray())
                {
                    if (bloque.TryGetProperty(
                            "type",
                            out var tipo) &&
                        tipo.GetString() == "output_text")
                    {
                        textoJson = bloque
                            .GetProperty("text")
                            .GetString();

                        break;
                    }
                }

                if (!string.IsNullOrWhiteSpace(textoJson))
                {
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(textoJson))
            {
                throw new InvalidOperationException(
                    "OpenAI no devolvió un análisis utilizable."
                );
            }

            var resultado =
                JsonSerializer.Deserialize<ResultadoAnalisisIA>(
                    textoJson
                );

            if (resultado == null)
            {
                throw new InvalidOperationException(
                    "No fue posible interpretar el análisis."
                );
            }

            resultado.Puntuacion = Math.Clamp(
                resultado.Puntuacion,
                0,
                100
            );

            return resultado;
        }
    }
}