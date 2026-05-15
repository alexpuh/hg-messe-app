import {ChangeDetectionStrategy, Component, computed, inject, OnInit, signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {Select, SelectChangeEvent} from 'primeng/select';
import {Button} from 'primeng/button';
import {RouterLink} from '@angular/router';
import {Dialog} from 'primeng/dialog';
import {ScanSessionStore} from '../../store';
import {GermanDateTimePipe} from '../../pipes/german-date-time.pipe';
import {ScanSessionsService} from '../../api/scan-sessions.service';
import {Ort, ScanSessionType} from '../../api/openapi/backend';

@Component({
  selector: 'app-scan-session',
  imports: [
    FormsModule,
    Select,
    Button,
    RouterLink,
    Dialog,
    GermanDateTimePipe
  ],
  templateUrl: './scan-session.component.html',
  styleUrl: './scan-session.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScanSession implements OnInit {
  protected readonly store = inject(ScanSessionStore);
  private readonly scanSessionsService = inject(ScanSessionsService);

  protected readonly scanMode = computed(() => {
    const session = this.store.selectedScanSession();
    if (!session) return 'Kein aktiver Scan';
    if (session.sessionType === ScanSessionType.ProcessDispatchList) return 'Beladung';
    if (session.sessionType === ScanSessionType.Inventory) {
      return session.ort === Ort.Lager ? 'Bestandsaufnahme Lager' : 'Bestandsaufnahme Stand';
    }
    return 'Kein aktiver Scan';
  });

  protected showBeladungDialog = signal(false);
  protected showBestandsaufnahmeDialog = signal(false);
  protected selectedDispatchSheetId = signal<number | null>(null);
  protected selectedBestandsOrt = signal<Ort>(Ort.Stand);
  protected selectedBestandsDispatchSheetId = signal<number | null>(null);

  protected readonly ortOptions = [
    { label: 'Stand', value: Ort.Stand },
    { label: 'Lager', value: Ort.Lager },
  ];

  protected items = computed(() => {
    const items = this.store.scanSessionArticles();
    return [...items].sort((a, b) => {
      const dateA = a.updatedAt ? new Date(a.updatedAt).getTime() : 0;
      const dateB = b.updatedAt ? new Date(b.updatedAt).getTime() : 0;
      return dateB - dateA; // Descending order (newest first)
    });
  });

  protected showSollColumn = computed(() => {
    const session = this.store.selectedScanSession();
    if (!session) return false;
    return session.ort === Ort.Lager;
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

  protected canStartBeladung = computed(() => {
    return this.selectedDispatchSheetId() !== null;
  });

  protected canStartBestandsaufnahme = computed(() => {
    if (this.selectedBestandsOrt() === Ort.Lager) {
      return this.selectedBestandsDispatchSheetId() !== null;
    }
    return true;
  });

  protected openBeladungDialog() {
    this.showBeladungDialog.set(true);
  }

  protected closeBeladungDialog() {
    this.showBeladungDialog.set(false);
    this.selectedDispatchSheetId.set(null);
  }

  protected openBestandsaufnahmeDialog() {
    this.selectedBestandsOrt.set(Ort.Stand);
    this.selectedBestandsDispatchSheetId.set(null);
    this.showBestandsaufnahmeDialog.set(true);
  }

  protected closeBestandsaufnahmeDialog() {
    this.showBestandsaufnahmeDialog.set(false);
  }

  protected onDispatchSheetChange(event: SelectChangeEvent) {
    this.selectedDispatchSheetId.set(event.value);
  }

  protected onBestandsOrtChange(event: SelectChangeEvent) {
    this.selectedBestandsOrt.set(event.value);
    this.selectedBestandsDispatchSheetId.set(null);
  }

  protected onBestandsDispatchSheetChange(event: SelectChangeEvent) {
    this.selectedBestandsDispatchSheetId.set(event.value);
  }

  protected startBeladung() {
    const selectedId = this.selectedDispatchSheetId();
    if (selectedId) {
      this.store.startNewScanSession({
        sessionType: ScanSessionType.ProcessDispatchList,
        ort: Ort.Lager,
        dispatchSheetId: selectedId,
      });
      this.closeBeladungDialog();
    }
  }

  protected startBestandsaufnahme() {
    const ort = this.selectedBestandsOrt();
    const dispatchSheetId = ort === Ort.Lager ? this.selectedBestandsDispatchSheetId() : null;
    this.store.startNewScanSession({
      sessionType: ScanSessionType.Inventory,
      ort,
      dispatchSheetId,
    });
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
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;

        const date = new Date().toISOString().split('T')[0];
        const dispatchSheetName = this.store.dispatchSheetName() || 'Bestand';
        link.download = `${dispatchSheetName}_${date}.xlsx`;
        document.body.appendChild(link);
        link.click();

        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
      },
      error: (error) => {
        console.error('Error exporting to Excel:', error);
      }
    });
  }

  ngOnInit(): void {
    console.log('ngOnInit');
    this.store.reloadScanSessionArticles();
  }

  protected readonly ScanSessionType = ScanSessionType;
  protected readonly Ort = Ort;
}

