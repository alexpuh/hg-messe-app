import { Component, computed, inject, signal } from '@angular/core';
import { Select, SelectChangeEvent } from 'primeng/select';
import { Button } from 'primeng/button';
import { RouterLink } from '@angular/router';
import { Dialog } from 'primeng/dialog';
import { ScanSessionStore } from '../../store';
import { InputText } from 'primeng/inputtext';
import { GermanDateTimePipe } from '../../pipes/german-date-time.pipe';
import { ScanSessionsService } from '../../api/scan-sessions.service';

// Special values for new options
const NEW_LOADING_LIST_VALUE = -1;
const NO_LOADING_LIST_VALUE = -2;

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
  protected readonly store = inject(ScanSessionStore);
  private readonly scanSessionsService = inject(ScanSessionsService);

  protected showNewInventoryDialog = signal(false);
  protected selectedLoadingListId = signal<number | null>(null);
  protected readonly newLoadingListName = signal<string>('');
  protected items = computed(() => {
    const items = this.store.scanSessionArticles();
    return [...items].sort((a, b) => {
      const dateA = a.updatedAt ? new Date(a.updatedAt).getTime() : 0;
      const dateB = b.updatedAt ? new Date(b.updatedAt).getTime() : 0;
      return dateB - dateA; // Descending order (newest first)
    });
  });

  protected startedAt = computed(() => {
    const scanSession = this.store.selectedScanSession();
    return scanSession?.startedAt ? new Date(scanSession.startedAt) : null;
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
      { label: 'Neue Messe erstellen', value: NEW_LOADING_LIST_VALUE },
      { label: 'Ohne Messezuordnung starten', value: NO_LOADING_LIST_VALUE },
      ...options
    ];
  });

  protected showNewTradeEventNameField = computed(() => {
    return this.selectedLoadingListId() === NEW_LOADING_LIST_VALUE;
  });

  protected canStartInventory = computed(() => {
    const selectedId = this.selectedLoadingListId();
    if (selectedId === null) {
      return false;
    }
    if (selectedId === NEW_LOADING_LIST_VALUE) {
      // Check if name is provided
      return this.newLoadingListName().trim().length > 0;
    }
    return true;
  });

  protected openNewInventoryDialog() {
    this.showNewInventoryDialog.set(true);
  }

  protected closeNewInventoryDialog() {
    this.showNewInventoryDialog.set(false);
    this.selectedLoadingListId.set(null);
    this.newLoadingListName.set('');
  }

  protected onTradeEventChange(event: SelectChangeEvent) {
    this.selectedLoadingListId.set(event.value);
    // Reset the name field when changing selection
    if (event.value !== NEW_LOADING_LIST_VALUE) {
      this.newLoadingListName.set('');
    }
  }

  protected onTradeEventNameChange(event: Event) {
    const input = event.target as HTMLInputElement;
    this.newLoadingListName.set(input.value);
  }

  protected startNewInventory() {
    const selectedId = this.selectedLoadingListId();

    if (selectedId === NEW_LOADING_LIST_VALUE) {
      // Create new trade event first, then create inventory
      const name = this.newLoadingListName().trim();
      if (name) {
        this.createTradeEventAndStartInventory(name);
      }
    } else if (selectedId === NO_LOADING_LIST_VALUE) {
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
    const scanSession = this.store.selectedScanSession();
    if (!scanSession?.id) {
      console.error('No scanSession selected');
      return;
    }

    this.scanSessionsService.getScanSessionArticlesExcel(scanSession.id).subscribe({
      next: (blob) => {
        // Create a download link
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;

        // Generate filename with trade event name and date
        const date = new Date().toISOString().split('T')[0];
        const tradeEventName = this.store.tradeEventName() || 'Bestand';
        const filename = `${tradeEventName}_${date}.xlsx`;

        link.download = filename;
        document.body.appendChild(link);
        link.click();

        // Cleanup
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
      },
      error: (error) => {
        console.error('Error exporting to Excel:', error);
      }
    });
  }
}
