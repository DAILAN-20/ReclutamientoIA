using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using ReclutamientoIA.Data;
using ReclutamientoIA.Models;

namespace ReclutamientoIA.Services
{
    public class CorreoService : ICorreoService
    {
        private readonly ReclutamientoDbContext
            _context;

        private readonly IDataProtector
            _protectorCorreo;

        public CorreoService(
            ReclutamientoDbContext context,
            IDataProtectionProvider dataProtectionProvider)
        {
            _context = context;

            _protectorCorreo =
                dataProtectionProvider.CreateProtector(
                    "ReclutamientoIA.ClaveCorreo"
                );
        }

        public async Task EnviarAsync(
            string destinatario,
            string asunto,
            string contenidoHtml,
            CancellationToken cancellationToken = default)
        {
            if (!MailboxAddress.TryParse(
                    destinatario,
                    out var direccionDestino))
            {
                throw new ArgumentException(
                    "El correo del destinatario no es válido.",
                    nameof(destinatario)
                );
            }

            var configuracion = await _context
                .ConfiguracionesCorreo
                .AsNoTracking()
                .Where(c => c.Activo)
                .OrderBy(c =>
                    c.IdConfiguracionCorreo
                )
                .FirstOrDefaultAsync(
                    cancellationToken
                );

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
                clave =
                    _protectorCorreo.Unprotect(
                        configuracion.ClaveProtegida
                    );
            }
            catch
            {
                throw new InvalidOperationException(
                    "No fue posible descifrar la contraseña " +
                    "del correo. Guárdela nuevamente desde " +
                    "Configuración del correo."
                );
            }

            if (!MailboxAddress.TryParse(
                    configuracion.CorreoRemitente,
                    out var direccionRemitente))
            {
                throw new InvalidOperationException(
                    "El correo remitente configurado no es válido."
                );
            }

            var mensaje =
                new MimeMessage();

            mensaje.From.Add(
                new MailboxAddress(
                    configuracion.NombreRemitente,
                    direccionRemitente.Address
                )
            );

            mensaje.To.Add(
                direccionDestino
            );

            mensaje.Subject =
                asunto;

            mensaje.Body =
                new TextPart("html")
                {
                    Text = contenidoHtml
                };

            using var cliente =
                new SmtpClient();

            cliente.Timeout =
                30000;

            var seguridad =
                configuracion.PuertoSmtp == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

            try
            {
                await cliente.ConnectAsync(
                    configuracion.ServidorSmtp,
                    configuracion.PuertoSmtp,
                    seguridad,
                    cancellationToken
                );

                await cliente.AuthenticateAsync(
                    configuracion.Usuario,
                    clave,
                    cancellationToken
                );

                await cliente.SendAsync(
                    mensaje,
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "No fue posible enviar el correo. " +
                    "Compruebe el servidor SMTP, el puerto, " +
                    "el usuario y la contraseña de aplicación.",
                    ex
                );
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
    }
}