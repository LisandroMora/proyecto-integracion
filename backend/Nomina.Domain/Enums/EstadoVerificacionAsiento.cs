namespace Nomina.Domain.Enums;

/// <summary>
/// Resultado del último cruce del asiento contra el Sistema de Contabilidad.
/// Es independiente de <see cref="EstadoEnvioAsiento"/>: haberlo enviado es un
/// hecho histórico que no cambia, pero el asiento puede desaparecer después
/// del lado de ellos.
/// </summary>
public enum EstadoVerificacionAsiento
{
    /// <summary>Nunca se ha verificado, o se verificó antes del último envío.</summary>
    NoVerificado = 0,

    /// <summary>Contabilidad lo tiene activo y por el mismo monto.</summary>
    Confirmado = 1,

    /// <summary>Contabilidad no lo tiene, o lo tiene anulado.</summary>
    NoEncontrado = 2,

    /// <summary>Contabilidad lo tiene pero con un monto distinto al nuestro.</summary>
    Divergente = 3
}
