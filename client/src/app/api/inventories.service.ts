import { Injectable, inject } from '@angular/core';
import {Observable, of} from 'rxjs';
import {DtoEventInventory, InventoriesOpenApi} from './openapi/backend';

@Injectable({
  providedIn: 'root'
})
export class InventoriesService {
  private readonly api = inject(InventoriesOpenApi);

  getCurrentInventory(): Observable<DtoEventInventory> {
    return this.api.getCurrentInventory();
  }

  getInventoryStockItems(inventoryId: number) {
    return this.api.getInventoryStockItems(inventoryId);
  }

  test(): Observable<void> {
    return of();
  }
}

