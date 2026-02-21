import {Component, computed, inject, signal} from '@angular/core';
import {Select, SelectChangeEvent} from 'primeng/select';
import {Button} from 'primeng/button';
import {RouterLink} from '@angular/router';
import {Dialog} from 'primeng/dialog';
import {ScanSessionStore} from '../../store';
import {GermanDateTimePipe} from '../../pipes/german-date-time.pipe';
import {ScanSessionsService} from '../../api/scan-sessions.service';

type ScanMode = 'Beladung' | 'Bestandsaufnahme';

@Component({
  selector: 'app-scan-session',
  imports: [
    Select,
    Button,
    RouterLink,
    Dialog,
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
    return this.store.dispatchSheets().map(event => ({
      label: event.name || 'Unbekannt',
      value: event.id
    }));
  });

  protected canStartScanSession = computed(() => {
    return this.selectedDispatchSheetId() !== null;
  });

  protected openBeladungDialog() {
    this.showBeladungDialog.set(true);
  }

  protected closeBeladungDialog() {
    this.showBeladungDialog.set(false);
    this.selectedDispatchSheetId.set(null);
  }

  protected openBestandsaufnahmeDialog() {
    this.showBestandsaufnahmeDialog.set(true);
  }

  protected closeBestandsaufnahmeDialog() {
    this.showBestandsaufnahmeDialog.set(false);
  }

  protected onDispatchSheetChange(event: SelectChangeEvent) {
    this.selectedDispatchSheetId.set(event.value);
  }

  protected startBeladung() {
    this.scanMode = 'Beladung';
    const selectedId = this.selectedDispatchSheetId();

    if (selectedId) {
      console.log('Start scan session with dispatch sheet', selectedId);
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
