namespace Nomina.Infrastructure.Contabilidad;

public class ContabilidadSettings
{
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Id de nuestro sistema en el catálogo de auxiliares de Contabilidad (Nóminas = 2).</summary>
    public int AuxiliarId { get; set; } = 2;

    /// <summary>
    /// Su servicio corre en un plan gratuito: la primera petición tras un rato de
    /// inactividad ha llegado a tardar más de 100 s, por encima del timeout por
    /// defecto de HttpClient.
    /// </summary>
    public int TimeoutSegundos { get; set; } = 180;

    public int ReintentosEnvio { get; set; } = 2;

    public CuentasSettings Cuentas { get; set; } = new();

    /// <summary>
    /// Se configuran los <b>códigos</b> contables, no los ids: el código es estable y
    /// el id cambia si Contabilidad recarga su base. Los ids se resuelven en caliente
    /// contra su catálogo.
    /// </summary>
    public class CuentasSettings
    {
        /// <summary>Gasto de Nómina. Débito de los ingresos.</summary>
        public string GastoNomina { get; set; } = "501";

        /// <summary>Nómina por Pagar. Crédito de los ingresos y débito de las deducciones.</summary>
        public string NominaPorPagar { get; set; } = "202";

        /// <summary>
        /// Cuentas por Pagar. Crédito de las deducciones: la retención al empleado
        /// se convierte en una deuda con un tercero (AFP, DGII).
        /// </summary>
        public string RetencionesPorPagar { get; set; } = "201";
    }
}
