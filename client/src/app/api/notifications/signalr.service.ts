import * as signalR from '@microsoft/signalr';
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SignalrService {
  private hubConnection!: signalR.HubConnection;

  startConnection() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('hubs/notification')
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start()
      .then(() => console.log('SignalR connected'))
      .catch(err => console.error(err));
  }

  onBarcodeScanned(callback: (msg: string) => void) {
    this.hubConnection.on('BarcodeScanned', callback);
  }

  onBarcodeError(callback: (ean: string, errorMessage: string) => void) {
    this.hubConnection.on('BarcodeError', callback);
  }

  onScannerStatusChanged(callback: (msg: string) => void) {
    this.hubConnection.on('ScannerStatusChanged', callback);
  }
}
