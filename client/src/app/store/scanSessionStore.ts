import { computed, inject } from '@angular/core';
import {
  patchState,
  signalStore,
  withComputed,
  withHooks,
  withMethods,
  withState,
} from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import {catchError, EMPTY, map, mergeWith, pipe, switchMap, tap} from 'rxjs';
import { tapResponse } from '@ngrx/operators';
import { ScanSessionsService } from '../api/scan-sessions.service';
import { DispatchSheetsService } from '../api/dispatch-sheets.service';
import { BarcodeScannerService, BarcodeScannerStatus } from '../api/barcode-scanner.service';
import { SignalrService } from '../api/notifications/signalr.service';
import { MessageService } from 'primeng/api';
import {DtoDispatchSheet, DtoScanSession, DtoScanSessionArticle} from '../api/openapi/backend';
import {HttpErrorResponse} from '@angular/common/http';

export interface ScanSessionState {
  selectedScanSession: DtoScanSession | null;
  scanSessionArticles: DtoScanSessionArticle[];
  dispatchSheets: DtoDispatchSheet[];
  dispatchSheetName: string | null;
  barcodeScannerStatus: BarcodeScannerStatus | null;
  isLoading: boolean;
  error: string | null;
}

const initialState: ScanSessionState = {
  selectedScanSession: null,
  scanSessionArticles: [],
  dispatchSheets: [],
  dispatchSheetName: null,
  barcodeScannerStatus: null,
  isLoading: false,
  error: null,
};

