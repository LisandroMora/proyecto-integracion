/* eslint-disable react-hooks/set-state-in-effect */
"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { api, ApiError } from "@/lib/api";
import { DataTable, type Column } from "@/components/DataTable";
import { Spinner } from "@/components/Spinner";
import { useToast } from "@/components/Toast";

type TipoTransaccion = 1 | 2; // 1 = Ingreso, 2 = Deduccion
type TipoMovimiento = 1 | 2; // 1 = Debito, 2 = Credito
type EstadoEnvio = 0 | 1 | 2; // Pendiente | Enviado | Fallido
type EstadoVerificacion = 0 | 1 | 2 | 3; // NoVerificado | Confirmado | NoEncontrado | Divergente
type EstadoRegistro = 0 | 1; // Inactivo (reabierto) | Activo

type AsientoPreview = {
  tipoTransaccion: TipoTransaccion;
  conceptoId: number;
  conceptoNombre: string;
  monto: number;
  cantidadTransacciones: number;
  descripcion: string;
  esComplementario: boolean;
  mensajeError: string | null;
};

type AsientoDetalle = {
  cuenta: number;
  cuentaCodigo: string;
  cuentaNombre: string;
  tipoMovimiento: TipoMovimiento;
  monto: number;
};

type Asiento = {
  id: number;
  anio: number;
  mes: number;
  tipoTransaccion: TipoTransaccion;
  conceptoNombre: string;
  descripcion: string;
  monto: number;
  fechaAsiento: string;
  cantidadTransacciones: number;
  estado: EstadoRegistro;
  estadoEnvio: EstadoEnvio;
  numeroAsiento: number | null;
  fechaEnvio: string | null;
  mensajeError: string | null;
  estadoVerificacion: EstadoVerificacion;
  fechaVerificacion: string | null;
  mensajeVerificacion: string | null;
  detalles: AsientoDetalle[];
};

/** Asiento que Contabilidad tiene del período sin respaldo nuestro. */
type EntradaHuerfana = {
  numeroAsiento: number | null;
  descripcion: string;
  monto: number;
  estado: string;
};

type VerificacionPeriodo = {
  confirmados: number;
  noEncontrados: number;
  divergentes: number;
  huerfanas: EntradaHuerfana[];
};

type Reapertura = { asientoId: number; transaccionesReabiertas: number };

const currency = new Intl.NumberFormat("es-DO", {
  style: "currency",
  currency: "DOP",
  maximumFractionDigits: 2,
});

const MESES = [
  "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
  "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre",
];

const hoy = new Date();

function EstadoEnvioBadge({ estado }: { estado: EstadoEnvio | null }) {
  const map: Record<string, { label: string; cls: string }> = {
    "0": { label: "Pendiente", cls: "bg-slate-800 text-slate-300 ring-slate-700" },
    "1": { label: "Enviado", cls: "bg-emerald-950/60 text-emerald-300 ring-emerald-900" },
    "2": { label: "Fallido", cls: "bg-rose-950/60 text-rose-300 ring-rose-900" },
  };
  const it = map[String(estado ?? 0)];
  return (
    <span
      className={
        "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset " + it.cls
      }
    >
      {it.label}
    </span>
  );
}

function VerificacionBadge({ estado }: { estado: EstadoVerificacion }) {
  const map: Record<string, { label: string; cls: string }> = {
    "0": { label: "Sin verificar", cls: "bg-slate-800 text-slate-400 ring-slate-700" },
    "1": { label: "Confirmado", cls: "bg-emerald-950/60 text-emerald-300 ring-emerald-900" },
    "2": { label: "No encontrado", cls: "bg-rose-950/60 text-rose-300 ring-rose-900" },
    "3": { label: "Divergente", cls: "bg-amber-950/60 text-amber-300 ring-amber-900" },
  };
  const it = map[String(estado ?? 0)];
  return (
    <span
      className={
        "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset " + it.cls
      }
    >
      {it.label}
    </span>
  );
}

