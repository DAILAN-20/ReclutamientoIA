using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using ReclutamientoIA.Data;
using ReclutamientoIA.Models;

namespace ReclutamientoIA.Services;

public class CorreoCurriculumsService :
    ICorreoCurriculumsService
{
    private readonly ReclutamientoDbContext _context;
    private readonly ICurriculumService _curriculumService;
    private readonly IDataProtector _protectorCorreo;

    public CorreoCurriculumsService(
        ReclutamientoDbContext context,
        ICurriculumService curriculumService,
        IDataProtectionProvider dataProtectionProvider)
    {
        _context = context;
        _curriculumService = curriculumService;

        _protectorCorreo =
            dataProtectionProvider.CreateProtector(
                "ReclutamientoIA.ClaveCorreo"
            );
    }

    public async Task<ResultadoImportacionCorreo> ImportarAsync(
        CancellationToken cancellationToken = default)
    {
        var resultado =
            new ResultadoImportacionCorreo();

        var configuracion = await _context
            .ConfiguracionesCorreo
            .AsNoTracking()
            .Where(c => c.Activo)
            .OrderBy(c => c.IdConfiguracionCorreo)
            .FirstOrDefaultAsync(cancellationToken);

        if (configuracion == null)
        {
            throw new InvalidOperationException(
                "No existe una configuración de correo activa."
            );
        }

        if (string.IsNullOrWhiteSpace(
                configuracion.ClaveProtegida))
        {
            throw new InvalidOperationException(
                "No existe una contraseña de aplicación configurada."
            );
        }

        string clave;

        try
        {
            clave = _protectorCorreo.Unprotect(
                configuracion.ClaveProtegida
            );
        }
        catch
        {
            throw new InvalidOperationException(
                "No fue posible descifrar la contraseña del correo. " +
                "Guárdela nuevamente desde Configuración del correo."
            );
        }

        using var cliente = new ImapClient();

        try
        {
            await cliente.ConnectAsync(
                configuracion.ServidorImap,
                configuracion.PuertoImap,
                SecureSocketOptions.SslOnConnect,
                cancellationToken
            );

            await cliente.AuthenticateAsync(
                configuracion.Usuario,
                clave,
                cancellationToken
            );

            var bandeja = cliente.Inbox;

            await bandeja.OpenAsync(
                FolderAccess.ReadOnly,
                cancellationToken
            );

            var cuentaCorreo = configuracion.Usuario
                .Trim()
                .ToLowerInvariant();

            var nombreCarpeta = bandeja.FullName;

            var uidValidity =
                (long)bandeja.UidValidity;

            var uidsRegistrados = await _context
                .CorreosProcesados
                .AsNoTracking()
                .Where(c =>
                    c.CuentaCorreo == cuentaCorreo &&
                    c.Carpeta == nombreCarpeta &&
                    c.UidValidity == uidValidity
                )
                .Select(c => c.UidCorreo)
                .ToListAsync(cancellationToken);

            var conjuntoUids =
                uidsRegistrados.ToHashSet();

            var identificadoresEncontrados =
                await bandeja.SearchAsync(
                    SearchQuery.GMailRawSearch(
                        "has:attachment filename:pdf"
                    ),
                    cancellationToken
                );

            var identificadores =
                identificadoresEncontrados
                    .Where(uid =>
                        !conjuntoUids.Contains(
                            (long)uid.Id
                        )
                    )
                    .ToList();

            foreach (var identificador in identificadores)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                MimeMessage mensaje;

                try
                {
                    mensaje =
                        await bandeja.GetMessageAsync(
                            identificador,
                            cancellationToken
                        );

                    resultado.CorreosRevisados++;
                }
                catch
                {
                    resultado.Errores++;

                    resultado.DetallesErrores.Add(
                        "No se pudo leer el correo " +
                        $"{identificador.Id}."
                    );

                    continue;
                }

                var remitente = mensaje.From
                    .Mailboxes
                    .FirstOrDefault();

                var cantidadPdfCorreo = 0;

                foreach (var adjunto in mensaje.Attachments)
                {
                    if (adjunto is not MimePart parte)
                    {
                        continue;
                    }

                    var nombrePredeterminado =
                        $"curriculum-{identificador.Id}.pdf";

                    var nombreArchivo =
                        string.IsNullOrWhiteSpace(
                            parte.FileName
                        )
                            ? nombrePredeterminado
                            : Path.GetFileName(
                                parte.FileName
                            ) ?? nombrePredeterminado;

                    var esPdf =
                        string.Equals(
                            parte.ContentType?.MimeType,
                            "application/pdf",
                            StringComparison.OrdinalIgnoreCase
                        ) ||
                        nombreArchivo.EndsWith(
                            ".pdf",
                            StringComparison.OrdinalIgnoreCase
                        );

                    if (!esPdf)
                    {
                        continue;
                    }

                    cantidadPdfCorreo++;
                    resultado.PdfEncontrados++;

                    await ImportarAdjuntoAsync(
                        parte,
                        nombreArchivo,
                        remitente,
                        mensaje.Subject,
                        mensaje.Date.UtcDateTime,
                        resultado,
                        cancellationToken
                    );
                }

                var correoProcesado = new CorreoProcesado
                {
                    CuentaCorreo = cuentaCorreo,
                    Carpeta = nombreCarpeta,
                    UidValidity = uidValidity,
                    UidCorreo = identificador.Id,

                    MessageId = Recortar(
                        mensaje.MessageId,
                        500
                    ),

                    Asunto = Recortar(
                        mensaje.Subject,
                        250
                    ),

                    CantidadPdf = cantidadPdfCorreo,
                    FechaProcesamiento = DateTime.Now
                };

                try
                {
                    _context.CorreosProcesados.Add(
                        correoProcesado
                    );

                    await _context.SaveChangesAsync(
                        cancellationToken
                    );
                }
                catch (DbUpdateException)
                {
                    // Otro proceso pudo registrar el mismo UID.
                    _context.ChangeTracker.Clear();
                }
            }

            return resultado;
        }
        finally
        {
            if (cliente.IsConnected)
            {
                await cliente.DisconnectAsync(
                    true,
                    CancellationToken.None
                );
            }
        }
    }

    private async Task ImportarAdjuntoAsync(
        MimePart parte,
        string nombreArchivo,
        MailboxAddress? remitente,
        string? asuntoCorreo,
        DateTime fechaCorreo,
        ResultadoImportacionCorreo resultado,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var memoria =
                new MemoryStream();

            await parte.Content.DecodeToAsync(
                memoria,
                cancellationToken
            );

            memoria.Position = 0;

            var archivo = new FormFile(
                memoria,
                0,
                memoria.Length,
                "ArchivoCv",
                nombreArchivo
            )
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            if (!_curriculumService.EsPdfValido(
                archivo
            ))
            {
                resultado.Errores++;

                resultado.DetallesErrores.Add(
                    $"{nombreArchivo}: el PDF no es válido " +
                    "o supera 10 MB."
                );

                return;
            }

            var hash = await _curriculumService
                .CalcularHashAsync(archivo);

            var candidatoExistente =
                await _context.Candidatos
                    .FirstOrDefaultAsync(
                        c => c.HashArchivo == hash,
                        cancellationToken
                    );

            if (candidatoExistente != null)
            {
                if (string.IsNullOrWhiteSpace(
                    candidatoExistente.OrigenRegistro
                ))
                {
                    candidatoExistente.OrigenRegistro =
                        "Correo";
                }
                else if (!candidatoExistente.OrigenRegistro.Contains(
                    "Correo",
                    StringComparison.OrdinalIgnoreCase
                ))
                {
                    candidatoExistente.OrigenRegistro =
                        "Manual y Correo";
                }

                candidatoExistente.AsuntoCorreo =
                    string.IsNullOrWhiteSpace(asuntoCorreo)
                        ? "Sin asunto"
                        : asuntoCorreo.Trim();

                candidatoExistente.FechaCorreo =
                    fechaCorreo;

                if (string.IsNullOrWhiteSpace(
                        candidatoExistente.Correo
                    ) &&
                    !string.IsNullOrWhiteSpace(
                        remitente?.Address
                    ))
                {
                    candidatoExistente.Correo =
                        remitente.Address.Trim();
                }

                await _context.SaveChangesAsync(
                    cancellationToken
                );

                resultado.Duplicados++;
                return;
            }

            var nombreGuardado =
                await _curriculumService.GuardarAsync(
                    archivo
                );

            var candidato = new Candidato
            {
                NombreCompleto =
                    string.IsNullOrWhiteSpace(
                        remitente?.Name
                    )
                        ? null
                        : remitente.Name.Trim(),

                Correo =
                    string.IsNullOrWhiteSpace(
                        remitente?.Address
                    )
                        ? null
                        : remitente.Address.Trim(),

                OrigenRegistro = "Correo",

                AsuntoCorreo =
                    string.IsNullOrWhiteSpace(
                        asuntoCorreo
                    )
                        ? "Sin asunto"
                        : asuntoCorreo.Trim(),

                FechaCorreo = fechaCorreo,

                NombreArchivoOriginal =
                    nombreArchivo,

                NombreArchivoGuardado =
                    nombreGuardado,

                TipoContenido =
                    "application/pdf",

                TamanoArchivo =
                    memoria.Length,

                HashArchivo =
                    hash,

                FechaRegistro =
                    DateTime.Now,

                Activo = true
            };

            try
            {
                _context.Candidatos.Add(
                    candidato
                );

                await _context.SaveChangesAsync(
                    cancellationToken
                );

                resultado.Importados++;
            }
            catch (DbUpdateException)
            {
                _curriculumService.Eliminar(
                    nombreGuardado
                );

                _context.ChangeTracker.Clear();

                resultado.Duplicados++;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            _context.ChangeTracker.Clear();

            resultado.Errores++;

            resultado.DetallesErrores.Add(
                $"{nombreArchivo}: no fue posible " +
                "importarlo."
            );
        }
    }

    private static string? Recortar(
        string? texto,
        int longitudMaxima)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        texto = texto.Trim();

        return texto.Length <= longitudMaxima
            ? texto
            : texto[..longitudMaxima];
    }
}
