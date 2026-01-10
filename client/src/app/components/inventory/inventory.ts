import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { Select, SelectChangeEvent } from 'primeng/select';
import { Button } from 'primeng/button';
import { RouterLink } from '@angular/router';
import { GermanDatePipe } from '../../pipes/german-date.pipe';
import { EventInventoriesService } from '../../api/event-inventories.service';
import { DtoEventInventory, DtoStockItem } from '../../api/openapi/backend';
import {SignalrService} from '../../api/notifications/signalr.service';

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
export class Inventory implements OnInit {
  private readonly eventInventoriesService = inject(EventInventoriesService);
  private readonly signalr = inject(SignalrService);

  protected messeList: string[] = ['Frankfurt', 'Berlin'];
  protected currentInventory = signal<DtoEventInventory | null>(null);
  protected stockItems = signal<DtoStockItem[]>([]);

  protected startedAt = computed(() => {
    const inventory = this.currentInventory();
    return inventory?.startedAt ? new Date(inventory.startedAt) : null;
  });

  ngOnInit(): void {
    this.loadCurrentInventory();

    this.signalr.startConnection();

    this.signalr.onMessage(msg => {
      console.log('Message:', msg);
    });
  }

  private loadCurrentInventory(): void {
    this.eventInventoriesService.getCurrentInventory().subscribe({
      next: (inventory) => {
        this.currentInventory.set(inventory);
        if (inventory.id) {
          this.loadStockItems(inventory.id);
        }
      },
      error: (error) => {
        console.error('Error loading current inventory:', error);
      }
    });
  }

  private loadStockItems(inventoryId: number): void {
    this.eventInventoriesService.getStockFromCurrentInventory(inventoryId).subscribe({
      next: (items) => {
        this.stockItems.set(items);
      },
      error: (error) => {
        console.error('Error loading stock items:', error);
      }
    });
  }

  protected onMesseChange(_event: SelectChangeEvent) {

  }

  protected doTest() {
    this.eventInventoriesService.test().subscribe(_ => {
      console.log('Test successful');
    });
  }
}