export const ScanSessionStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed((state) => ({
    hasScanSession: computed(() => state.selectedScanSession() !== null),
    scanSessionId: computed(() => state.selectedScanSession()?.id ?? null),
    isScannerConnected: computed(
      () => state.barcodeScannerStatus()?.isConnected ?? false
    ),
  })),
  withMethods(
    (
      store,
      scanSessionsService = inject(ScanSessionsService),
      dispatchSheetsService = inject(DispatchSheetsService),
      barcodeScannerService = inject(BarcodeScannerService),
      signalrService = inject(SignalrService),
      messageService = inject(MessageService)
    ) => {


      const displayErrorAndStopLoading = (error: Error, detail: string)=> {
        {
          messageService.add({ severity: 'error', summary: 'Fehler', detail: `${detail}: ${error.message}` });
          patchState(store, {
            isLoading: false,
            error: error.message,
          });
        }
      }

      const reloadScanSessionArticlesInternal = rxMethod<void>(
        pipe(
          tap(() => patchState(store, { isLoading: true, error: null })),
          switchMap(() => {
            const scanSessionId = store.scanSessionId();
            if (scanSessionId) {
              return scanSessionsService.getScanSessionArticles(scanSessionId);
            }
            else {
              return EMPTY;
            }
          }),
          tapResponse({
            next: articles => {
              patchState(store, {
                scanSessionArticles: articles,
                isLoading: false,
              });
            },
            error: (error: Error) => displayErrorAndStopLoading(error, 'Scan-Session konnte nicht geladen werden')
          })
        )
      )


      // Helper function to load barcode scanner status
      const loadBarcodeScannerStatusInternal = () => {
        barcodeScannerService.getStatus().subscribe({
          next: (status) => {
            patchState(store, { barcodeScannerStatus: status });
          },
          error: (error: Error) => {
            messageService.add({ severity: 'error', summary: 'Fehler', detail: `Scanner-Status konnte nicht geladen werden: ${error.message}` });
            patchState(store, {
              barcodeScannerStatus: {
                isConnected: false,
                status: 'Error loading status',
              },
            });
          },
        });
      };

      const setupSessionAndStopLoading = (scanSession: DtoScanSession, articles: DtoScanSessionArticle[]) => {
        const dispatchSheetName = scanSession.dispatchSheetId
          ? store.dispatchSheets().find(te => te.id === scanSession.dispatchSheetId)?.name ?? null
          : null;
        patchState(store, {
          selectedScanSession: scanSession,
          scanSessionArticles: articles,
          dispatchSheetName: dispatchSheetName,
          isLoading: false,
        });
      }

      return {
        reloadScanSessionArticles: reloadScanSessionArticlesInternal,
        loadCurrentScanSession: rxMethod<void>(
          pipe(
            tap(() => patchState(store, { isLoading: true, error: null })),
            switchMap(() => scanSessionsService.getCurrentScanSession().pipe(
              catchError(err => {
                if (err instanceof HttpErrorResponse && err.status === 404) {
                  patchState(store, { selectedScanSession: null, isLoading: false, scanSessionArticles: [] });
                  return EMPTY;
                } else {
                  throw err;
                }
              })
            )),
            switchMap(scanSession => scanSessionsService.getScanSessionArticles(scanSession.id!).pipe(map(articles => ({scanSession, articles})))),
            tapResponse({
              next: (r) => setupSessionAndStopLoading(r.scanSession, r.articles),
              error: (error: Error) => displayErrorAndStopLoading(error, 'Scan-Session konnte nicht geladen werden')
            })
          )
        ),

        startNewScanSession: rxMethod<number | null>(
          pipe(
            tap(() => patchState(store, { isLoading: true, error: null })),
            switchMap((dispatchSheetId) => scanSessionsService.createScanSession(dispatchSheetId)),
            switchMap(scanSession => scanSessionsService.getScanSessionArticles(scanSession.id!).pipe(map(articles => ({scanSession, articles})))),
            tapResponse({
              next: (r) => setupSessionAndStopLoading(r.scanSession, r.articles),
              error: (error: Error) => displayErrorAndStopLoading(error, 'Scan-Session konnte nicht gestartet werden')
            })
          )
        ),


        // Load all dispatch sheets
        loadDispatchSheets: rxMethod<void>(
          pipe(
            tap(() => patchState(store, { isLoading: true, error: null })),
            switchMap(() => dispatchSheetsService.getDispatchSheets()),
            tapResponse({
              next: (dispatchSheets) => {
                patchState(store, {
                  dispatchSheets: dispatchSheets,
                  isLoading: false,
                });
              },
              error: (error: Error) => {
                messageService.add({ severity: 'error', summary: 'Fehler', detail: `Beladelisten konnten nicht geladen werden: ${error.message}` });
                patchState(store, {
                  isLoading: false,
                  error: error.message,
                });
              },
            })
          )
        ),

        // Load barcode scanner status
        loadBarcodeScannerStatus: rxMethod<void>(
          pipe(
            switchMap(() => barcodeScannerService.getStatus()),
            tapResponse({
              next: (status) => {
                patchState(store, { barcodeScannerStatus: status });
              },
              error: (error: Error) => {
                messageService.add({ severity: 'error', summary: 'Fehler', detail: `Scanner-Status konnte nicht geladen werden: ${error.message}` });
                patchState(store, {
                  barcodeScannerStatus: {
                    isConnected: false,
                    status: 'Error loading status',
                  },
                });
              },
            })
          )
        ),

        // Create a new dispatch sheet
        createDispatchSheet: rxMethod<string>(
          pipe(
            tap(() => patchState(store, { isLoading: true, error: null })),
            switchMap(name => dispatchSheetsService.addDispatchSheet({name})),
            tapResponse({
              next: dispatchSheet => {
                patchState(store, {
                  isLoading: false,
                  dispatchSheets: [...store.dispatchSheets(), dispatchSheet]
                });
              },
              error: (error: Error) => displayErrorAndStopLoading(error, 'Beladeliste konnte nicht erstellt werden'),
            })
          )
        ),

        // Create a new dispatch sheet and start a scan session for it
        createDispatchSheetAndStartScanSession: rxMethod<string>(
          pipe(
            tap(() => patchState(store, { isLoading: true, error: null })),
            switchMap(name => dispatchSheetsService.addDispatchSheet({name})),
            switchMap(dispatchSheet => scanSessionsService.createScanSession(dispatchSheet.id!)),
            switchMap(scanSession => scanSessionsService.getScanSessionArticles(scanSession.id!).pipe(map(articles => ({scanSession, articles})))),
            tapResponse({
              next: (r) => setupSessionAndStopLoading(r.scanSession, r.articles),
              error: (error: Error) => displayErrorAndStopLoading(error, 'Scan-Session konnte nicht gestartet werden')
            })
          )
        ),

        // Setup SignalR listener for stock changes
        setupSignalRListener: () => {
          signalrService.onBarcodeScanned((msg) => {
            console.log('StockChanged event received:', msg);
            reloadScanSessionArticlesInternal();
          });
        },

        // Setup SignalR listener for scanner status changes
        setupScannerStatusListener: () => {
          signalrService.onScannerStatusChanged(() => {
            console.log('Scanner status changed, reloading...');
            loadBarcodeScannerStatusInternal();
          });
        },

        // Setup SignalR listener for barcode errors
        setupBarcodeErrorListener: () => {
          signalrService.onBarcodeError((ean, errorMessage) => {
            console.error('Barcode error received:', { ean, errorMessage });
            messageService.add({
              severity: 'error',
              summary: 'Barcode-Fehler',
              detail: `EAN: ${ean} - ${errorMessage}`,
              life: 5000
            });
          });
        },
      };
    }
  ),
  withHooks({
    onInit(store) {
      // Load initial data
      store.loadCurrentScanSession();
      store.loadDispatchSheets();
      store.loadBarcodeScannerStatus();

      // Set up SignalR connection and listeners
      const signalrService = inject(SignalrService);
      signalrService.startConnection();
      store.setupSignalRListener();
      store.setupScannerStatusListener();
      store.setupBarcodeErrorListener();
    },
  })
);

