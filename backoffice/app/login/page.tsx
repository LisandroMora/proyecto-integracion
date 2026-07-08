"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { API_URL, ApiError } from "@/lib/api";
import { saveSession, useSession, type Session } from "@/lib/auth";
import { Spinner } from "@/components/Spinner";

export default function LoginPage() {
  const router = useRouter();
  const session = useSession();
  const [email, setEmail] = useState("admin@nomina.local");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (session) router.replace("/");
  }, [session, router]);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const res = await fetch(`${API_URL}/api/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password }),
      });

      if (res.status === 401) {
        setError("Credenciales inválidas.");
        return;
      }
      if (!res.ok) {
        throw new ApiError(res.status, `HTTP ${res.status}`);
      }

      const data = (await res.json()) as Session;
      saveSession(data);
      router.replace("/");
    } catch {
      setError("No se pudo contactar con la API. ¿Está corriendo en " + API_URL + "?");
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="min-h-screen flex items-center justify-center p-6 bg-slate-950 text-slate-100">
      <form
        onSubmit={handleSubmit}
        className="w-full max-w-sm space-y-4 rounded-lg border border-slate-800 bg-slate-900 p-6 shadow-2xl shadow-slate-950/50"
      >
        <div>
          <h1 className="text-2xl font-semibold text-slate-100">Sistema de Nómina</h1>
        </div>

        <div>
          <label htmlFor="email" className="block text-sm font-medium mb-1 text-slate-300">
            Email
          </label>
          <input
            id="email"
            type="email"
            required
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="w-full rounded border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-600"
          />
        </div>

        <div>
          <label htmlFor="password" className="block text-sm font-medium mb-1 text-slate-300">
            Contraseña
          </label>
          <input
            id="password"
            type="password"
            required
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="w-full rounded border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-600"
          />
        </div>

        {error && (
          <div className="text-sm text-rose-200 bg-rose-950/60 border border-rose-900 rounded px-3 py-2">
            {error}
          </div>
        )}

        <button
          type="submit"
          disabled={loading}
          className="w-full rounded bg-sky-600 text-white py-2 font-medium hover:bg-sky-500 disabled:opacity-40 disabled:hover:bg-sky-600 transition-colors"
        >
          {loading ? <Spinner size="sm" label="Entrando…" /> : "Entrar"}
        </button>
      </form>
    </main>
  );
}
