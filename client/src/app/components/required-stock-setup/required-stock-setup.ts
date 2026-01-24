import {ChangeDetectionStrategy, Component, computed, effect, inject, signal} from '@angular/core';
import { Button } from 'primeng/button';
import { RouterLink } from '@angular/router';
import { Select, SelectChangeEvent } from 'primeng/select';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { FormsModule } from '@angular/forms';
import { InventoryStore } from '../../store';
import { TradeEventsService } from '../../api/trade-events.service';
import { DtoTradeEventArticleUnit } from '../../api/openapi/backend';
import {TableModule} from 'primeng/table';
import {NgIf} from '@angular/common';

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
  private readonly store = inject(InventoryStore);
  private readonly tradeEventsService = inject(TradeEventsService);

  protected selectedTradeEventId = signal<number | null>(this.store.selectedInventory()?.tradeEventId ?? null);
  protected showNewTradeEventDialog = signal(false);
  protected newTradeEventName = signal('');
  private articlesData = signal<DtoTradeEventArticleUnit[]>([]);

  constructor() {
    // Load articles when trade event changes
    effect(() => {
      const tradeEventId = this.selectedTradeEventId();
      if (!tradeEventId) {
        this.articlesData.set([]);
        return;
      }

      this.tradeEventsService.getTradeEventArticleUnits(tradeEventId).subscribe({
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

    this.tradeEventsService.addTradeEvent({ name }).subscribe({
      next: (tradeEvent) => {
        // Reload trade events from store
        this.store.loadTradeEvents();
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
