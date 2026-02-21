import { Injectable, inject } from '@angular/core';
import {Observable} from 'rxjs';
import {DtoScanSession, DtoScanSessionArticle, ScanSessionsOpenApi} from './openapi/backend';

@Injectable({
  providedIn: 'root'
})
export class ScanSessionsService {
  private readonly api = inject(ScanSessionsOpenApi);

  getCurrentScanSession(): Observable<DtoScanSession> {
    return this.api.getCurrentScanSession();
  }

  getScanSession(id: number): Observable<DtoScanSession> {
    return this.api.getScanSession(id);
  }

  getScanSessionArticles(sessionId: number): Observable<Array<DtoScanSessionArticle>> {
    return this.api.getScanSessionArticles(sessionId);
  }

  getScanSessionArticlesExcel(sessionId: number): Observable<Blob> {
    return this.api.getScanSessionArticlesExcel(sessionId);
  }

  createScanSession(loadingListId: number | null): Observable<DtoScanSession> {
    return this.api.createScanSession(loadingListId ?? undefined);
  }
}

