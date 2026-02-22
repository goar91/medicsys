using System;

namespace MEDICSYS.Api.Services;

/// <summary>
/// Helper centralizado para obtener la fecha/hora actual en zona horaria de Ecuador (UTC-5).
/// Todas las fechas del sistema usan este helper para mantener consistencia.
/// Se marca como DateTimeKind.Utc para compatibilidad con Npgsql/PostgreSQL timestamptz.
/// </summary>
public static class DateTimeHelper
{
    private static readonly TimeZoneInfo EcuadorZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");

    /// <summary>
    /// Retorna la fecha/hora actual en hora de Ecuador (UTC-5).
    /// </summary>
    public static DateTime Now()
    {
        var ecuadorTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EcuadorZone);
        // Se marca como Utc para que Npgsql acepte el valor en columnas timestamptz.
        return DateTime.SpecifyKind(ecuadorTime, DateTimeKind.Utc);
    }

    /// <summary>
    /// Convierte una fecha UTC a hora de Ecuador (UTC-5).
    /// </summary>
    public static DateTime ToEcuadorTime(DateTime utcDate)
    {
        var source = utcDate.Kind == DateTimeKind.Utc
            ? utcDate
            : DateTime.SpecifyKind(utcDate, DateTimeKind.Utc);
        var ecuadorTime = TimeZoneInfo.ConvertTimeFromUtc(source, EcuadorZone);
        return DateTime.SpecifyKind(ecuadorTime, DateTimeKind.Utc);
    }
}
