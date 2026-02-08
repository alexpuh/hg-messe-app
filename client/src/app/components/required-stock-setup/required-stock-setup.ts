import {ChangeDetectionStrategy, Component, computed, effect, inject, signal} from '@angular/core';
import { Button } from 'primeng/button';
import { RouterLink } from '@angular/router';
import { Select, SelectChangeEvent } from 'primeng/select';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { FormsModule } from '@angular/forms';
import { ScanSessionStore } from '../../store';
import { LoadingListsService } from '../../api/loading-lists.service';
import {TableModule} from 'primeng/table';
import {DtoLoadingListArticleUnit} from '../../api/openapi/backend';

@Component({
  selector: 'app-required-stock-setup',
  imports: [
    Button,
    RouterLink,
    Select,
    Dialog,
    InputText,
    FormsModule,
    TableModule
  ],
  templateUrl: './required-stock-setup.html',
  styleUrl: './required-stock-setup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'h-full'
  }
})
export class RequiredStockSetup {
  private readonly store = inject(ScanSessionStore);
  private readonly tradeEventsService = inject(LoadingListsService);

  protected selectedTradeEventId = signal<number | null>(this.store.selectedScanSession()?.loadingListId ?? null);
  protected showNewTradeEventDialog = signal(false);
  protected newTradeEventName = signal('');
  private articlesData = signal<DtoLoadingListArticleUnit[]>([]);

  // Editing state for required counts
  protected editingUnitId = signal<number | null>(null);
  protected editingValue = signal<string>('');

  constructor() {
    // Load articles when loadinglist changes
    effect(() => {
      const tradeEventId = this.selectedTradeEventId();
      if (!tradeEventId) {
        this.articlesData.set([]);
        return;
      }

      this.tradeEventsService.getLoadingListArticleUnits(tradeEventId).subscribe({
        next: (articles) => {
          this.articlesData.set(articles);
        },
        error: (error) => {
          console.error('Error loading articles:', error);
          this.articlesData.set([]);
        }
      });
    });
  }

  protected messeName = computed(() => {
    const selectedId = this.selectedTradeEventId();
    if (!selectedId) return 'Keine Messe ausgewählt';

    const tradeEvent = this.store.tradeEvents().find(te => te.id === selectedId);
    return tradeEvent?.name ?? 'Unbekannt';
  });

  protected tradeEventOptions = computed(() => {
    return this.store.tradeEvents().map(event => ({
      label: event.name || 'Unbekannt',
      value: event.id
    }));
  });

  // Sorted articles: first by required (not null first), then by name
  protected articles = computed(() => {
    const items = this.articlesData();
    return [...items].sort(this.compareArticles);
  });

  protected onTradeEventChange(event: SelectChangeEvent) {
    this.selectedTradeEventId.set(event.value);
  }

  protected openNewTradeEventDialog() {
    this.showNewTradeEventDialog.set(true);
    this.newTradeEventName.set('');
  }

  protected closeNewTradeEventDialog() {
    this.showNewTradeEventDialog.set(false);
    this.newTradeEventName.set('');
  }

  protected onNewTradeEventNameChange(event: Event) {
    const input = event.target as HTMLInputElement;
    this.newTradeEventName.set(input.value);
  }

  protected canCreateTradeEvent = computed(() => {
    return this.newTradeEventName().trim().length > 0;
  });

  protected createNewTradeEvent() {
    const name = this.newTradeEventName().trim();
    if (!name) return;

    this.tradeEventsService.addLoadingList({ name }).subscribe({
      next: (tradeEvent) => {
        // Reload trade events from the store
        this.store.readLoadingLists();
        // Select the newly created trade event
        if (tradeEvent.id) {
          this.selectedTradeEventId.set(tradeEvent.id);
        }
        this.closeNewTradeEventDialog();
      },
      error: (error) => {
        console.error('Error creating trade event:', error);
      }
    });
  }

  protected startEditing(unitId: number | undefined, currentValue: number | null | undefined) {
    if (!unitId) return;
    this.editingUnitId.set(unitId);
    this.editingValue.set(currentValue?.toString() ?? '');
  }

  protected cancelEditing() {
    this.editingUnitId.set(null);
    this.editingValue.set('');
  }

  protected saveRequiredCount(unitId: number | undefined) {
    if (!unitId) return;

    const tradeEventId = this.selectedTradeEventId();
    if (!tradeEventId) return;

    const value = parseInt(this.editingValue(), 10);
    if (isNaN(value) || value < 0) {
      console.error('Invalid value');
      return;
    }

    this.tradeEventsService.setRequiredUnits(tradeEventId, {
      unitId,
      count: value
    }).subscribe({
      next: () => {
        // Update local data
        const articles = this.articlesData();
        const updatedArticles = articles.map(a =>
          a.unitId === unitId ? { ...a, requiredCount: value } : a
        );
        this.articlesData.set(updatedArticles);
        this.cancelEditing();
      },
      error: (error) => {
        console.error('Error saving required count:', error);
      }
    });
  }

  protected deleteRequiredCount(unitId: number | undefined) {
    if (!unitId) return;

    const tradeEventId = this.selectedTradeEventId();
    if (!tradeEventId) return;

    this.tradeEventsService.deleteRequiredUnit(tradeEventId, unitId).subscribe({
      next: () => {
        // Update local data
        const articles = this.articlesData();
        const updatedArticles = articles.map(a =>
          a.unitId === unitId ? { ...a, requiredCount: null } : a
        );
        this.articlesData.set(updatedArticles);
        this.cancelEditing();
      },
      error: (error) => {
        console.error('Error deleting required count:', error);
      }
    });
  }

  protected isEditing(unitId: number | undefined): boolean {
    return this.editingUnitId() === unitId;
  }

  private compareArticles(
    a: { requiredCount?: number | null; articleDisplayName?: string | null },
    b: { requiredCount?: number | null; articleDisplayName?: string | null }
  ): number {
    // Sort by required: not null first
    const aHasRequired = a.requiredCount !== null && a.requiredCount !== undefined;
    const bHasRequired = b.requiredCount !== null && b.requiredCount !== undefined;

    if (aHasRequired && !bHasRequired) return -1;
    if (!aHasRequired && bHasRequired) return 1;

    // Then sort by name
    const aName = a.articleDisplayName ?? '';
    const bName = b.articleDisplayName ?? '';
    return aName.localeCompare(bName);
  }
}
