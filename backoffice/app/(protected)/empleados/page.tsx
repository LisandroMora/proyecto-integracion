/* eslint-disable react-hooks/set-state-in-effect */
"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { ApiError } from "@/lib/api";
import { createResourceClient, type EstadoRegistro } from "@/lib/resource";
import { DataTable, type Column } from "@/components/DataTable";
import { EstadoBadge } from "@/components/EstadoBadge";
import { EstadoFilterControl, type EstadoFilter } from "@/components/EstadoFilterControl";
import { Modal } from "@/components/Modal";
import { Spinner } from "@/components/Spinner";
import { useToast } from "@/components/Toast";
import { formatCedula, maskCedulaInput } from "@/lib/format";

type EmpleadoRead = {
  id: number;
  cedula: string;
  nombre: string;
  departamento: string | null;
  puesto: string | null;
  salarioMensual: number;
  nominaId: number;
  nominaNombre: string;
  estado: EstadoRegistro;
};

type EmpleadoCreate = {
  cedula: string;
  nombre: string;
  departamento: string | null;
  puesto: string | null;
  salarioMensual: number;
  nominaId: number;
};

type EmpleadoUpdate = EmpleadoCreate & { estado: EstadoRegistro };

type NominaRead = { id: number; nombre: string; estado: EstadoRegistro };

const empleadosResource = createResourceClient<EmpleadoRead, EmpleadoCreate, EmpleadoUpdate>(
  "/api/empleados"
);

const nominasResource = createResourceClient<NominaRead, never, never>("/api/nominas");

const currency = new Intl.NumberFormat("es-DO", {
  style: "currency",
  currency: "DOP",
  maximumFractionDigits: 2,
});

type FormState = {
  cedula: string;
  nombre: string;
  departamento: string;
  puesto: string;
  salarioMensual: string;
  nominaId: number | "";
  estado: EstadoRegistro;
};

const emptyForm: FormState = {
  cedula: "",
  nombre: "",
  departamento: "",
  puesto: "",
  salarioMensual: "0",
  nominaId: "",
  estado: 1,
};

