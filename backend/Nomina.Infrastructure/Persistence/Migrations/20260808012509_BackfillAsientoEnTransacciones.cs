using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nomina.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Enlaza retroactivamente las transacciones con los asientos que ya se habían
    /// enviado a Contabilidad antes de que existiera Transaccion.AsientoContableId.
    /// <para>
    /// Sin esto, esas transacciones quedan como pendientes de contabilizar y el
    /// siguiente cierre las volvería a enviar, duplicando asientos en Contabilidad
    /// —que no controla duplicados—. El índice único que antes lo impedía se retiró
    /// en la migración anterior.
    /// </para>
    /// </summary>
    public partial class BackfillAsientoEnTransacciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El período del asiento se compara contra el de la fecha de la
            // transacción, que es exactamente el criterio con que se agrupó al
            // generarlo. Solo cuenta lo aceptado por Contabilidad (EstadoEnvio = 1).
            migrationBuilder.Sql(@"
                UPDATE t
                SET t.AsientoContableId = a.Id
                FROM Transacciones t
                INNER JOIN AsientosContables a
                    ON  a.TipoTransaccion = t.TipoTransaccion
                    AND a.ConceptoId      = t.ConceptoId
                    AND a.Anio            = YEAR(t.Fecha)
                    AND a.Mes             = MONTH(t.Fecha)
                WHERE t.AsientoContableId IS NULL
                  AND t.Estado    = 1
                  AND a.Estado    = 1
                  AND a.EstadoEnvio = 1;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No se puede distinguir lo que enlazó este backfill de lo que enlazó
            // un cierre posterior, así que revertir es deliberadamente un no-op.
        }
    }
}
