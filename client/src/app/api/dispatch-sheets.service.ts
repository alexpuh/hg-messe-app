import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  DispatchSheetsOpenApi, DtoDispatchSheet, DtoDispatchSheetArticleUnit,
  SetRequiredUnitsRequest
} from './openapi/backend';


@Injectable({
  providedIn: 'root'
})
export class DispatchSheetsService {
  private readonly api = inject(DispatchSheetsOpenApi);

  getDispatchSheets(): Observable<Array<DtoDispatchSheet>> {
    return this.api.getDispatchSheets();
  }

  addDispatchSheet(dispatchSheet?: DtoDispatchSheet): Observable<DtoDispatchSheet> {
    return this.api.addDispatchSheet(dispatchSheet);
  }

  setRequiredUnits(dispatchSheetId: number, request?: SetRequiredUnitsRequest): Observable<void> {
    return this.api.setRequiredUnits(dispatchSheetId, request);
  }

  deleteRequiredUnit(dispatchSheetId: number, unitId: number): Observable<void> {
    return this.api.deleteRequiredUnit(dispatchSheetId, unitId);
  }

  getDispatchSheetArticleUnits(dispatchSheetId: number): Observable<Array<DtoDispatchSheetArticleUnit>> {
    return this.api.getDispatchSheetArticleUnits(dispatchSheetId);
  }
}

