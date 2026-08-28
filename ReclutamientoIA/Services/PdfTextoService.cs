using System.Text;
using UglyToad.PdfPig;

namespace ReclutamientoIA.Services
{
    public interface IPdfTextoService
    {
        string ExtraerTexto(Stream archivoPdf);
    }

    public class PdfTextoService : IPdfTextoService
    {
        public string ExtraerTexto(Stream archivoPdf)
        {
            if (archivoPdf == null ||
                !archivoPdf.CanRead)
            {
                throw new ArgumentException(
                    "No fue posible leer el archivo PDF."
                );
            }

            var texto = new StringBuilder();

            using var documento = PdfDocument.Open(archivoPdf);

            foreach (var pagina in documento.GetPages())
            {
                texto.AppendLine(pagina.Text);
                texto.AppendLine();
            }

            var resultado = texto.ToString().Trim();

            if (string.IsNullOrWhiteSpace(resultado))
            {
                throw new InvalidOperationException(
                    "El PDF no contiene texto extraíble. Puede ser un documento escaneado como imagen."
                );
            }

            return resultado;
        }
    }
}