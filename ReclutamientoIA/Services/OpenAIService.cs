using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
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

    public class OpenAIService :
        IInteligenciaArtificialService
    {
        private readonly IHttpClientFactory
            _httpClientFactory;

        private readonly IDataProtector
            _protector;

        private readonly JsonSerializerOptions
            _opcionesJson;

        public OpenAIService(
            IHttpClientFactory httpClientFactory,
            IDataProtectionProvider dataProtectionProvider)
        {
            _httpClientFactory =
                httpClientFactory;

            _protector =
                dataProtectionProvider.CreateProtector(
                    "ReclutamientoIA.ClaveApi"
                );

            _opcionesJson =
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
        }

        // =====================================================
        // SELECCIONAR PROVEEDOR
        // =====================================================

        public async Task<ResultadoAnalisisIA> AnalizarAsync(
            Vacante vacante,
            string textoCurriculum,
            ConfiguracionIA configuracion,
            PromptIA prompt)
        {
            ValidarConfiguracion(
                configuracion,
                prompt
            );

            var claveApi =
                ObtenerClaveApi(configuracion);

            var instrucciones =
                ConstruirInstrucciones(
                    vacante,
                    textoCurriculum,
                    prompt
                );

            ResultadoAnalisisIA resultado;

            if (configuracion.Proveedor.Equals(
                    "OpenAI",
                    StringComparison.OrdinalIgnoreCase))
            {
                resultado =
                    await AnalizarConOpenAIAsync(
                        instrucciones,
                        configuracion.Modelo,
                        claveApi
                    );
            }
            else if (configuracion.Proveedor.Equals(
                         "Gemini",
                         StringComparison.OrdinalIgnoreCase))
            {
                resultado =
                    await AnalizarConGeminiAsync(
                        instrucciones,
                        configuracion.Modelo,
                        claveApi
                    );
            }
            else
            {
                throw new InvalidOperationException(
                    $"El proveedor '{configuracion.Proveedor}' " +
                    "no está soportado."
                );
            }

            resultado.Puntuacion =
                Math.Clamp(
                    resultado.Puntuacion,
                    0,
                    100
                );

            return resultado;
        }

        // =====================================================
        // OPENAI
        // =====================================================

        private async Task<ResultadoAnalisisIA>
            AnalizarConOpenAIAsync(
                string instrucciones,
                string modelo,
                string claveApi)
        {
            var esquema = CrearEsquema();

            var solicitud = new
            {
                model = modelo,

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

            var cliente =
                _httpClientFactory.CreateClient();

            using var mensaje =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.openai.com/v1/responses"
                );

            mensaje.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    claveApi
                );

            mensaje.Content =
                CrearContenidoJson(solicitud);

            using var respuesta =
                await cliente.SendAsync(mensaje);

            var contenido =
                await respuesta.Content
                    .ReadAsStringAsync();

            if (!respuesta.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    "OpenAI rechazó la solicitud. " +
                    $"Código HTTP: {(int)respuesta.StatusCode}. " +
                    "Compruebe la clave API y el modelo."
                );
            }

            using var documento =
                JsonDocument.Parse(contenido);

            var textoJson =
                ExtraerTextoOpenAI(
                    documento.RootElement
                );

            return ConvertirResultado(
                textoJson,
                "OpenAI"
            );
        }

        private static string ExtraerTextoOpenAI(
            JsonElement raiz)
        {
            if (!raiz.TryGetProperty(
                    "output",
                    out var salidas))
            {
                throw new InvalidOperationException(
                    "OpenAI no devolvió el campo output."
                );
            }

            foreach (var salida in salidas.EnumerateArray())
            {
                if (!salida.TryGetProperty(
                        "content",
                        out var bloques))
                {
                    continue;
                }

                foreach (var bloque in bloques.EnumerateArray())
                {
                    var esTexto =
                        bloque.TryGetProperty(
                            "type",
                            out var tipo
                        ) &&
                        tipo.GetString() == "output_text";

                    if (!esTexto)
                    {
                        continue;
                    }

                    if (bloque.TryGetProperty(
                            "text",
                            out var texto))
                    {
                        var resultado =
                            texto.GetString();

                        if (!string.IsNullOrWhiteSpace(
                                resultado))
                        {
                            return resultado;
                        }
                    }
                }
            }

            throw new InvalidOperationException(
                "OpenAI no devolvió un análisis utilizable."
            );
        }

        // =====================================================
        // GEMINI
        // =====================================================

        private async Task<ResultadoAnalisisIA>
            AnalizarConGeminiAsync(
                string instrucciones,
                string modelo,
                string claveApi)
        {
            var esquema = CrearEsquema();

            var solicitud = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",

                        parts = new[]
                        {
                            new
                            {
                                text = instrucciones
                            }
                        }
                    }
                },

                generationConfig = new
                {
                    responseMimeType =
                        "application/json",

                    responseSchema =
                        esquema
                }
            };

            var modeloSeguro =
                Uri.EscapeDataString(modelo);

            var direccion =
                "https://generativelanguage.googleapis.com/" +
                $"v1beta/models/{modeloSeguro}:generateContent";

            var cliente =
                _httpClientFactory.CreateClient();

            using var mensaje =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    direccion
                );

            mensaje.Headers.Add(
                "x-goog-api-key",
                claveApi
            );

            mensaje.Content =
                CrearContenidoJson(solicitud);

            using var respuesta =
                await cliente.SendAsync(mensaje);

            var contenido =
                await respuesta.Content
                    .ReadAsStringAsync();

            if (!respuesta.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    "Gemini rechazó la solicitud. " +
                    $"Código HTTP: {(int)respuesta.StatusCode}. " +
                    "Compruebe la clave API y el modelo."
                );
            }

            using var documento =
                JsonDocument.Parse(contenido);

            var textoJson =
                ExtraerTextoGemini(
                    documento.RootElement
                );

            return ConvertirResultado(
                textoJson,
                "Gemini"
            );
        }

        private static string ExtraerTextoGemini(
            JsonElement raiz)
        {
            if (!raiz.TryGetProperty(
                    "candidates",
                    out var candidatos) ||
                candidatos.GetArrayLength() == 0)
            {
                throw new InvalidOperationException(
                    "Gemini no devolvió candidatos."
                );
            }

            var primerCandidato =
                candidatos[0];

            if (!primerCandidato.TryGetProperty(
                    "content",
                    out var contenido) ||
                !contenido.TryGetProperty(
                    "parts",
                    out var partes))
            {
                throw new InvalidOperationException(
                    "Gemini no devolvió contenido."
                );
            }

            foreach (var parte in partes.EnumerateArray())
            {
                if (parte.TryGetProperty(
                        "text",
                        out var texto))
                {
                    var resultado =
                        texto.GetString();

                    if (!string.IsNullOrWhiteSpace(
                            resultado))
                    {
                        return resultado;
                    }
                }
            }

            throw new InvalidOperationException(
                "Gemini no devolvió un análisis utilizable."
            );
        }

        // =====================================================
        // CONSTRUIR PROMPT
        // =====================================================

        private static string ConstruirInstrucciones(
            Vacante vacante,
            string textoCurriculum,
            PromptIA prompt)
        {
            var textoLimitado =
                textoCurriculum.Length > 60000
                    ? textoCurriculum[..60000]
                    : textoCurriculum;

            return $"""
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
        }

        // =====================================================
        // ESQUEMA JSON
        // =====================================================

        private static object CrearEsquema()
        {
            return new
            {
                type = "object",

                properties = new
                {
                    nombre = new
                    {
                        type = "string"
                    },

                    correo = new
                    {
                        type = "string"
                    },

                    telefono = new
                    {
                        type = "string"
                    },

                    puntuacion = new
                    {
                        type = "integer",
                        minimum = 0,
                        maximum = 100
                    },

                    resumen = new
                    {
                        type = "string"
                    },

                    fortalezas = new
                    {
                        type = "string"
                    },

                    debilidades = new
                    {
                        type = "string"
                    },

                    recomendacion = new
                    {
                        type = "string"
                    }
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
        }

        // =====================================================
        // DESCIFRAR CLAVE API
        // =====================================================

        private string ObtenerClaveApi(
            ConfiguracionIA configuracion)
        {
            if (string.IsNullOrWhiteSpace(
                    configuracion.ClaveApiProtegida))
            {
                throw new InvalidOperationException(
                    "No existe una clave API configurada."
                );
            }

            try
            {
                return _protector.Unprotect(
                    configuracion.ClaveApiProtegida
                );
            }
            catch
            {
                throw new InvalidOperationException(
                    "No fue posible descifrar la clave API. " +
                    "Guárdela nuevamente desde Configuración de IA."
                );
            }
        }

        // =====================================================
        // VALIDACIONES
        // =====================================================

        private static void ValidarConfiguracion(
            ConfiguracionIA configuracion,
            PromptIA prompt)
        {
            if (configuracion == null)
            {
                throw new InvalidOperationException(
                    "No existe una configuración de IA."
                );
            }

            if (string.IsNullOrWhiteSpace(
                    configuracion.Proveedor))
            {
                throw new InvalidOperationException(
                    "No se ha seleccionado un proveedor de IA."
                );
            }

            if (string.IsNullOrWhiteSpace(
                    configuracion.Modelo))
            {
                throw new InvalidOperationException(
                    "No se ha seleccionado un modelo de IA."
                );
            }

            if (prompt == null ||
                string.IsNullOrWhiteSpace(
                    prompt.Contenido))
            {
                throw new InvalidOperationException(
                    "El prompt de análisis está vacío."
                );
            }
        }

        // =====================================================
        // CONVERTIR RESULTADO
        // =====================================================

        private ResultadoAnalisisIA ConvertirResultado(
            string textoJson,
            string proveedor)
        {
            try
            {
                var resultado =
                    JsonSerializer.Deserialize<
                        ResultadoAnalisisIA
                    >(
                        textoJson,
                        _opcionesJson
                    );

                if (resultado == null)
                {
                    throw new JsonException();
                }

                return resultado;
            }
            catch (JsonException)
            {
                throw new InvalidOperationException(
                    $"{proveedor} devolvió una respuesta " +
                    "que no pudo interpretarse."
                );
            }
        }

        private static StringContent CrearContenidoJson(
            object solicitud)
        {
            return new StringContent(
                JsonSerializer.Serialize(solicitud),
                Encoding.UTF8,
                "application/json"
            );
        }
    }
}