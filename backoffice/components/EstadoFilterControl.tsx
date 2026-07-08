"use client";

export type EstadoFilter = "activos" | "inactivos" | "todos";

const OPTIONS: { value: EstadoFilter; label: string }[] = [
  { value: "activos", label: "Activos" },
  { value: "inactivos", label: "Inactivos" },
  { value: "todos", label: "Todos" },
];

type Props = {
  value: EstadoFilter;
  onChange: (value: EstadoFilter) => void;
  disabled?: boolean;
};

export function EstadoFilterControl({ value, onChange, disabled }: Props) {
  return (
    <div className="inline-flex rounded-md border border-slate-800 bg-slate-900 p-0.5 text-xs">
      {OPTIONS.map((opt) => (
        <button
          key={opt.value}
          type="button"
          disabled={disabled}
          onClick={() => onChange(opt.value)}
          className={
            "px-3 py-1 rounded transition disabled:opacity-50 " +
            (value === opt.value
              ? "bg-sky-600 text-white"
              : "text-slate-400 hover:bg-slate-800 hover:text-slate-100")
          }
        >
          {opt.label}
        </button>
      ))}
    </div>
  );
}
