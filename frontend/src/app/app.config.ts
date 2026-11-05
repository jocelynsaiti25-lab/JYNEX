import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { credencialesInterceptor } from './core/interceptors/credenciales.interceptor';
import { sesionExpiradaInterceptor } from './core/interceptors/sesion-expirada.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([credencialesInterceptor, sesionExpiradaInterceptor])),
  ],
};
