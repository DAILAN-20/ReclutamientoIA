using System.Security.Cryptography;

namespace ReclutamientoIA.Services
{
    public interface ICurriculumService
    {
        Task<string> CalcularHashAsync(IFormFile archivo);

        Task<string> GuardarAsync(IFormFile archivo);

        bool EsPdfValido(IFormFile archivo);

        void Eliminar(string nombreArchivoGuardado);
        Stream? AbrirLectura(string nombreArchivoGuardado);

    }

    public class CurriculumService : ICurriculumService
    {

        private const long TamanoMaximo = 10 * 1024 * 1024;

        private readonly string _carpetaCurriculums;

        public CurriculumService(IWebHostEnvironment entorno)
        {
            _carpetaCurriculums = Path.Combine(
                entorno.ContentRootPath,
                "Storage",
                "Curriculums"
            );
        }

        public bool EsPdfValido(IFormFile archivo)
        {
            if (archivo == null ||
                archivo.Length == 0 ||
                archivo.Length > TamanoMaximo)
            {
                return false;
            }

            if (!string.Equals(
                    Path.GetExtension(archivo.FileName),
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            using var lector = archivo.OpenReadStream();
            var encabezado = new byte[5];

            if (lector.Read(encabezado, 0, encabezado.Length) != 5)
            {
                return false;
            }

            return encabezado[0] == 0x25 &&
                   encabezado[1] == 0x50 &&
                   encabezado[2] == 0x44 &&
                   encabezado[3] == 0x46 &&
                   encabezado[4] == 0x2D;
        }

        public async Task<string> CalcularHashAsync(
            IFormFile archivo)
        {
            await using var contenido = archivo.OpenReadStream();
            var hash = await SHA256.HashDataAsync(contenido);

            return Convert.ToHexString(hash);
        }

        public async Task<string> GuardarAsync(IFormFile archivo)
        {
            Directory.CreateDirectory(_carpetaCurriculums);

            var nombreSeguro = $"{Guid.NewGuid():N}.pdf";

            var rutaCompleta = Path.Combine(
                _carpetaCurriculums,
                nombreSeguro
            );

            await using var destino = new FileStream(
                rutaCompleta,
                FileMode.CreateNew
            );

            await archivo.CopyToAsync(destino);

            return nombreSeguro;
        }
        public void Eliminar(string nombreArchivoGuardado)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivoGuardado))
            {
                return;
            }

            var nombreSeguro = Path.GetFileName(
                nombreArchivoGuardado
            );

            if (nombreSeguro != nombreArchivoGuardado)
            {
                return;
            }

            var rutaCompleta = Path.Combine(
                _carpetaCurriculums,
                nombreSeguro
            );

            if (File.Exists(rutaCompleta))
            {
                File.Delete(rutaCompleta);
            }
        }
        public Stream? AbrirLectura(string nombreArchivoGuardado)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivoGuardado))
            {
                return null;
            }

            var nombreSeguro = Path.GetFileName(
                nombreArchivoGuardado
            );

            if (nombreSeguro != nombreArchivoGuardado)
            {
                return null;
            }

            var rutaCompleta = Path.Combine(
                _carpetaCurriculums,
                nombreSeguro
            );

            if (!File.Exists(rutaCompleta))
            {
                return null;
            }

            return new FileStream(
                rutaCompleta,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );
        }
    }
}