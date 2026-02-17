import {ChangeDetectionStrategy, Component, computed, effect, inject, signal} from '@angular/core';
import { Button } from 'primeng/button';
import { RouterLink } from '@angular/router';
import { Select, SelectChangeEvent } from 'primeng/select';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { FormsModule } from '@angular/forms';
import { ScanSessionStore } from '../../store';
import { DispatchSheetsService } from '../../api/dispatch-sheets.service';
import {TableModule} from 'primeng/table';
import {DtoDispatchSheetArticleUnit} from '../../api/openapi/backend';

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
  private readonly dispatchSheetsService = inject(DispatchSheetsService);

  protected selectedDispatchSheetId = signal<number | null>(this.store.selectedScanSession()?.dispatchSheetId ?? null);
  protected showNewDispatchSheetDialog = signal(false);
  protected newDispatchSheetName = signal('');
  private articlesData = signal<DtoDispatchSheetArticleUnit[]>([]);

  // Editing state for required counts
  protected editingUnitId = signal<number | null>(null);
  protected editingValue = signal<string>('');

  constructor() {
    // Load articles when the dispatch sheet changes
    effect(() => {
      const dispatchSheetId = this.selectedDispatchSheetId();
      if (!dispatchSheetId) {
        this.articlesData.set([]);
        return;
      }

      this.dispatchSheetsService.getDispatchSheetArticleUnits(dispatchSheetId).subscribe({
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
    const selectedId = this.selectedDispatchSheetId();
    if (!selectedId) return 'Keine Messe ausgewählt';

    const dispatchSheet = this.store.dispatchSheets().find(te => te.id === selectedId);
    return dispatchSheet?.name ?? 'Unbekannt';
  });

  protected dispatchSheetOptions = computed(() => {
    return this.store.dispatchSheets().map(dispatchSheet => ({
      label: dispatchSheet.name || 'Unbekannt',
      value: dispatchSheet.id
    }));
  });

  // Sorted articles: first by required (not null first), then by name
  protected articles = computed(() => {
    const items = this.articlesData();
    return [...items].sort(this.compareArticles);
  });

  protected onDispatchSheetChange(event: SelectChangeEvent) {
    this.selectedDispatchSheetId.set(event.value);
  }

  protected openNewDispatchSheetDialog() {
    this.showNewDispatchSheetDialog.set(true);
    this.newDispatchSheetName.set('');
  }

  protected closeNewDispatchSheetDialog() {
    this.showNewDispatchSheetDialog.set(false);
    this.newDispatchSheetName.set('');
  }

  protected onNewDispatchSheetNameChange(event: Event) {
    const input = event.target as HTMLInputElement;
    this.newDispatchSheetName.set(input.value);
  }

  protected canCreateDispatchSheet = computed(() => {
    return this.newDispatchSheetName().trim().length > 0;
  });

  protected createNewDispatchSheet() {
    const name = this.newDispatchSheetName().trim();
    if (!name) return;

    this.dispatchSheetsService.addDispatchSheet({ name }).subscribe({
      next: (dispatchSheet) => {
        // Reload dispatch sheets from the store
        this.store.loadDispatchSheets();
        // Select the newly created dispatch sheet
        if (dispatchSheet.id) {
          this.selectedDispatchSheetId.set(dispatchSheet.id);
        }
        this.closeNewDispatchSheetDialog();
      },
      error: (error) => {
        console.error('Error creating dispatch sheet:', error);
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

    const dispatchSheetId = this.selectedDispatchSheetId();
    if (!dispatchSheetId) return;

    const value = parseInt(this.editingValue(), 10);
    if (isNaN(value) || value < 0) {
      console.error('Invalid value');
      return;
    }

    this.dispatchSheetsService.setRequiredUnits(dispatchSheetId, {
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

    const dispatchSheetId = this.selectedDispatchSheetId();
    if (!dispatchSheetId) return;

    this.dispatchSheetsService.deleteRequiredUnit(dispatchSheetId, unitId).subscribe({
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

  protected uploadArticles() {
  }
}
