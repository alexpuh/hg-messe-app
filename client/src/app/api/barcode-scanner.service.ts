import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { BarcodeScannerOpenApi } from './openapi/backend';

export interface BarcodeScannerStatus {
  isConnected: boolean;
  deviceName?: string;
  lastScan?: string;
  status?: string;
}

@Injectable({
  providedIn: 'root'
})
export class BarcodeScannerService {
  private readonly api = inject(BarcodeScannerOpenApi);

  getStatus(): Observable<BarcodeScannerStatus> {
    return this.api.apiBarcodeScannerStatusGet();
  }
}

