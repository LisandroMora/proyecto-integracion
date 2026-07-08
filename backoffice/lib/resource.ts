import { api } from "./api";

export type EstadoRegistro = 0 | 1;

export type QueryParams = Record<string, string | number | boolean | undefined | null>;

export type ResourceClient<TRead, TCreate, TUpdate> = {
  list: (query?: QueryParams) => Promise<TRead[]>;
  get: (id: number) => Promise<TRead>;
  create: (dto: TCreate) => Promise<TRead>;
  update: (id: number, dto: TUpdate) => Promise<TRead>;
  remove: (id: number) => Promise<void>;
};

function buildQueryString(query?: QueryParams): string {
  if (!query) return "";
  const usp = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === null || value === "") continue;
    usp.set(key, String(value));
  }
  const s = usp.toString();
  return s ? `?${s}` : "";
}

export function createResourceClient<TRead, TCreate, TUpdate>(
  path: string
): ResourceClient<TRead, TCreate, TUpdate> {
  return {
    list: (query) => api<TRead[]>(`${path}${buildQueryString(query)}`),
    get: (id) => api<TRead>(`${path}/${id}`),
    create: (dto) => api<TRead>(path, { method: "POST", body: dto }),
    update: (id, dto) => api<TRead>(`${path}/${id}`, { method: "PUT", body: dto }),
    remove: (id) => api<void>(`${path}/${id}`, { method: "DELETE" }),
  };
}
