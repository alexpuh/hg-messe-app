import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { DtoEventInventory } from './openapi/backend';
import {EventInventoriesOpenApi} from './openapi/backend/api/eventInventories.openapi.service';
import {DtoStockItem} from './openapi/backend/model/dtoStockItem';

@Injectable({
  providedIn: 'root'
})
export class EventInventoriesService {
  private readonly api = inject(EventInventoriesOpenApi);

  getCurrentInventory(): Observable<DtoEventInventory> {
    return this.api.getCurrentInventory();
  }

  getStockFromCurrentInventory(id: number): Observable<Array<DtoStockItem>> {
    return this.api.getStockFromCurrentInventory(id);
  }

  addEventInventory(eventInventory?: DtoEventInventory): Observable<DtoEventInventory> {
    return this.api.addEventInventory(eventInventory);
  }

  setCurrentInventory(id: number): Observable<DtoEventInventory> {
    return this.api.setCurrentInventory(id);
  }

  getEventInventories(count?: number): Observable<Array<DtoEventInventory>> {
    return this.api.getEventInventories(count);
  }

  test(): Observable<void> {
    return this.api.test();
  }
}

