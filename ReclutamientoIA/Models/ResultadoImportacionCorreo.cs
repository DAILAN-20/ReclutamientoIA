namespace ReclutamientoIA.Models;

public class ResultadoImportacionCorreo
{
    public int CorreosRevisados { get; set; }

    public int PdfEncontrados { get; set; }

    public int Importados { get; set; }

    public int Duplicados { get; set; }

    public int Errores { get; set; }

    public List<string> DetallesErrores { get; set; } =
        new();
}