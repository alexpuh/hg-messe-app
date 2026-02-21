import {Component, computed, inject, signal} from '@angular/core';
import {Select, SelectChangeEvent} from 'primeng/select';
import {Button} from 'primeng/button';
import {RouterLink} from '@angular/router';
import {Dialog} from 'primeng/dialog';
import {ScanSessionStore} from '../../store';
import {InputText} from 'primeng/inputtext';
import {GermanDateTimePipe} from '../../pipes/german-date-time.pipe';
import {ScanSessionsService} from '../../api/scan-sessions.service';

// Special values for new options
const NEW_DISPATCH_SHEET_VALUE = -1;
const NO_DISPATCH_SHEET_VALUE = -2;
type ScanMode = 'Beladung' | 'Bestandsaufnahme';

@Component({
  selector: 'app-scan-session',
  imports: [
    Select,
    Button,
    RouterLink,
    Dialog,
    InputText,
    GermanDateTimePipe
  ],
  templateUrl: './scan-session.component.html',
  styleUrl: './scan-session.component.scss',
})
export class ScanSession {
  protected scanMode: ScanMode = 'Beladung';
  protected readonly store = inject(ScanSessionStore);
  private readonly scanSessionsService = inject(ScanSessionsService);

  protected showBeladungDialog = signal(false);
  protected showBestandsaufnahmeDialog = signal(false);
  protected selectedDispatchSheetId = signal<number | null>(null);
  protected readonly newDispatchSheetName = signal<string>('');
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

  protected dispatchSheetOptions = computed(() => {
    const options = this.store.dispatchSheets().map(event => ({
      label: event.name || 'Unbekannt',
      value: event.id
    }));

    // Add option to create new dispatch sheet
    return [
      { label: 'Neue Messe erstellen', value: NEW_DISPATCH_SHEET_VALUE },
      ...options
    ];
  });

  protected showNewDispatchSheetNameField = computed(() => {
    return this.selectedDispatchSheetId() === NEW_DISPATCH_SHEET_VALUE;
  });

  protected canStartScanSession = computed(() => {
    const selectedId = this.selectedDispatchSheetId();
    if (selectedId === null) {
      return false;
    }
    if (selectedId === NEW_DISPATCH_SHEET_VALUE) {
      // Check if a name is provided
      return this.newDispatchSheetName().trim().length > 0;
    }
    return true;
  });

  protected openBeladungDialog() {
    this.showBeladungDialog.set(true);
  }

  protected closeBeladungDialog() {
    this.showBeladungDialog.set(false);
    this.selectedDispatchSheetId.set(null);
    this.newDispatchSheetName.set('');
  }

  protected openBestandsaufnahmeDialog() {
    this.showBestandsaufnahmeDialog.set(true);
  }

  protected closeBestandsaufnahmeDialog() {
    this.showBestandsaufnahmeDialog.set(false);
  }

  protected onDispatchSheetChange(event: SelectChangeEvent) {
    this.selectedDispatchSheetId.set(event.value);
    // Reset the name field when changing selection
    if (event.value !== NEW_DISPATCH_SHEET_VALUE) {
      this.newDispatchSheetName.set('');
    }
  }

  protected onDispatchSheetNameChange(event: Event) {
    const input = event.target as HTMLInputElement;
    this.newDispatchSheetName.set(input.value);
  }

  protected startBeladung() {
    this.scanMode = 'Beladung';
    const selectedId = this.selectedDispatchSheetId();

    if (selectedId === NEW_DISPATCH_SHEET_VALUE) {
      // Create a new dispatch sheet first, then create a scan session
      const name = this.newDispatchSheetName().trim();
      if (name) {
        this.createDispatchSheetAndStartScanSession(name);
      }
    } else if (selectedId) {
      console.log('Start scan session with dispatch sheet', selectedId);
      // Start loading with an existing dispatch sheet
      this.store.startNewScanSession(selectedId);
      this.closeBeladungDialog();
    }
  }

  protected startBestandsaufnahme() {
    this.scanMode = 'Bestandsaufnahme';
    // Start inventory without a dispatch sheet
    this.store.startNewScanSession(undefined);
    this.closeBestandsaufnahmeDialog();
  }

  private createDispatchSheetAndStartScanSession(name: string) {
    // Use the store method that will handle both creation and inventory start
    this.store.createDispatchSheetAndStartScanSession(name);
    this.closeBeladungDialog();
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

        // Generate filename with the dispatch sheet name and date
        const date = new Date().toISOString().split('T')[0];
        const dispatchSheetName = this.store.dispatchSheetName() || 'Bestand';
        link.download = `${dispatchSheetName}_${date}.xlsx`;
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
