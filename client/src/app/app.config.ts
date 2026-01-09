import {ApplicationConfig, importProvidersFrom, provideBrowserGlobalErrorListeners} from '@angular/core';
import { provideRouter } from '@angular/router';
import * as BackendService from './api/openapi/backend';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    importProvidersFrom(
      BackendService.ApiModule.forRoot(() => new BackendService.Configuration({basePath: ''}))
    )
  ]
};
