import type { ReactNode } from "react";

export type Column<T> = {
  header: string;
  cell: (row: T) => ReactNode;
  className?: string;
};

type DataTableProps<T> = {
  columns: Column<T>[];
  rows: T[];
  rowKey: (row: T) => string | number;
  emptyLabel?: ReactNode;
  actions?: (row: T) => ReactNode;
};

export function DataTable<T>({
  columns,
  rows,
  rowKey,
  emptyLabel = "Sin registros",
  actions,
}: DataTableProps<T>) {
  return (
    <div className="overflow-hidden rounded-md border border-slate-800 bg-slate-900">
      <table className="min-w-full divide-y divide-slate-800 text-sm">
        <thead className="bg-slate-900/60">
          <tr>
            {columns.map((c) => (
              <th
                key={c.header}
                className={
                  "px-4 py-2 text-left font-medium text-slate-400 " + (c.className ?? "")
                }
              >
                {c.header}
              </th>
            ))}
            {actions && <th className="px-4 py-2 text-right font-medium text-slate-400">Acciones</th>}
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-800">
          {rows.length === 0 ? (
            <tr>
              <td
                colSpan={columns.length + (actions ? 1 : 0)}
                className="px-4 py-8 text-center text-slate-500"
              >
                {emptyLabel}
              </td>
            </tr>
          ) : (
            rows.map((row) => (
              <tr key={rowKey(row)} className="hover:bg-slate-800/50 transition-colors">
                {columns.map((c) => (
                  <td key={c.header} className={"px-4 py-2 text-slate-200 " + (c.className ?? "")}>
                    {c.cell(row)}
                  </td>
                ))}
                {actions && <td className="px-4 py-2 text-right">{actions(row)}</td>}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}
