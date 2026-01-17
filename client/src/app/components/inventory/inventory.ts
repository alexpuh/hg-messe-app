import { Component, computed, inject } from '@angular/core';
import { Select, SelectChangeEvent } from 'primeng/select';
import { Button } from 'primeng/button';
import { RouterLink } from '@angular/router';
import { GermanDatePipe } from '../../pipes/german-date.pipe';
import { InventoryStore } from '../../store/inventory.store';


@Component({
  selector: 'app-inventory',
  imports: [
    Select,
    Button,
    RouterLink,
    GermanDatePipe
  ],
  templateUrl: './inventory.html',
  styleUrl: './inventory.scss',
})
export class Inventory {
  protected readonly store = inject(InventoryStore);

  protected messeList: string[] = ['Frankfurt', 'Berlin'];

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

  protected onMesseChange(_event: SelectChangeEvent) {

  }

  protected doTest() {
    console.log('Test button clicked');
  }
}
