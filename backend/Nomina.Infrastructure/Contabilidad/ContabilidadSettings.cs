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
    /// <para>
    /// El techo es el límite de petición de App Service (~230 s). El costo de
    /// despertar su servidor se paga una sola vez, al consultar el catálogo de
    /// cuentas; los envíos posteriores del mismo cierre ya lo encuentran caliente.
    /// </para>
    /// </summary>
    public int TimeoutSegundos { get; set; } = 90;

    public int ReintentosEnvio { get; set; } = 2;

    public CuentasSettings Cuentas { get; set; } = new();

    /// <summary>
    /// Se configuran los <b>códigos</b> contables, no los ids: el código es estable y
    /// el id cambia si Contabilidad recarga su base. Los ids se resuelven en caliente
    /// contra su catálogo.
    /// </summary>
    public class CuentasSettings
    {
        /// <summary>Gasto de Nómina. Débito de todo asiento de nómina.</summary>
        public string GastoNomina { get; set; } = "501";

        /// <summary>Nómina por Pagar. Crédito de todo asiento de nómina.</summary>
        public string NominaPorPagar { get; set; } = "202";
    }
}
