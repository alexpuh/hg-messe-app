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

  onStockChanged(callback: (msg: string) => void) {
    this.hubConnection.on('StockChanged', callback);
  }

  onOrderCreated(callback: (msg: string) => void) {
    this.hubConnection.on('OrderCreated', callback);
  }
}
