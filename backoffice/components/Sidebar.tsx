"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { clearSession, useSession } from "@/lib/auth";

const links = [
  { href: "/", label: "Inicio" },
  { href: "/empleados", label: "Empleados" },
  { href: "/transacciones", label: "Transacciones" },
  { href: "/tipos-ingreso", label: "Tipos de Ingreso" },
  { href: "/tipos-deduccion", label: "Tipos de Deducción" },
];

export function Sidebar() {
  const router = useRouter();
  const pathname = usePathname();
  const session = useSession();

  function logout() {
    clearSession();
    router.replace("/login");
  }

  return (
    <aside className="w-60 shrink-0 border-r border-slate-800 bg-slate-900 flex flex-col">
      <div className="border-b border-slate-800 px-4 py-4">
        <div className="text-sm font-semibold text-slate-100">Sistema de Nómina</div>
      </div>

      <nav className="flex-1 px-2 py-3 space-y-1">
        {links.map((l) => {
          const active =
            l.href === "/" ? pathname === "/" : pathname.startsWith(l.href);
          return (
            <Link
              key={l.href}
              href={l.href}
              className={
                "block rounded px-3 py-2 text-sm transition-colors " +
                (active
                  ? "bg-sky-600 text-white"
                  : "text-slate-400 hover:bg-slate-800 hover:text-slate-100")
              }
            >
              {l.label}
            </Link>
          );
        })}
      </nav>

      <div className="border-t border-slate-800 px-4 py-3 text-xs text-slate-500 space-y-2">
        <button
          onClick={logout}
          className="w-full rounded border border-slate-700 bg-slate-900 px-3 py-1.5 text-slate-300 hover:bg-slate-800 hover:text-slate-100"
        >
          Cerrar sesión
        </button>
      </div>
    </aside>
  );
}
