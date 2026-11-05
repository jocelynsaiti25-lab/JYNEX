// Configuración de DESARROLLO (ng serve).
// '/api' se redirige a http://localhost:5080 gracias a proxy.conf.json,
// así Angular y la API parecen el mismo sitio y las cookies de sesión (HU-01) funcionan sin CORS.
export const environment = {
  production: false,
  apiUrl: '/api',
};