function TipoBadge({ tipo }: { tipo: TipoTransaccion }) {
  return (
    <span
      className={
        "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset " +
        (tipo === 1
          ? "bg-emerald-950/60 text-emerald-300 ring-emerald-900"
          : "bg-rose-950/60 text-rose-300 ring-rose-900")
      }
    >
      {tipo === 1 ? "Ingreso" : "Deducción"}
    </span>
  );
}

export default function CierreContablePage() {
  const toast = useToast();
  const [anio, setAnio] = useState(hoy.getFullYear());
  const [mes, setMes] = useState(hoy.getMonth() + 1);

  const [preview, setPreview] = useState<AsientoPreview[]>([]);
  const [historial, setHistorial] = useState<Asiento[]>([]);
  const [huerfanas, setHuerfanas] = useState<EntradaHuerfana[]>([]);
  const [loading, setLoading] = useState(true);
  const [enviando, setEnviando] = useState(false);
  const [verificando, setVerificando] = useState(false);
  const [reintentandoId, setReintentandoId] = useState<number | null>(null);
  const [reabriendoId, setReabriendoId] = useState<number | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    // Las huérfanas salen de la verificación, no del período: al recargar dejan
    // de ser válidas hasta que se vuelva a verificar.
    setHuerfanas([]);
    try {
      const [p, h] = await Promise.all([
        api<AsientoPreview[]>(`/api/asientos-contables/preview?anio=${anio}&mes=${mes}`),
        api<Asiento[]>(`/api/asientos-contables?anio=${anio}&mes=${mes}`),
      ]);
      setPreview(p);
      setHistorial(h);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo cargar el período.");
    } finally {
      setLoading(false);
    }
  }, [anio, mes, toast]);

  useEffect(() => {
    void load();
  }, [load]);

  // El preview ya trae solo lo pendiente: lo contabilizado no vuelve a aparecer.
  const pendientes = preview;

  const totales = useMemo(() => {
    let ingresos = 0;
    let deducciones = 0;
    for (const p of preview) {
      if (p.tipoTransaccion === 1) ingresos += p.monto;
      else deducciones += p.monto;
    }
    return { ingresos, deducciones, neto: ingresos - deducciones };
  }, [preview]);

  async function enviar() {
    if (pendientes.length === 0) return;
    const msg =
      `Se enviarán ${pendientes.length} asiento(s) al Sistema de Contabilidad ` +
      `para ${MESES[mes - 1]} ${anio}. Esta acción no se puede deshacer desde aquí. ¿Continuar?`;
    if (!window.confirm(msg)) return;

    setEnviando(true);
    try {
      const res = await api<Asiento[]>("/api/asientos-contables/enviar", {
        method: "POST",
        body: { anio, mes },
      });
      const ok = res.filter((a) => a.estadoEnvio === 1).length;
      const fallidos = res.filter((a) => a.estadoEnvio === 2).length;
      if (fallidos > 0) {
        toast.error(`${ok} asiento(s) enviados, ${fallidos} fallaron. Revise el detalle.`);
      } else {
        toast.success(`${ok} asiento(s) enviados a Contabilidad.`);
      }
      await load();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo completar el envío.");
    } finally {
      setEnviando(false);
    }
  }

  async function verificar() {
    setVerificando(true);
    try {
      const r = await api<VerificacionPeriodo>("/api/asientos-contables/verificar", {
        method: "POST",
        body: { anio, mes },
      });

      // load() limpia las huérfanas, así que se asignan después de recargar.
      await load();
      setHuerfanas(r.huerfanas);

      const problemas = r.noEncontrados + r.divergentes;
      if (r.confirmados === 0 && problemas === 0) {
        toast.success("Este período no tiene asientos enviados que verificar.");
      } else if (problemas === 0) {
        toast.success(`${r.confirmados} asiento(s) confirmados en Contabilidad.`);
      } else {
        toast.error(
          `${r.confirmados} confirmado(s) · ${r.noEncontrados} no encontrado(s) · ` +
            `${r.divergentes} divergente(s).`
        );
      }
    } catch (err) {
      // Un fallo de red no marca nada: no saber si está no es lo mismo que no esté.
      toast.error(
        err instanceof ApiError ? err.message : "No se pudo verificar contra Contabilidad."
      );
    } finally {
      setVerificando(false);
    }
  }

  async function reabrir(id: number) {
    const msg =
      "Se dará de baja este asiento y sus transacciones volverán a quedar pendientes " +
      "de contabilizar, para incluirlas en un próximo envío. Hazlo solo si Contabilidad " +
      "realmente no lo tiene. ¿Continuar?";
    if (!window.confirm(msg)) return;

    setReabriendoId(id);
    try {
      const r = await api<Reapertura>(`/api/asientos-contables/${id}/reabrir`, { method: "POST" });
      toast.success(`${r.transaccionesReabiertas} transacción(es) volvieron a pendientes.`);
      await load();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo reabrir el asiento.");
    } finally {
      setReabriendoId(null);
    }
  }

  async function reintentar(id: number) {
    setReintentandoId(id);
    try {
      const a = await api<Asiento>(`/api/asientos-contables/${id}/reintentar`, { method: "POST" });
      if (a.estadoEnvio === 1) toast.success(`Asiento enviado. Número ${a.numeroAsiento}.`);
      else toast.error(a.mensajeError ?? "El reintento falló.");
      await load();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo reintentar.");
    } finally {
      setReintentandoId(null);
    }
  }

  const previewColumns: Column<AsientoPreview>[] = [
    { header: "Tipo", cell: (r) => <TipoBadge tipo={r.tipoTransaccion} />, className: "w-28" },
    { header: "Concepto", cell: (r) => r.conceptoNombre },
    {
      header: "Transacciones",
      cell: (r) => <span className="tabular-nums">{r.cantidadTransacciones}</span>,
      className: "w-32 text-right",
    },
    {
      header: "Monto",
      cell: (r) => <span className="font-medium tabular-nums">{currency.format(r.monto)}</span>,
      className: "w-40 text-right",
    },
    /* {
      header: "Nota",
      cell: (r) => (
        <div className="space-y-1">
          {r.esComplementario && (
            <div className="text-xs text-amber-300">
              Complementario · el concepto ya tuvo un cierre
            </div>
          )}
          {r.mensajeError && (
            <div className="text-xs text-rose-300">Intento anterior: {r.mensajeError}</div>
          )}
          {!r.esComplementario && !r.mensajeError && (
            <span className="text-xs text-slate-500">—</span>
          )}
        </div>
      ),
      className: "w-64",
    }, */
  ];

  const historialColumns: Column<Asiento>[] = [
    {
      header: "Asiento",
      cell: (r) =>
        r.numeroAsiento === null ? (
          <span className="text-slate-500">—</span>
        ) : (
          <span className="font-mono text-slate-100">#{r.numeroAsiento}</span>
        ),
      className: "w-24",
    },
    { header: "Tipo", cell: (r) => <TipoBadge tipo={r.tipoTransaccion} />, className: "w-28" },
    { header: "Concepto", cell: (r) => r.conceptoNombre },
    {
      header: "Movimientos",
      cell: (r) => (
        <div className="space-y-0.5">
          {r.detalles.map((d) => (
            <div key={`${d.cuentaCodigo}-${d.tipoMovimiento}`} className="text-xs text-slate-400">
              <span
                className={
                  "font-mono font-medium " +
                  (d.tipoMovimiento === 1 ? "text-sky-300" : "text-amber-300")
                }
              >
                {d.tipoMovimiento === 1 ? "DB" : "CR"}
              </span>{" "}
              {d.cuentaCodigo} · {d.cuentaNombre}
            </div>
          ))}
        </div>
      ),
    },
    {
      header: "Monto",
      cell: (r) => <span className="font-medium tabular-nums">{currency.format(r.monto)}</span>,
      className: "w-40 text-right",
    },
    {
      header: "Envío",
      cell: (r) => (
        <div className="space-y-1">
          <EstadoEnvioBadge estado={r.estadoEnvio} />
          {r.mensajeError && (
            <div className="text-xs text-rose-300 max-w-xs">{r.mensajeError}</div>
          )}
        </div>
      ),
      className: "w-56",
    },
    {
      header: "Verificación",
      cell: (r) => (
        <div className="space-y-1">
          <VerificacionBadge estado={r.estadoVerificacion} />
          {r.estado === 0 && (
            <div className="text-xs text-slate-400">
              Reabierto · sus transacciones volvieron a pendientes
            </div>
          )}
          {r.mensajeVerificacion && (
            <div className="text-xs text-slate-400 max-w-xs">{r.mensajeVerificacion}</div>
          )}
        </div>
      ),
      className: "w-64",
    },
  ];

  return (
    <div className="w-full space-y-6">
      <header>
        <h1 className="text-2xl font-semibold text-slate-100">Cierre Contable</h1>
        <p className="mt-1 text-sm text-slate-500">
          Genera un asiento por cada concepto del período y lo envía al Sistema de Contabilidad.
        </p>
      </header>

      <section className="rounded-md border border-slate-800 bg-slate-900 p-4">
        <div className="flex flex-col sm:flex-row sm:items-end gap-3">
          <div>
            <label htmlFor="mes" className="block text-xs font-medium mb-1 text-slate-400">
              Mes
            </label>
            <select
              id="mes"
              value={mes}
              onChange={(e) => setMes(Number(e.target.value))}
              className="rounded border border-slate-700 bg-slate-900 px-2.5 py-1.5 text-sm text-slate-100 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-600"
            >
              {MESES.map((m, i) => (
                <option key={m} value={i + 1}>
                  {m}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="anio" className="block text-xs font-medium mb-1 text-slate-400">
              Año
            </label>
            <input
              id="anio"
              type="number"
              min={2000}
              max={2100}
              value={anio}
              onChange={(e) => setAnio(Number(e.target.value))}
              className="w-28 rounded border border-slate-700 bg-slate-900 px-2.5 py-1.5 text-sm text-slate-100 tabular-nums focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-600"
            />
          </div>

          <div className="flex-1" />

          <button
            onClick={() => void verificar()}
            disabled={verificando || enviando || loading}
            className="rounded border border-slate-700 bg-slate-900 text-sm text-slate-300 px-4 py-2 hover:bg-slate-800 hover:text-slate-100 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            {verificando ? <Spinner size="sm" label="Verificando…" /> : "Verificar en Contabilidad"}
          </button>

          <button
            onClick={() => void enviar()}
            disabled={enviando || loading || pendientes.length === 0}
            className="rounded bg-sky-600 text-white text-sm px-4 py-2 hover:bg-sky-500 disabled:bg-slate-800 disabled:text-slate-500 disabled:cursor-not-allowed transition-colors"
          >
            {enviando ? (
              <Spinner size="sm" label="Enviando…" />
            ) : (
              `Enviar a Contabilidad (${pendientes.length})`
            )}
          </button>
        </div>
      </section>

      {/* {preview.length > 0 && (
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div className="rounded-md border border-slate-800 bg-slate-900 px-4 py-3">
            <div className="text-xs text-slate-400">Ingresos por contabilizar</div>
            <div className="mt-1 text-lg font-semibold tabular-nums text-emerald-300">
              {currency.format(totales.ingresos)}
            </div>
          </div>
          <div className="rounded-md border border-slate-800 bg-slate-900 px-4 py-3">
            <div className="text-xs text-slate-400">Deducciones por contabilizar</div>
            <div className="mt-1 text-lg font-semibold tabular-nums text-rose-300">
              {currency.format(totales.deducciones)}
            </div>
          </div>
          <div className="rounded-md border border-slate-800 bg-slate-900 px-4 py-3">
            <div className="text-xs text-slate-400">Neto por contabilizar</div>
            <div
              className={
                "mt-1 text-lg font-semibold tabular-nums " +
                (totales.neto < 0 ? "text-rose-300" : "text-slate-100")
              }
            >
              {currency.format(totales.neto)}
            </div>
          </div>
        </div>
      )} */}

      <section className="space-y-2">
        <h2 className="text-sm font-medium text-slate-300">
          Asientos a generar | {MESES[mes - 1]} {anio}
        </h2>
        <DataTable
          columns={previewColumns}
          rows={preview}
          rowKey={(r) => `${r.tipoTransaccion}-${r.conceptoId}`}
          emptyLabel={
            loading ? (
              <Spinner label="Cargando…" />
            ) : (
              "No hay transacciones pendientes de contabilizar en este período."
            )
          }
        />
      </section>

      <section className="space-y-2">
        <h2 className="text-sm font-medium text-slate-300">Asientos registrados</h2>
        <DataTable
          columns={historialColumns}
          rows={historial}
          rowKey={(r) => r.id}
          emptyLabel={loading ? <Spinner label="Cargando…" /> : "Aún no se ha enviado nada de este período."}
          actions={(row) => {
            if (row.estado === 0) return <span className="text-xs text-slate-500">—</span>;

            if (row.estadoEnvio !== 1) {
              return (
                <button
                  onClick={() => void reintentar(row.id)}
                  disabled={reintentandoId === row.id}
                  className="rounded border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-300 hover:bg-slate-800 hover:text-slate-100 disabled:opacity-40 transition-colors"
                >
                  {reintentandoId === row.id ? <Spinner size="sm" label="…" /> : "Reintentar"}
                </button>
              );
            }

            // Reabrir solo aparece sobre lo que la verificación no encontró: es la
            // única evidencia de que reenviarlo no va a duplicar el asiento.
            if (row.estadoVerificacion === 2) {
              return (
                <button
                  onClick={() => void reabrir(row.id)}
                  disabled={reabriendoId === row.id}
                  className="rounded border border-rose-900 bg-rose-950/40 px-2 py-1 text-xs text-rose-300 hover:bg-rose-950 hover:text-rose-200 disabled:opacity-40 transition-colors"
                >
                  {reabriendoId === row.id ? <Spinner size="sm" label="…" /> : "Reabrir"}
                </button>
              );
            }

            return <span className="text-xs text-slate-500">—</span>;
          }}
        />
      </section>

      {huerfanas.length > 0 && (
        <section className="space-y-2">
          <h2 className="text-sm font-medium text-amber-300">
            En Contabilidad sin respaldo nuestro
          </h2>
          <p className="text-xs text-slate-500">
            Asientos de este período que Contabilidad tiene bajo nuestro auxiliar y que no
            corresponden a ninguno de los nuestros. Suele significar que un envío se registró
            dos veces; hay que corregirlo del lado de ellos.
          </p>
          <div className="rounded-md border border-amber-900/60 bg-amber-950/20 divide-y divide-amber-900/40">
            {huerfanas.map((h, i) => (
              <div
                key={`${h.numeroAsiento ?? "s/n"}-${i}`}
                className="flex items-center justify-between gap-4 px-4 py-2 text-sm"
              >
                <span className="font-mono text-xs text-amber-300">
                  {h.numeroAsiento === null ? "s/n" : `#${h.numeroAsiento}`}
                </span>
                <span className="flex-1 text-slate-300">{h.descripcion}</span>
                <span className="text-xs text-slate-500">{h.estado}</span>
                <span className="tabular-nums text-slate-100">{currency.format(h.monto)}</span>
              </div>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
