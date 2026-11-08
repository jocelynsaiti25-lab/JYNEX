import { ApplicationConfig, LOCALE_ID, provideZoneChangeDetection } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import localeEsMx from '@angular/common/locales/es-MX';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { credencialesInterceptor } from './core/interceptors/credenciales.interceptor';
import { sesionExpiradaInterceptor } from './core/interceptors/sesion-expirada.interceptor';

// Fechas, números y moneda en español de México (ej. "jueves 5 de noviembre de 2026")
registerLocaleData(localeEsMx);

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([credencialesInterceptor, sesionExpiradaInterceptor])),
    { provide: LOCALE_ID, useValue: 'es-MX' },
  ],
};
