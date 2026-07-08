"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { ApiError } from "@/lib/api";
import { createResourceClient } from "@/lib/resource";
import { useSession } from "@/lib/auth";

const empleadosResource = createResourceClient<{ id: number }, never, never>("/api/empleados");
const transaccionesResource = createResourceClient<{ id: number }, never, never>("/api/transacciones");

export default function Home() {
  const session = useSession();
  const [empleadosCount, setEmpleadosCount] = useState<number | null>(null);
  const [transaccionesCount, setTransaccionesCount] = useState<number | null>(null);
  const [statsError, setStatsError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [empleados, transacciones] = await Promise.all([
          empleadosResource.list(),
          transaccionesResource.list(),
        ]);
        if (cancelled) return;
        setEmpleadosCount(empleados.length);
        setTransaccionesCount(transacciones.length);
      } catch (err) {
        if (cancelled) return;
        setStatsError(err instanceof ApiError ? err.message : "No se pudieron cargar los indicadores.");
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div className="w-full space-y-6">
      <header>
        <h1 className="text-3xl font-semibold text-slate-100">Sistema de Nómina</h1>
      </header>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Link
          href="/empleados"
          className="rounded-md border border-slate-800 bg-slate-900 p-5 hover:border-slate-600 hover:bg-slate-900/70 transition"
        >
          <div className="text-sm font-medium text-slate-400">Empleados activos</div>
          <div className="mt-2 text-3xl font-semibold tabular-nums text-slate-100">
            {empleadosCount === null ? "—" : empleadosCount}
          </div>
          <div className="mt-1 text-xs text-slate-500">Personal habilitado en el sistema.</div>
        </Link>

        <Link
          href="/transacciones"
          className="rounded-md border border-slate-800 bg-slate-900 p-5 hover:border-slate-600 hover:bg-slate-900/70 transition"
        >
          <div className="text-sm font-medium text-slate-400">Transacciones activas</div>
          <div className="mt-2 text-3xl font-semibold tabular-nums text-slate-100">
            {transaccionesCount === null ? "—" : transaccionesCount}
          </div>
          <div className="mt-1 text-xs text-slate-500">Ingresos y deducciones vigentes.</div>
        </Link>
      </div>

      {statsError && (
        <div className="rounded border border-rose-900 bg-rose-950/60 px-3 py-2 text-sm text-rose-200">
          {statsError}
        </div>
      )}

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Link
          href="/empleados"
          className="rounded-md border border-slate-800 bg-slate-900 p-4 hover:border-slate-600 hover:bg-slate-900/70 transition"
        >
          <div className="font-medium text-slate-100">Empleados</div>
          <div className="text-sm text-slate-500 mt-1">Personal asignado a una nómina.</div>
        </Link>

        <Link
          href="/transacciones"
          className="rounded-md border border-slate-800 bg-slate-900 p-4 hover:border-slate-600 hover:bg-slate-900/70 transition"
        >
          <div className="font-medium text-slate-100">Transacciones</div>
          <div className="text-sm text-slate-500 mt-1">Ingresos y deducciones aplicados al personal.</div>
        </Link>

        <Link
          href="/tipos-ingreso"
          className="rounded-md border border-slate-800 bg-slate-900 p-4 hover:border-slate-600 hover:bg-slate-900/70 transition"
        >
          <div className="font-medium text-slate-100">Tipos de Ingreso</div>
          <div className="text-sm text-slate-500 mt-1">Catálogo de conceptos que suman en la nómina.</div>
        </Link>

        <Link
          href="/tipos-deduccion"
          className="rounded-md border border-slate-800 bg-slate-900 p-4 hover:border-slate-600 hover:bg-slate-900/70 transition"
        >
          <div className="font-medium text-slate-100">Tipos de Deducción</div>
          <div className="text-sm text-slate-500 mt-1">Catálogo de conceptos que restan en la nómina.</div>
        </Link>
      </div>
    </div>
  );
}
