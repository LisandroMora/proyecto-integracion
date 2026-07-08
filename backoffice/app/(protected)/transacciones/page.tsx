/* eslint-disable react-hooks/set-state-in-effect */
"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { ApiError } from "@/lib/api";
import { createResourceClient, type EstadoRegistro } from "@/lib/resource";
import { DataTable, type Column } from "@/components/DataTable";
import { EstadoBadge } from "@/components/EstadoBadge";
import { EstadoFilterControl, type EstadoFilter } from "@/components/EstadoFilterControl";
import { Modal } from "@/components/Modal";
import { Spinner } from "@/components/Spinner";
import { useToast } from "@/components/Toast";
import { formatCedula } from "@/lib/format";

type TipoTransaccion = 1 | 2; // 1 = Ingreso, 2 = Deduccion

type TransaccionRead = {
  id: number;
  empleadoId: number;
  empleadoCedula: string;
  empleadoNombre: string;
  tipoTransaccion: TipoTransaccion;
  conceptoId: number;
  conceptoNombre: string;
  fecha: string;
  monto: number;
  estado: EstadoRegistro;
};

type TransaccionCreate = {
  empleadoId: number;
  tipoTransaccion: TipoTransaccion;
  conceptoId: number;
  fecha: string;
  monto: number;
};

type TransaccionUpdate = TransaccionCreate & { estado: EstadoRegistro };

type EmpleadoLite = {
  id: number;
  cedula: string;
  nombre: string;
  salarioMensual: number;
  estado: EstadoRegistro;
};
type ConceptoLite = {
  id: number;
  nombre: string;
  dependeDeSalario: boolean;
  porcentaje: number | null;
  estado: EstadoRegistro;
};

const transaccionesResource = createResourceClient<TransaccionRead, TransaccionCreate, TransaccionUpdate>(
  "/api/transacciones"
);
const empleadosResource = createResourceClient<EmpleadoLite, never, never>("/api/empleados");
const tiposIngresoResource = createResourceClient<ConceptoLite, never, never>("/api/tipos-ingreso");
const tiposDeduccionResource = createResourceClient<ConceptoLite, never, never>("/api/tipos-deduccion");

const currency = new Intl.NumberFormat("es-DO", {
  style: "currency",
  currency: "DOP",
  maximumFractionDigits: 2,
});

const isoDate = (d: string | Date) => new Date(d).toISOString().slice(0, 10);
const today = () => new Date().toISOString().slice(0, 10);

type FormState = {
  empleadoId: number | "";
  tipoTransaccion: TipoTransaccion;
  conceptoId: number | "";
  fecha: string;
  monto: string;
  estado: EstadoRegistro;
};

const emptyForm: FormState = {
  empleadoId: "",
  tipoTransaccion: 1,
  conceptoId: "",
  fecha: today(),
  monto: "0",
  estado: 1,
};

