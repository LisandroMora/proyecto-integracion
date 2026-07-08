// Formatea una cédula dominicana normalizada (11 dígitos) como 000-0000000-0.
// Si el valor no tiene 11 dígitos, lo devuelve tal cual (evita romper legacy).
export function formatCedula(cedula: string): string {
  const digits = cedula.replace(/\D/g, "");
  if (digits.length !== 11) return cedula;
  return `${digits.slice(0, 3)}-${digits.slice(3, 10)}-${digits.slice(10)}`;
}

// Máscara progresiva para el input de cédula: descarta no-dígitos, corta a 11
// dígitos e inserta guiones a medida que se escribe (000-0000000-0).
export function maskCedulaInput(value: string): string {
  const digits = value.replace(/\D/g, "").slice(0, 11);
  if (digits.length <= 3) return digits;
  if (digits.length <= 10) return `${digits.slice(0, 3)}-${digits.slice(3)}`;
  return `${digits.slice(0, 3)}-${digits.slice(3, 10)}-${digits.slice(10)}`;
}