export default function EmpleadosPage() {
  const toast = useToast();
  const [rows, setRows] = useState<EmpleadoRead[]>([]);
  const [nominas, setNominas] = useState<NominaRead[]>([]);
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
      const [empleados, nominasList] = await Promise.all([
        empleadosResource.list({ estado: estadoFilter }),
        nominasResource.list(),
      ]);
      setRows(empleados);
      setNominas(nominasList);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo cargar la lista.");
    } finally {
      setLoading(false);
    }
  }, [estadoFilter, toast]);

  useEffect(() => {
    void load();
  }, [load]);

  function openCreate() {
    setEditingId(null);
    setForm({ ...emptyForm, nominaId: nominas[0]?.id ?? "" });
    setFormError(null);
    setModalOpen(true);
  }

  function openEdit(row: EmpleadoRead) {
    setEditingId(row.id);
    setForm({
      cedula: row.cedula,
      nombre: row.nombre,
      departamento: row.departamento ?? "",
      puesto: row.puesto ?? "",
      salarioMensual: String(row.salarioMensual),
      nominaId: row.nominaId,
      estado: row.estado,
    });
    setFormError(null);
    setModalOpen(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setFormError(null);
    if (!form.cedula.trim() || !form.nombre.trim()) {
      setFormError("Cédula y Nombre son obligatorios.");
      return;
    }
    if (form.nominaId === "") {
      setFormError("Debe seleccionar una nómina.");
      return;
    }
    const salario = Number(form.salarioMensual);
    if (!Number.isFinite(salario) || salario < 0) {
      setFormError("El salario debe ser un número mayor o igual a cero.");
      return;
    }

    const payload = {
      cedula: form.cedula.trim(),
      nombre: form.nombre.trim(),
      departamento: form.departamento.trim() || null,
      puesto: form.puesto.trim() || null,
      salarioMensual: salario,
      nominaId: form.nominaId,
    };

    setSaving(true);
    try {
      const isCreate = editingId === null;
      if (isCreate) {
        await empleadosResource.create(payload);
      } else {
        await empleadosResource.update(editingId, { ...payload, estado: form.estado });
      }
      setModalOpen(false);
      toast.success(isCreate ? "Empleado creado." : "Empleado actualizado.");
      await load();
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : "No se pudo guardar.");
    } finally {
      setSaving(false);
    }
  }

  async function toggleEstado(row: EmpleadoRead) {
    const target = row.estado === 1 ? "desactivar" : "reactivar";
    if (!window.confirm(`¿Seguro que desea ${target} a "${row.nombre}"?`)) return;
    try {
      if (row.estado === 1) {
        await empleadosResource.remove(row.id);
      } else {
        await empleadosResource.update(row.id, {
          cedula: row.cedula,
          nombre: row.nombre,
          departamento: row.departamento,
          puesto: row.puesto,
          salarioMensual: row.salarioMensual,
          nominaId: row.nominaId,
          estado: 1,
        });
      }
      toast.success(row.estado === 1 ? "Empleado desactivado." : "Empleado reactivado.");
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
        r.nombre.toLowerCase().includes(q) ||
        r.cedula.toLowerCase().includes(q) ||
        (r.departamento ?? "").toLowerCase().includes(q) ||
        (r.puesto ?? "").toLowerCase().includes(q)
    );
  }, [rows, query]);

  const columns: Column<EmpleadoRead>[] = [
    { header: "Cédula", cell: (r) => <span className="font-mono text-slate-300">{formatCedula(r.cedula)}</span>, className: "w-40" },
    { header: "Nombre", cell: (r) => <span className="font-medium">{r.nombre}</span> },
    { header: "Depto.", cell: (r) => r.departamento ?? "—", className: "w-32" },
    { header: "Puesto", cell: (r) => r.puesto ?? "—", className: "w-32" },
    {
      header: "Salario",
      cell: (r) => currency.format(r.salarioMensual),
      className: "w-32 text-right",
    },
    { header: "Nómina", cell: (r) => r.nominaNombre, className: "w-40" },
    { header: "Estado", cell: (r) => <EstadoBadge estado={r.estado} />, className: "w-24" },
  ];

  return (
    <div className="w-full space-y-6">
      <header className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-slate-100">Empleados</h1>
        </div>
        <button
          onClick={openCreate}
          disabled={nominas.length === 0}
          className="rounded bg-sky-600 text-white text-sm px-3 py-2 hover:bg-sky-500 disabled:opacity-40 disabled:hover:bg-sky-600 transition-colors"
          title={nominas.length === 0 ? "No hay nóminas disponibles" : ""}
        >
          + Nuevo empleado
        </button>
      </header>

      <div className="flex items-center gap-3">
        <input
          type="search"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Buscar por cédula, nombre, departamento…"
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
        emptyLabel={loading ? <Spinner label="Cargando…" /> : "Sin empleados"}
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
              {row.estado === 1 ? "Desactivar" : "Reactivar"}
            </button>
          </div>
        )}
      />

      <Modal
        open={modalOpen}
        title={editingId === null ? "Nuevo empleado" : "Editar empleado"}
        onClose={() => setModalOpen(false)}
      >
        <form onSubmit={handleSubmit} className="space-y-3">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium mb-1 text-slate-300">Cédula</label>
              <input
                type="text"
                required
                inputMode="numeric"
                autoComplete="off"
                maxLength={13}
                value={maskCedulaInput(form.cedula)}
                onChange={(e) => setForm({ ...form, cedula: maskCedulaInput(e.target.value) })}
                className="w-full rounded border border-slate-700 bg-slate-900 px-3 py-2 font-mono text-slate-100 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-600"
                placeholder="001-1234567-8"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1 text-slate-300">Nombre</label>
              <input
                type="text"
                required
                value={form.nombre}
                onChange={(e) => setForm({ ...form, nombre: e.target.value })}
                className="w-full rounded border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-600"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1 text-slate-300">Departamento</label>
              <input
                type="text"
                value={form.departamento}
                onChange={(e) => setForm({ ...form, departamento: e.target.value })}
                className="w-full rounded border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-600"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1 text-slate-300">Puesto</label>
              <input
                type="text"
                value={form.puesto}
                onChange={(e) => setForm({ ...form, puesto: e.target.value })}
                className="w-full rounded border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-600"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1 text-slate-300">Salario mensual</label>
              <input
                type="number"
                min={0}
                step="0.01"
                required
                value={form.salarioMensual}
                onChange={(e) => setForm({ ...form, salarioMensual: e.target.value })}
                className="w-full rounded border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-600"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1 text-slate-300">Nómina</label>
              <select
                required
                value={form.nominaId}
                onChange={(e) =>
                  setForm({ ...form, nominaId: e.target.value ? Number(e.target.value) : "" })
                }
                className="w-full rounded border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-600"
              >
                <option value="">— seleccionar —</option>
                {nominas.map((n) => (
                  <option key={n.id} value={n.id}>
                    {n.nombre}
                  </option>
                ))}
              </select>
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
