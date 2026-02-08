import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  DtoLoadingList,
  DtoLoadingListArticleUnit,
  LoadingListsOpenApi,
  SetRequiredUnitsRequest
} from './openapi/backend';


@Injectable({
  providedIn: 'root'
})
export class LoadingListsService {
  private readonly api = inject(LoadingListsOpenApi);

  getLoadingLists(): Observable<Array<DtoLoadingList>> {
    return this.api.getLoadingLists();
  }

  addLoadingList(loadingList?: DtoLoadingList): Observable<DtoLoadingList> {
    return this.api.addLoadingList(loadingList);
  }

  setRequiredUnits(loadingListId: number, request?: SetRequiredUnitsRequest): Observable<void> {
    return this.api.setRequiredUnits(loadingListId, request);
  }

  deleteRequiredUnit(loadingListId: number, unitId: number): Observable<void> {
    return this.api.deleteRequiredUnit(loadingListId, unitId);
  }

  getLoadingListArticleUnits(tradeEventId: number): Observable<Array<DtoLoadingListArticleUnit>> {
    return this.api.getLoadingListArticleUnits(tradeEventId);
  }
}