export default function TransaccionesPage() {
  const toast = useToast();
  const [rows, setRows] = useState<TransaccionRead[]>([]);
  const [empleados, setEmpleados] = useState<EmpleadoLite[]>([]);
  const [ingresos, setIngresos] = useState<ConceptoLite[]>([]);
  const [deducciones, setDeducciones] = useState<ConceptoLite[]>([]);
  const [loading, setLoading] = useState(true);
  const [query, setQuery] = useState("");
  const [estadoFilter, setEstadoFilter] = useState<EstadoFilter>("activos");

  const [modalOpen, setModalOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [tx, emp, ti, td] = await Promise.all([
        transaccionesResource.list({ estado: estadoFilter }),
        empleadosResource.list(),
        tiposIngresoResource.list(),
        tiposDeduccionResource.list(),
      ]);
      setRows(tx);
      setEmpleados(emp);
      setIngresos(ti);
      setDeducciones(td);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo cargar la lista.");
    } finally {
      setLoading(false);
    }
  }, [estadoFilter, toast]);

  useEffect(() => {
    void load();
  }, [load]);

  const empleadosActivos = useMemo(() => empleados.filter((e) => e.estado === 1), [empleados]);
  const conceptosActivos = useMemo(
    () => (form.tipoTransaccion === 1 ? ingresos : deducciones).filter((c) => c.estado === 1),
    [form.tipoTransaccion, ingresos, deducciones]
  );

  function openCreate() {
    setEditingId(null);
    setForm({ ...emptyForm, empleadoId: empleadosActivos[0]?.id ?? "" });
    setFormError(null);
    setModalOpen(true);
  }

  function openEdit(row: TransaccionRead) {
    setEditingId(row.id);
    setForm({
      empleadoId: row.empleadoId,
      tipoTransaccion: row.tipoTransaccion,
      conceptoId: row.conceptoId,
      fecha: isoDate(row.fecha),
      monto: String(row.monto),
      estado: row.estado,
    });
    setFormError(null);
    setModalOpen(true);
  }

  const conceptoSeleccionado = useMemo(() => {
    if (form.conceptoId === "") return null;
    const conceptos = form.tipoTransaccion === 1 ? ingresos : deducciones;
    return conceptos.find((c) => c.id === form.conceptoId) ?? null;
  }, [form.conceptoId, form.tipoTransaccion, ingresos, deducciones]);

  const empleadoSeleccionado = useMemo(() => {
    if (form.empleadoId === "") return null;
    return empleados.find((e) => e.id === form.empleadoId) ?? null;
  }, [form.empleadoId, empleados]);

  const montoLocked =
    conceptoSeleccionado?.dependeDeSalario === true &&
    conceptoSeleccionado.porcentaje !== null;

  function computeAutoMonto(
    empleadoId: number | "",
    conceptoId: number | "",
    tipo: TipoTransaccion
  ): string | null {
    if (empleadoId === "" || conceptoId === "") return null;
    const conceptos = tipo === 1 ? ingresos : deducciones;
    const c = conceptos.find((x) => x.id === conceptoId);
    if (!c || !c.dependeDeSalario || c.porcentaje === null) return null;
    const emp = empleados.find((x) => x.id === empleadoId);
    if (!emp) return null;
    const raw = emp.salarioMensual * c.porcentaje / 100;
    return (Math.round(raw * 100) / 100).toFixed(2);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setFormError(null);
    if (form.empleadoId === "" || form.conceptoId === "") {
      setFormError("Debe seleccionar un empleado y un concepto.");
      return;
    }
    const monto = Number(form.monto);
    if (!Number.isFinite(monto) || monto < 0) {
      setFormError("El monto debe ser un número mayor o igual a cero.");
      return;
    }

    const payload = {
      empleadoId: form.empleadoId,
      tipoTransaccion: form.tipoTransaccion,
      conceptoId: form.conceptoId,
      fecha: form.fecha,
      monto,
    };

    setSaving(true);
    try {
      const isCreate = editingId === null;
      if (isCreate) {
        await transaccionesResource.create(payload);
      } else {
        await transaccionesResource.update(editingId, { ...payload, estado: form.estado });
      }
      setModalOpen(false);
      toast.success(isCreate ? "Transacción registrada." : "Transacción actualizada.");
      await load();
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : "No se pudo guardar.");
    } finally {
      setSaving(false);
    }
  }

  async function toggleEstado(row: TransaccionRead) {
    const target = row.estado === 1 ? "anular" : "reactivar";
    if (!window.confirm(`¿Seguro que desea ${target} esta transacción?`)) return;
    try {
      if (row.estado === 1) {
        await transaccionesResource.remove(row.id);
      } else {
        await transaccionesResource.update(row.id, {
          empleadoId: row.empleadoId,
          tipoTransaccion: row.tipoTransaccion,
          conceptoId: row.conceptoId,
          fecha: isoDate(row.fecha),
          monto: row.monto,
          estado: 1,
        });
      }
      toast.success(row.estado === 1 ? "Transacción anulada." : "Transacción reactivada.");
      await load();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo actualizar el estado.");
    }
  }

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return rows;
    return rows.filter(
      (r) =>
        r.empleadoNombre.toLowerCase().includes(q) ||
        r.empleadoCedula.toLowerCase().includes(q) ||
        r.conceptoNombre.toLowerCase().includes(q)
    );
  }, [rows, query]);

  const columns: Column<TransaccionRead>[] = [
    {
      header: "Fecha",
      cell: (r) => new Date(r.fecha).toLocaleDateString("es-DO"),
      className: "w-28",
    },
    {
      header: "Empleado",
      cell: (r) => (
        <div>
          <div className="font-medium text-slate-100">{r.empleadoNombre}</div>
          <div className="text-xs text-slate-500 font-mono">{formatCedula(r.empleadoCedula)}</div>
        </div>
      ),
    },
    {
      header: "Tipo",
      cell: (r) => (
        <span
          className={
            "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset " +
            (r.tipoTransaccion === 1
              ? "bg-emerald-950/60 text-emerald-300 ring-emerald-900"
              : "bg-rose-950/60 text-rose-300 ring-rose-900")
          }
        >
          {r.tipoTransaccion === 1 ? "Ingreso" : "Deducción"}
        </span>
      ),
      className: "w-28",
    },
    { header: "Concepto", cell: (r) => r.conceptoNombre },
    {
      header: "Monto",
      cell: (r) => (
        <span
          className={
            "font-medium " + (r.tipoTransaccion === 1 ? "text-emerald-300" : "text-rose-300")
          }
        >
          {r.tipoTransaccion === 1 ? "+" : "−"}
          {currency.format(r.monto)}
        </span>
      ),
      className: "w-40 text-right",
    },
    { header: "Estado", cell: (r) => <EstadoBadge estado={r.estado} />, className: "w-24" },
  ];

  const noHayEmpleadosActivos = !loading && empleadosActivos.length === 0;

  return (
    <div className="w-full space-y-6">
      <header className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-slate-100">Transacciones</h1>
        </div>
        <button
          onClick={openCreate}
          disabled={noHayEmpleadosActivos}
          className="rounded bg-sky-600 text-white text-sm px-3 py-2 hover:bg-sky-500 disabled:bg-slate-800 disabled:text-slate-500 disabled:cursor-not-allowed transition-colors"
        >
          + Nueva transacción
        </button>
      </header>

      {noHayEmpleadosActivos && (
        <div className="rounded-md border border-amber-900 bg-amber-950/50 px-4 py-3 text-sm text-amber-200 flex items-start justify-between gap-3">
          <div>
            <div className="font-medium text-amber-100">No puedes registrar transacciones aún</div>
            <div className="text-amber-300/80 mt-0.5">
              No hay empleados activos. Reactiva uno existente o crea uno nuevo desde el módulo de Empleados.
            </div>
          </div>
          <Link
            href="/empleados"
            className="shrink-0 rounded border border-amber-800 bg-amber-950/40 px-3 py-1.5 text-amber-200 hover:bg-amber-900/40 hover:text-amber-100 transition-colors"
          >
            Ir a Empleados →
          </Link>
        </div>
      )}

      <div className="flex items-center gap-3">
        <input
          type="search"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Buscar por empleado, cédula o concepto…"
          className="flex-1 max-w-md rounded border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-600"
        />
        <EstadoFilterControl value={estadoFilter} onChange={setEstadoFilter} disabled={loading} />
        <div className="text-xs text-slate-500">
          {loading ? <Spinner size="sm" label="Cargando…" /> : `${filtered.length} de ${rows.length}`}
        </div>
      </div>

      <DataTable
        columns={columns}
        rows={filtered}
        rowKey={(r) => r.id}
        emptyLabel={loading ? <Spinner label="Cargando…" /> : "Sin transacciones"}
        actions={(row) => (
          <div className="flex justify-end gap-2">
            <button
              onClick={() => openEdit(row)}
              className="rounded border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-300 hover:bg-slate-800 hover:text-slate-100 transition-colors"
            >
              Editar
            </button>
            <button
              onClick={() => void toggleEstado(row)}
              className={
                "rounded px-2 py-1 text-xs border " +
                (row.estado === 1
                  ? "border-rose-900 bg-rose-950/60 text-rose-300 hover:bg-rose-950 hover:text-rose-200"
                  : "border-emerald-900 bg-emerald-950/60 text-emerald-300 hover:bg-emerald-950 hover:text-emerald-200") +
                " transition-colors"
              }
            >
              {row.estado === 1 ? "Anular" : "Reactivar"}
            </button>
          </div>
        )}
      />

      <Modal
        open={modalOpen}
        title={editingId === null ? "Nueva transacción" : "Editar transacción"}
        onClose={() => setModalOpen(false)}
      >
        <form onSubmit={handleSubmit} className="space-y-3">
          <div>
            <label className="block text-sm font-medium mb-1 text-slate-300">Empleado</label>
            <select
              required
              value={form.empleadoId}
              onChange={(e) => {
                const empleadoId = e.target.value ? Number(e.target.value) : "";
                const auto = computeAutoMonto(empleadoId, form.conceptoId, form.tipoTransaccion);
                setForm({ ...form, empleadoId, monto: auto ?? form.monto });
              }}
              className="w-full rounded border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-600"
            >
              <option value="">— seleccionar —</option>
              {empleadosActivos.map((emp) => (
                <option key={emp.id} value={emp.id}>
                  {emp.nombre} · {formatCedula(emp.cedula)}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium mb-2 text-slate-300">Tipo</label>
            <div className="grid grid-cols-2 gap-2">
              {(
                [
                  { value: 1, label: "Ingreso", accent: "emerald" },
                  { value: 2, label: "Deducción", accent: "rose" },
                ] as const
              ).map((opt) => {
                const active = form.tipoTransaccion === opt.value;
                const base = "rounded border px-3 py-2 text-sm cursor-pointer transition-colors ";
                const on =
                  opt.accent === "emerald"
                    ? "border-emerald-700 bg-emerald-950/60 text-emerald-200"
                    : "border-rose-700 bg-rose-950/60 text-rose-200";
                const off = "border-slate-700 bg-slate-900 text-slate-400 hover:bg-slate-800 hover:text-slate-200";
                return (
                  <label key={opt.value} className={base + (active ? on : off)}>
                    <input
                      type="radio"
                      name="tipoTransaccion"
                      className="sr-only"
                      checked={active}
                      onChange={() =>
                        setForm({
                          ...form,
                          tipoTransaccion: opt.value as TipoTransaccion,
                          conceptoId: "",
                        })
                      }
                    />
                    {opt.label}
                  </label>
                );
              })}
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium mb-1 text-slate-300">
              Concepto{" "}
              <span className="text-xs text-slate-400">
                ({form.tipoTransaccion === 1 ? "Tipos de Ingreso" : "Tipos de Deducción"})
              </span>
            </label>
            <select
              required
              value={form.conceptoId}
              onChange={(e) => {
                const conceptoId = e.target.value ? Number(e.target.value) : "";
                const auto = computeAutoMonto(form.empleadoId, conceptoId, form.tipoTransaccion);
                setForm({ ...form, conceptoId, monto: auto ?? form.monto });
              }}
              className="w-full rounded border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-600"
              disabled={conceptosActivos.length === 0}
            >
              <option value="">— seleccionar —</option>
              {conceptosActivos.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.nombre}
                </option>
              ))}
            </select>
            {conceptosActivos.length === 0 && (
              <div className="mt-1 text-xs text-amber-300">
                No hay {form.tipoTransaccion === 1 ? "tipos de ingreso" : "tipos de deducción"} activos.
              </div>
            )}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium mb-1 text-slate-300">Fecha</label>
              <input
                type="date"
                required
                value={form.fecha}
                onChange={(e) => setForm({ ...form, fecha: e.target.value })}
                className="w-full rounded border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-600"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1 text-slate-300">Monto</label>
              <input
                type="number"
                min={0}
                step="0.01"
                required
                readOnly={montoLocked}
                tabIndex={montoLocked ? -1 : undefined}
                aria-readonly={montoLocked || undefined}
                value={form.monto}
                onChange={(e) => setForm({ ...form, monto: e.target.value })}
                className={
                  "w-full rounded border px-3 py-2 tabular-nums focus:outline-none " +
                  (montoLocked
                    ? "border-slate-800 bg-slate-950 text-slate-400 cursor-not-allowed"
                    : "border-slate-700 bg-slate-900 text-slate-100 focus:ring-2 focus:ring-sky-500/30 focus:border-sky-600")
                }
              />
              {montoLocked && empleadoSeleccionado && conceptoSeleccionado?.porcentaje !== null && (
                <p className="mt-1 text-xs text-sky-300">
                  {conceptoSeleccionado?.porcentaje?.toFixed(2)}% de{" "}
                  {currency.format(empleadoSeleccionado.salarioMensual)}. Calculado automáticamente.
                </p>
              )}
            </div>
          </div>

          {editingId !== null && (
            <label className="flex items-center gap-2 text-sm pt-1">
              <input
                type="checkbox"
                checked={form.estado === 1}
                onChange={(e) => setForm({ ...form, estado: e.target.checked ? 1 : 0 })}
                className="rounded border-slate-700 bg-slate-800 accent-slate-100"
              />
              Activo
            </label>
          )}

          {formError && <div className="text-sm text-rose-400">{formError}</div>}

          <div className="flex justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={() => setModalOpen(false)}
              className="rounded border border-slate-700 bg-slate-900 px-3 py-1.5 text-sm text-slate-300 hover:bg-slate-800 hover:text-slate-100 transition-colors"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={saving}
              className="rounded bg-sky-600 px-3 py-1.5 text-sm text-white hover:bg-sky-500 disabled:opacity-40 disabled:hover:bg-sky-600 transition-colors"
            >
              {saving ? <Spinner size="sm" label="Guardando…" /> : "Guardar"}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
