using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using ReclutamientoIA.Models;

namespace ReclutamientoIA.Services;

public class CorreoService : ICorreoService
{
    private readonly CorreoOptions _opciones;

    public CorreoService(IOptions<CorreoOptions> opciones)
    {
        _opciones = opciones.Value;
    }

    public async Task EnviarAsync(
        string destinatario,
        string asunto,
        string contenidoHtml,
        CancellationToken cancellationToken = default)
    {
        if (!MailboxAddress.TryParse(
            destinatario, out var direccionDestino))
        {
            throw new ArgumentException(
                "El correo del destinatario no es válido.",
                nameof(destinatario)
            );
        }

        var mensaje = new MimeMessage();

        mensaje.From.Add(new MailboxAddress(
            _opciones.NombreRemitente,
            _opciones.CorreoRemitente
        ));

        mensaje.To.Add(direccionDestino);

        mensaje.Subject = asunto;

        mensaje.Body = new TextPart("html")
        {
            Text = contenidoHtml
        };

        using var cliente = new SmtpClient();

        cliente.Timeout = 30000;

        // Puerto 465: cifrado desde el inicio.
        // Otros puertos: exigir STARTTLS.
        var seguridad = _opciones.Puerto == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        await cliente.ConnectAsync(
            _opciones.Servidor,
            _opciones.Puerto,
            seguridad,
            cancellationToken
        );

        await cliente.AuthenticateAsync(
            _opciones.Usuario,
            _opciones.Clave,
            cancellationToken
        );

        await cliente.SendAsync(
            mensaje,
            cancellationToken
        );

        await cliente.DisconnectAsync(
            true,
            cancellationToken
        );
    }
}