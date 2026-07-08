"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useHydrated, useSession } from "@/lib/auth";
import { Sidebar } from "@/components/Sidebar";
import { Spinner } from "@/components/Spinner";
import { ToastProvider } from "@/components/Toast";

export default function ProtectedLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const session = useSession();
  const hydrated = useHydrated();

  useEffect(() => {
    if (hydrated && !session) {
      router.replace("/login");
    }
  }, [hydrated, session, router]);

  if (!hydrated) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-950 text-slate-500">
        <Spinner size="lg" label="Cargando…" />
      </div>
    );
  }

  if (!session) return null;

  return (
    <ToastProvider>
      <div className="min-h-screen flex bg-slate-950 text-slate-100">
        <Sidebar />
        <main className="flex-1 p-8 overflow-auto">{children}</main>
      </div>
    </ToastProvider>
  );
}
