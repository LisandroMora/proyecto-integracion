namespace Nomina.Infrastructure.Contabilidad;

/// <summary>
/// Cachea el catálogo de cuentas de Contabilidad. Sin esto, cada envío pagaría
/// otra vez el arranque en frío de su servidor, que ha superado los 100 segundos.
/// </summary>
internal class CuentasContablesCache
{
    private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(30);

    private readonly Lock _gate = new();
    private Dictionary<string, CuentaContable>? _cuentas;
    private DateTime _expiraUtc;

    public bool TryGet(out Dictionary<string, CuentaContable> cuentas)
    {
        lock (_gate)
        {
            if (_cuentas is not null && DateTime.UtcNow < _expiraUtc)
            {
                cuentas = _cuentas;
                return true;
            }
        }

        cuentas = new Dictionary<string, CuentaContable>();
        return false;
    }

    public void Set(Dictionary<string, CuentaContable> cuentas)
    {
        lock (_gate)
        {
            _cuentas = cuentas;
            _expiraUtc = DateTime.UtcNow.Add(Vigencia);
        }
    }
}
