import { Injectable, inject } from '@angular/core';
import {Observable} from 'rxjs';
import {DtoCombinedArticle, DtoScanSession, DtoScanSessionArticle, Ort, ScanSessionsOpenApi, ScanSessionType} from './openapi/backend';

@Injectable({
  providedIn: 'root'
})
export class ScanSessionsService {
  private readonly api = inject(ScanSessionsOpenApi);

  getAllScanSessions(): Observable<Array<DtoScanSession>> {
    return this.api.getAllScanSessions();
  }

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

  createScanSession(
    sessionType: ScanSessionType,
    ort: Ort,
    dispatchSheetId: number | null,
  ): Observable<DtoScanSession> {
    return this.api.createScanSession(ort, sessionType, dispatchSheetId ?? undefined);
  }

  getCombinedArticles(standSessionId: number, lagerSessionId: number): Observable<Array<DtoCombinedArticle>> {
    return this.api.getCombinedArticles(standSessionId, lagerSessionId);
  }

  getCombinedArticlesExcel(standSessionId: number, lagerSessionId: number): Observable<Blob> {
    return this.api.getCombinedArticlesExcel(standSessionId, lagerSessionId);
  }
}

