"use client";

import { useCallback, useEffect, useState } from "react";
import { ApiError } from "@/lib/api";
import type { EstadoRegistro, ResourceClient } from "@/lib/resource";
import { DataTable, type Column } from "./DataTable";
import { EstadoBadge } from "./EstadoBadge";
import { EstadoFilterControl, type EstadoFilter } from "./EstadoFilterControl";
import { Modal } from "./Modal";
import { Spinner } from "./Spinner";
import { useToast } from "./Toast";

export type CatalogoConceptoRead = {
  id: number;
  nombre: string;
  dependeDeSalario: boolean;
  porcentaje: number | null;
  estado: EstadoRegistro;
};

export type CatalogoConceptoCreate = {
  nombre: string;
  dependeDeSalario: boolean;
  porcentaje: number | null;
};

export type CatalogoConceptoUpdate = CatalogoConceptoCreate & {
  estado: EstadoRegistro;
};

type Props = {
  title: string;
  description: string;
  resource: ResourceClient<CatalogoConceptoRead, CatalogoConceptoCreate, CatalogoConceptoUpdate>;
};

type FormState = {
  nombre: string;
  dependeDeSalario: boolean;
  porcentaje: string;
  estado: EstadoRegistro;
};

const emptyForm: FormState = { nombre: "", dependeDeSalario: false, porcentaje: "", estado: 1 };

