import { useSyncExternalStore } from "react";

export type Session = {
  token: string;
  expiresAt: string;
  email: string;
  rol: string;
};

const KEY = "nomina.session";
const listeners = new Set<() => void>();

// Snapshot cache — imprescindible para useSyncExternalStore: si devolvemos un objeto
// nuevo en cada llamada, React entra en re-render infinito. Solo re-parseamos cuando
// el raw de localStorage cambia.
let cachedRaw: string | null | undefined = undefined;
let cachedSession: Session | null = null;

function parseSession(raw: string | null): Session | null {
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as Session;
    if (Date.now() >= new Date(parsed.expiresAt).getTime()) return null;
    return parsed;
  } catch {
    return null;
  }
}

function readSnapshot(): Session | null {
  if (typeof window === "undefined") return null;
  const raw = window.localStorage.getItem(KEY);
  if (cachedRaw === raw) return cachedSession;
  cachedRaw = raw;
  cachedSession = parseSession(raw);
  return cachedSession;
}

function notify(): void {
  cachedRaw = undefined;
  for (const l of listeners) l();
}

export function saveSession(s: Session): void {
  if (typeof window === "undefined") return;
  window.localStorage.setItem(KEY, JSON.stringify(s));
  notify();
}

export function clearSession(): void {
  if (typeof window === "undefined") return;
  window.localStorage.removeItem(KEY);
  notify();
}

export function getSession(): Session | null {
  return readSnapshot();
}

function subscribe(cb: () => void): () => void {
  listeners.add(cb);
  const onStorage = (e: StorageEvent) => {
    if (e.key === KEY || e.key === null) {
      cachedRaw = undefined;
      cb();
    }
  };
  window.addEventListener("storage", onStorage);
  return () => {
    listeners.delete(cb);
    window.removeEventListener("storage", onStorage);
  };
}

const nullServerSnapshot = (): Session | null => null;

export function useSession(): Session | null {
  return useSyncExternalStore(subscribe, readSnapshot, nullServerSnapshot);
}

// "¿Ya hidrató el cliente?" con la misma primitiva: en SSR devuelve false, en cliente true.
const noopUnsubscribe = () => {};
const hydrationSubscribe = (): (() => void) => noopUnsubscribe;
const trueSnapshot = () => true;
const falseSnapshot = () => false;

export function useHydrated(): boolean {
  return useSyncExternalStore(hydrationSubscribe, trueSnapshot, falseSnapshot);
}
