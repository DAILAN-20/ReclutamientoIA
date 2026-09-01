using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Options;
using ReclutamientoIA.Models;

namespace ReclutamientoIA.Services;

public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly GoogleCalendarOptions _options;
    private CalendarService? _calendarService;

    public GoogleCalendarService(
        IOptions<GoogleCalendarOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> CrearEventoAsync(
        Entrevista entrevista,
        AnalisisIA analisis,
        CancellationToken cancellationToken = default)
    {
        var servicio = await ObtenerServicioAsync(
            cancellationToken
        );

        var evento = ConstruirEvento(
            entrevista,
            analisis
        );

        var solicitud = servicio.Events.Insert(
            evento,
            _options.CalendarId
        );

        var eventoCreado = await solicitud.ExecuteAsync(
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(eventoCreado.Id))
        {
            throw new InvalidOperationException(
                "Google Calendar no devolvió el identificador " +
                "del evento."
            );
        }

        return eventoCreado.Id;
    }

    public async Task ActualizarEventoAsync(
        string googleEventId,
        Entrevista entrevista,
        AnalisisIA analisis,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(googleEventId))
        {
            throw new ArgumentException(
                "El identificador del evento es obligatorio.",
                nameof(googleEventId)
            );
        }

        var servicio = await ObtenerServicioAsync(
            cancellationToken
        );

        var evento = ConstruirEvento(
            entrevista,
            analisis
        );

        var solicitud = servicio.Events.Update(
            evento,
            _options.CalendarId,
            googleEventId
        );

        await solicitud.ExecuteAsync(cancellationToken);
    }

    public async Task EliminarEventoAsync(
        string googleEventId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(googleEventId))
        {
            return;
        }

        var servicio = await ObtenerServicioAsync(
            cancellationToken
        );

        var solicitud = servicio.Events.Delete(
            _options.CalendarId,
            googleEventId
        );

        await solicitud.ExecuteAsync(cancellationToken);
    }

    private async Task<CalendarService> ObtenerServicioAsync(
        CancellationToken cancellationToken)
    {
        if (_calendarService != null)
        {
            return _calendarService;
        }

        var carpetaTokens = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData
            ),
            "ReclutamientoIA",
            "GoogleCalendarTokens"
        );

        Directory.CreateDirectory(carpetaTokens);

        var credencial = await GoogleWebAuthorizationBroker
            .AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = _options.ClientId,
                    ClientSecret = _options.ClientSecret
                },
                new[]
                {
                    CalendarService.Scope.CalendarEvents
                },
                "reclutamiento-ia",
                cancellationToken,
                new FileDataStore(carpetaTokens, true)
            );

        _calendarService = new CalendarService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credencial,
                ApplicationName = "Reclutamiento IA"
            }
        );

        return _calendarService;
    }

    private Event ConstruirEvento(
        Entrevista entrevista,
        AnalisisIA analisis)
    {
        var fechaHoraInicio =
            entrevista.FechaEntrevista.Date
                .Add(entrevista.HoraEntrevista);

        var fechaHoraFinal = fechaHoraInicio.AddMinutes(
            _options.DuracionMinutos
        );

        var nombreCandidato =
            analisis.Candidato.NombreCompleto ??
            "Candidato";

        var tituloVacante =
            analisis.Vacante.Titulo;

        var descripcion = string.Join(
            Environment.NewLine,
            $"Candidato: {nombreCandidato}",
            $"Vacante: {tituloVacante}",
            $"Entrevistador: {entrevista.Entrevistador}",
            string.IsNullOrWhiteSpace(
                entrevista.EnlaceEntrevista
            )
                ? string.Empty
                : $"Enlace: {entrevista.EnlaceEntrevista}",
            string.IsNullOrWhiteSpace(
                entrevista.ComentariosIniciales
            )
                ? string.Empty
                : $"Comentarios: " +
                  entrevista.ComentariosIniciales
        );

        return new Event
        {
            Summary =
                $"Entrevista - {nombreCandidato}",

            Description = descripcion,

            Location =
                entrevista.EnlaceEntrevista,

            Start = new EventDateTime
            {
                DateTimeDateTimeOffset =
                    ConvertirAZonaHoraria(
                        fechaHoraInicio
                    ),

                TimeZone =
                    _options.ZonaHoraria
            },

            End = new EventDateTime
            {
                DateTimeDateTimeOffset =
                    ConvertirAZonaHoraria(
                        fechaHoraFinal
                    ),

                TimeZone =
                    _options.ZonaHoraria
            },

            Visibility = "private"
        };
    }

    private DateTimeOffset ConvertirAZonaHoraria(
        DateTime fechaHora)
    {
        TimeZoneInfo zonaHoraria;

        try
        {
            zonaHoraria =
                TimeZoneInfo.FindSystemTimeZoneById(
                    _options.ZonaHoraria
                );
        }
        catch (TimeZoneNotFoundException)
        {
            zonaHoraria =
                TimeZoneInfo.FindSystemTimeZoneById(
                    "Central America Standard Time"
                );
        }

        var fechaSinZona = DateTime.SpecifyKind(
            fechaHora,
            DateTimeKind.Unspecified
        );

        var diferencia = zonaHoraria.GetUtcOffset(
            fechaSinZona
        );

        return new DateTimeOffset(
            fechaSinZona,
            diferencia
        );
    }
}