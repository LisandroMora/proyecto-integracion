"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";

export type ToastKind = "success" | "error" | "info";

type Toast = {
  id: number;
  kind: ToastKind;
  message: string;
};

type ToastApi = {
  success: (message: string) => void;
  error: (message: string) => void;
  info: (message: string) => void;
  remove: (id: number) => void;
};

const ToastContext = createContext<ToastApi | null>(null);

const DEFAULT_MS: Record<ToastKind, number> = {
  success: 3500,
  error: 6000,
  info: 4000,
};

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const nextId = useRef(1);

  const remove = useCallback((id: number) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const push = useCallback(
    (kind: ToastKind, message: string) => {
      const id = nextId.current++;
      setToasts((prev) => [...prev, { id, kind, message }]);
      window.setTimeout(() => remove(id), DEFAULT_MS[kind]);
    },
    [remove]
  );

  const api = useMemo<ToastApi>(
    () => ({
      success: (m) => push("success", m),
      error: (m) => push("error", m),
      info: (m) => push("info", m),
      remove,
    }),
    [push, remove]
  );

  return (
    <ToastContext.Provider value={api}>
      {children}
      <ToastViewport toasts={toasts} onDismiss={remove} />
    </ToastContext.Provider>
  );
}

export function useToast(): ToastApi {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error("useToast debe usarse dentro de <ToastProvider>.");
  return ctx;
}

function ToastViewport({
  toasts,
  onDismiss,
}: {
  toasts: Toast[];
  onDismiss: (id: number) => void;
}) {
  if (toasts.length === 0) return null;
  return (
    <div className="pointer-events-none fixed bottom-4 right-4 z-50 flex w-full max-w-sm flex-col gap-2">
      {toasts.map((t) => (
        <ToastItem key={t.id} toast={t} onDismiss={() => onDismiss(t.id)} />
      ))}
    </div>
  );
}

const STYLES: Record<ToastKind, { border: string; bg: string; text: string; icon: string }> = {
  success: {
    border: "border-emerald-900",
    bg: "bg-emerald-950/70",
    text: "text-emerald-200",
    icon: "text-emerald-300",
  },
  error: {
    border: "border-rose-900",
    bg: "bg-rose-950/70",
    text: "text-rose-200",
    icon: "text-rose-300",
  },
  info: {
    border: "border-slate-700",
    bg: "bg-slate-900",
    text: "text-slate-100",
    icon: "text-slate-400",
  },
};

const ICONS: Record<ToastKind, string> = {
  success: "✓",
  error: "!",
  info: "i",
};

function ToastItem({ toast, onDismiss }: { toast: Toast; onDismiss: () => void }) {
  const [entered, setEntered] = useState(false);
  useEffect(() => {
    const t = window.setTimeout(() => setEntered(true), 10);
    return () => window.clearTimeout(t);
  }, []);
  const s = STYLES[toast.kind];
  return (
    <div
      role="status"
      className={
        "pointer-events-auto flex items-start gap-3 rounded-md border px-3 py-2 shadow-lg shadow-slate-950/40 backdrop-blur-sm transition-all " +
        s.border +
        " " +
        s.bg +
        " " +
        s.text +
        " " +
        (entered ? "translate-y-0 opacity-100" : "translate-y-2 opacity-0")
      }
    >
      <span
        className={
          "mt-0.5 inline-flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-slate-950/60 text-xs font-bold ring-1 ring-inset ring-current " +
          s.icon
        }
        aria-hidden
      >
        {ICONS[toast.kind]}
      </span>
      <div className="flex-1 text-sm leading-5">{toast.message}</div>
      <button
        type="button"
        onClick={onDismiss}
        className="shrink-0 text-slate-500 hover:text-slate-200"
        aria-label="Cerrar"
      >
        ×
      </button>
    </div>
  );
}
