import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // El backoffice no tiene nada de servidor: todas las páginas son de cliente y
  // los datos se piden a la API por REST desde el navegador. Exportarlo como
  // estático permite servirlo desde Azure Static Web Apps en el plan gratuito.
  output: "export",

  // Genera una carpeta por ruta (out/login/index.html) en lugar de login.html,
  // que es lo que un host estático resuelve sin configuración extra.
  trailingSlash: true,
};

export default nextConfig;
