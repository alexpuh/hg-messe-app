import { Component, computed, inject, signal } from '@angular/core';
import { Select, SelectChangeEvent } from 'primeng/select';
import { Button } from 'primeng/button';
import { RouterLink } from '@angular/router';
import { Dialog } from 'primeng/dialog';
import { InventoryStore } from '../../store';
import { InputText } from 'primeng/inputtext';
import {GermanDateTimePipe} from '../../pipes/german-date-time.pipe';

// Special values for new options
const NEW_TRADE_EVENT_VALUE = -1;
const NO_TRADE_EVENT_VALUE = -2;

@Component({
  selector: 'app-inventory',
  imports: [
    Select,
    Button,
    RouterLink,
    Dialog,
    InputText,
    GermanDateTimePipe
  ],
  templateUrl: './inventory.html',
  styleUrl: './inventory.scss',
})
export class Inventory {
  protected readonly store = inject(InventoryStore);

  protected showNewInventoryDialog = signal(false);
  protected selectedTradeEventId = signal<number | null>(null);
  protected readonly newTradeEventName = signal<string>('');
  protected items = computed(() => {
    const items = this.store.stockItems();
    return [...items].sort((a, b) => {
      const dateA = a.updatedAt ? new Date(a.updatedAt).getTime() : 0;
      const dateB = b.updatedAt ? new Date(b.updatedAt).getTime() : 0;
      return dateB - dateA; // Descending order (newest first)
    });
  });

  protected startedAt = computed(() => {
    const inventory = this.store.selectedInventory();
    return inventory?.startedAt ? new Date(inventory.startedAt) : null;
  });

  protected barcodeScannerStatusText = computed(() => {
    const status = this.store.barcodeScannerStatus();
    if (!status) {
      return 'Scanner-Status: Unbekannt';
    }
    if (status.isConnected) {
      return `Scanner: Verbunden${status.deviceName ? ` (${status.deviceName})` : ''}`;
    }
    return 'Scanner: Nicht verbunden';
  });

  protected barcodeScannerStatusSeverity = computed(() => {
    return this.store.isScannerConnected() ? 'success' : 'danger';
  });

  protected tradeEventOptions = computed(() => {
    const options = this.store.tradeEvents().map(event => ({
      label: event.name || 'Unbekannt',
      value: event.id
    }));

    // Add special options at the beginning
    return [
      { label: 'Neue Messe erstellen', value: NEW_TRADE_EVENT_VALUE },
      { label: 'Ohne Messezuordnung starten', value: NO_TRADE_EVENT_VALUE },
      ...options
    ];
  });

  protected showNewTradeEventNameField = computed(() => {
    return this.selectedTradeEventId() === NEW_TRADE_EVENT_VALUE;
  });

  protected canStartInventory = computed(() => {
    const selectedId = this.selectedTradeEventId();
    if (selectedId === null) {
      return false;
    }
    if (selectedId === NEW_TRADE_EVENT_VALUE) {
      // Check if name is provided
      return this.newTradeEventName().trim().length > 0;
    }
    return true;
  });

  protected openNewInventoryDialog() {
    this.showNewInventoryDialog.set(true);
  }

  protected closeNewInventoryDialog() {
    this.showNewInventoryDialog.set(false);
    this.selectedTradeEventId.set(null);
    this.newTradeEventName.set('');
  }

  protected onTradeEventChange(event: SelectChangeEvent) {
    this.selectedTradeEventId.set(event.value);
    // Reset the name field when changing selection
    if (event.value !== NEW_TRADE_EVENT_VALUE) {
      this.newTradeEventName.set('');
    }
  }

  protected onTradeEventNameChange(event: Event) {
    const input = event.target as HTMLInputElement;
    this.newTradeEventName.set(input.value);
  }

  protected startNewInventory() {
    const selectedId = this.selectedTradeEventId();

    if (selectedId === NEW_TRADE_EVENT_VALUE) {
      // Create new trade event first, then create inventory
      const name = this.newTradeEventName().trim();
      if (name) {
        this.createTradeEventAndStartInventory(name);
      }
    } else if (selectedId === NO_TRADE_EVENT_VALUE) {
      // Start inventory without trade event
      this.store.startNewInventory(undefined);
      this.closeNewInventoryDialog();
    } else if (selectedId) {
      // Start inventory with existing trade event
      this.store.startNewInventory(selectedId);
      this.closeNewInventoryDialog();
    }
  }

  private createTradeEventAndStartInventory(name: string) {
    // Use the store method that will handle both creation and inventory start
    this.store.createTradeEventAndStartInventory(name);
    this.closeNewInventoryDialog();
  }

  protected doTest() {
    console.log('Test button clicked');
  }

  protected exportToExcel() {

  }
}
