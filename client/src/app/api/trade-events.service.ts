import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { DtoTradeEvent, SetRequiredUnitsRequest } from './openapi/backend';
import {TradeEventsOpenApi} from './openapi/backend';

@Injectable({
  providedIn: 'root'
})
export class TradeEventsService {
  private readonly api = inject(TradeEventsOpenApi);

  getTradeEvents(): Observable<Array<DtoTradeEvent>> {
    return this.api.getTradeEvents();
  }

  getTradeEventById(id: number): Observable<DtoTradeEvent> {
    return this.api.getTradeEventById(id);
  }

  addTradeEvent(tradeEvent?: DtoTradeEvent): Observable<DtoTradeEvent> {
    return this.api.addTradeEvent(tradeEvent);
  }

  updateTradeEvent(id: number, tradeEvent?: DtoTradeEvent): Observable<void> {
    return this.api.updateTradeEvent(id, tradeEvent);
  }

  deleteTradeEvent(id: number): Observable<void> {
    return this.api.deleteTradeEvent(id);
  }

  getRequiredUnits(tradeEventId: number): Observable<{ [key: string]: number; }> {
    return this.api.getRequiredUnits(tradeEventId);
  }

  setRequiredUnits(tradeEventId: number, request?: SetRequiredUnitsRequest): Observable<void> {
    return this.api.setRequiredUnits(tradeEventId, request);
  }

  deleteRequiredUnit(tradeEventId: number, unitId: number): Observable<void> {
    return this.api.deleteRequiredUnit(tradeEventId, unitId);
  }
}