export function CatalogoConceptoPage({ title, description, resource }: Props) {
  const toast = useToast();
  const [rows, setRows] = useState<CatalogoConceptoRead[]>([]);
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
      const data = await resource.list({ estado: estadoFilter });
      setRows(data);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo cargar la lista.");
    } finally {
      setLoading(false);
    }
  }, [resource, estadoFilter, toast]);

  useEffect(() => {
    void load();
  }, [load]);

  function openCreate() {
    setEditingId(null);
    setForm(emptyForm);
    setFormError(null);
    setModalOpen(true);
  }

  function openEdit(row: CatalogoConceptoRead) {
    setEditingId(row.id);
    setForm({
      nombre: row.nombre,
      dependeDeSalario: row.dependeDeSalario,
      porcentaje: row.porcentaje === null ? "" : String(row.porcentaje),
      estado: row.estado,
    });
    setFormError(null);
    setModalOpen(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.nombre.trim()) {
      setFormError("El nombre es obligatorio.");
      return;
    }
    let porcentaje: number | null = null;
    if (form.dependeDeSalario) {
      const p = Number(form.porcentaje);
      if (!Number.isFinite(p) || p <= 0 || p > 100) {
        setFormError("Ingresa un porcentaje entre 0.01 y 100.");
        return;
      }
      porcentaje = p;
    }
    setSaving(true);
    setFormError(null);
    try {
      const isCreate = editingId === null;
      const payload = {
        nombre: form.nombre.trim(),
        dependeDeSalario: form.dependeDeSalario,
        porcentaje,
      };
      if (isCreate) {
        await resource.create(payload);
      } else {
        await resource.update(editingId, { ...payload, estado: form.estado });
      }
      setModalOpen(false);
      toast.success(isCreate ? "Registro creado." : "Registro actualizado.");
      await load();
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : "No se pudo guardar.");
    } finally {
      setSaving(false);
    }
  }

  async function toggleEstado(row: CatalogoConceptoRead) {
    const target = row.estado === 1 ? "desactivar" : "reactivar";
    if (!window.confirm(`¿Seguro que desea ${target} "${row.nombre}"?`)) return;
    try {
      if (row.estado === 1) {
        await resource.remove(row.id);
      } else {
        await resource.update(row.id, {
          nombre: row.nombre,
          dependeDeSalario: row.dependeDeSalario,
          porcentaje: row.porcentaje,
          estado: 1,
        });
      }
      toast.success(row.estado === 1 ? "Registro desactivado." : "Registro reactivado.");
      await load();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo actualizar el estado.");
    }
  }

  const filtered = rows.filter((r) =>
    r.nombre.toLowerCase().includes(query.trim().toLowerCase())
  );

  const columns: Column<CatalogoConceptoRead>[] = [
    { header: "ID", cell: (r) => <span className="text-slate-500">{r.id}</span>, className: "w-16" },
    { header: "Nombre", cell: (r) => <span className="font-medium text-slate-100">{r.nombre}</span> },
    {
      header: "Depende de salario",
      cell: (r) => (r.dependeDeSalario ? "Sí" : "No"),
      className: "w-40",
    },
    {
      header: "Porcentaje",
      cell: (r) =>
        r.porcentaje === null ? (
          <span className="text-slate-500">—</span>
        ) : (
          <span className="tabular-nums text-slate-200">{r.porcentaje.toFixed(2)}%</span>
        ),
      className: "w-28 text-right",
    },
    {
      header: "Estado",
      cell: (r) => <EstadoBadge estado={r.estado} />,
      className: "w-32",
    },
  ];

  return (
    <div className="w-full space-y-6">
      <header className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-slate-100">{title}</h1>
          <p className="text-sm text-slate-500">{description}</p>
        </div>
        <button
          onClick={openCreate}
          className="rounded bg-sky-600 text-white text-sm px-3 py-2 hover:bg-sky-500 transition-colors"
        >
          + Nuevo
        </button>
      </header>

      <div className="flex items-center gap-3">
        <input
          type="search"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Buscar por nombre…"
          className="flex-1 max-w-sm rounded border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-600"
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
        emptyLabel={loading ? <Spinner label="Cargando…" /> : "Sin registros"}
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
                "rounded px-2 py-1 text-xs border transition-colors " +
                (row.estado === 1
                  ? "border-rose-900 bg-rose-950/60 text-rose-300 hover:bg-rose-950 hover:text-rose-200"
                  : "border-emerald-900 bg-emerald-950/60 text-emerald-300 hover:bg-emerald-950 hover:text-emerald-200")
              }
            >
              {row.estado === 1 ? "Desactivar" : "Reactivar"}
            </button>
          </div>
        )}
      />

      <Modal
        open={modalOpen}
        title={editingId === null ? `Nuevo · ${title}` : `Editar · ${title}`}
        onClose={() => setModalOpen(false)}
      >
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1 text-slate-300">Nombre</label>
            <input
              type="text"
              required
              value={form.nombre}
              onChange={(e) => setForm({ ...form, nombre: e.target.value })}
              className="w-full rounded border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-600"
            />
          </div>

          <label className="flex items-center gap-2 text-sm text-slate-300">
            <input
              type="checkbox"
              checked={form.dependeDeSalario}
              onChange={(e) =>
                setForm({
                  ...form,
                  dependeDeSalario: e.target.checked,
                  porcentaje: e.target.checked ? form.porcentaje : "",
                })
              }
              className="rounded border-slate-700 bg-slate-800 accent-slate-100"
            />
            Depende del salario
          </label>

          {form.dependeDeSalario && (
            <div>
              <label className="block text-sm font-medium mb-1 text-slate-300">
                Porcentaje del salario
              </label>
              <div className="relative">
                <input
                  type="number"
                  min={0.01}
                  max={100}
                  step="0.01"
                  required
                  value={form.porcentaje}
                  onChange={(e) => setForm({ ...form, porcentaje: e.target.value })}
                  className="w-full rounded border border-slate-700 bg-slate-900 px-3 py-2 pr-8 text-slate-100 tabular-nums focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-600"
                  placeholder="Ej: 2.87"
                />
                <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-slate-500 text-sm">
                  %
                </span>
              </div>
              <p className="mt-1 text-xs text-slate-500">
                Se aplica sobre el salario mensual del empleado al crear una transacción.
              </p>
            </div>
          )}

          {editingId !== null && (
            <label className="flex items-center gap-2 text-sm text-slate-300">
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
