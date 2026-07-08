import type { EstadoRegistro } from "@/lib/resource";

export function EstadoBadge({ estado }: { estado: EstadoRegistro }) {
  const isActivo = estado === 1;
  return (
    <span
      className={
        "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium " +
        (isActivo
          ? "bg-emerald-950/60 text-emerald-300 ring-1 ring-inset ring-emerald-900"
          : "bg-slate-800 text-slate-400 ring-1 ring-inset ring-slate-700")
      }
    >
      {isActivo ? "Activo" : "Inactivo"}
    </span>
  );
}
