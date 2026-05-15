import {ChangeDetectionStrategy, Component, computed, inject, OnInit, signal} from '@angular/core';
import {formatDate} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {Select, SelectChangeEvent} from 'primeng/select';
import {Button} from 'primeng/button';
import {RouterLink} from '@angular/router';
import {ScanSessionsService} from '../../api/scan-sessions.service';
import {DtoCombinedArticle, DtoScanSession, Ort, ScanSessionType} from '../../api/openapi/backend';

@Component({
  selector: 'app-combined-view',
  imports: [
    FormsModule,
    Select,
    Button,
    RouterLink,
  ],
  templateUrl: './combined-view.component.html',
  styleUrl: './combined-view.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CombinedView implements OnInit {
  private readonly scanSessionsService = inject(ScanSessionsService);

  protected allSessions = signal<DtoScanSession[]>([]);
  protected combinedArticles = signal<DtoCombinedArticle[]>([]);
  protected isLoading = signal(false);
  protected error = signal<string | null>(null);

  protected selectedStandSessionId = signal<number | null>(null);
  protected selectedLagerSessionId = signal<number | null>(null);

  protected standSessionOptions = computed(() =>
    this.allSessions()
      .filter(s => s.ort === Ort.Stand)
      .map(s => ({ label: this.sessionLabel(s), value: s.id! }))
  );

  protected lagerSessionOptions = computed(() =>
    this.allSessions()
      .filter(s => s.ort === Ort.Lager)
      .map(s => ({ label: this.sessionLabel(s), value: s.id! }))
  );

  protected canLoad = computed(() =>
    this.selectedStandSessionId() !== null && this.selectedLagerSessionId() !== null
  );

  ngOnInit(): void {
    this.loadSessions();
  }

  private loadSessions(): void {
    this.scanSessionsService.getAllScanSessions().subscribe({
      next: (sessions) => {
        this.allSessions.set(sessions);
        const standSessions = sessions.filter(s => s.ort === Ort.Stand);
        const lagerSessions = sessions.filter(s => s.ort === Ort.Lager);
        if (standSessions.length > 0) {
          this.selectedStandSessionId.set(standSessions[0].id!);
        }
        if (lagerSessions.length > 0) {
          this.selectedLagerSessionId.set(lagerSessions[0].id!);
        }
      },
      error: () => {
        this.error.set('Fehler beim Laden der Scan-Sitzungen');
      },
    });
  }

  protected onStandSessionChange(event: SelectChangeEvent): void {
    this.selectedStandSessionId.set(event.value);
    this.combinedArticles.set([]);
  }

  protected onLagerSessionChange(event: SelectChangeEvent): void {
    this.selectedLagerSessionId.set(event.value);
    this.combinedArticles.set([]);
  }

  protected loadCombined(): void {
    const standId = this.selectedStandSessionId();
    const lagerId = this.selectedLagerSessionId();
    if (standId === null || lagerId === null) return;

    this.isLoading.set(true);
    this.error.set(null);
    this.scanSessionsService.getCombinedArticles(standId, lagerId).subscribe({
      next: (articles) => {
        this.combinedArticles.set(articles);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Fehler beim Laden der kombinierten Übersicht');
        this.isLoading.set(false);
      },
    });
  }

  protected exportToExcel(): void {
    const standId = this.selectedStandSessionId();
    const lagerId = this.selectedLagerSessionId();
    if (standId === null || lagerId === null) return;

    this.scanSessionsService.getCombinedArticlesExcel(standId, lagerId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        const date = new Date().toISOString().split('T')[0];
        link.download = `Messeabschluss_${date}.xlsx`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        this.error.set('Fehler beim Exportieren der kombinierten Übersicht');
      },
    });
  }

  private sessionLabel(s: DtoScanSession): string {
    const date = s.startedAt ? formatDate(s.startedAt, 'dd.MM.yyyy HH:mm', 'de') : '';
    const type = s.sessionType === ScanSessionType.Inventory ? 'Bestandsaufnahme' : 'Beladung';
    return `${type} – ${date}`;
  }
}
