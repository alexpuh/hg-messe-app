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
import { pipe, switchMap, tap } from 'rxjs';
import { tapResponse } from '@ngrx/operators';
import { ScanSessionsService } from '../api/scan-sessions.service';
import { DispatchSheetsService } from '../api/dispatch-sheets.service';
import { BarcodeScannerService, BarcodeScannerStatus } from '../api/barcode-scanner.service';
import { SignalrService } from '../api/notifications/signalr.service';
import { MessageService } from 'primeng/api';
import {DtoDispatchSheet, DtoScanSession, DtoScanSessionArticle} from '../api/openapi/backend';

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
      // Helper function to load stock items
      const loadScanSessionArticlesInternal = rxMethod<number>(
        pipe(
          tap(() => patchState(store, { isLoading: true, error: null })),
          switchMap((scanSessionId) =>
            scanSessionsService.getScanSessionArticles(scanSessionId).pipe(
              tapResponse({
                next: (items) => {
                  patchState(store, {
                    scanSessionArticles: items,
                    isLoading: false,
                  });
                },
                error: (error: Error) => {
                  messageService.add({ severity: 'error', summary: 'Fehler', detail: `Artikel konnten nicht geladen werden: ${error.message}` });
                  patchState(store, {
                    isLoading: false,
                    error: error.message,
                  });
                },
              })
            )
          )
        )
      );

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

      // Helper function to create a dispatch sheet and add it to the list
      const createDispatchSheetInternal = (name: string) => {
        return dispatchSheetsService.addDispatchSheet({ name }).pipe(
          tap((dispatchSheet) => {
            patchState(store, {
              dispatchSheets: [...store.dispatchSheets(), dispatchSheet],
            });
          })
        );
      };

      return {
        // Load the current scan session and its stock items
        loadCurrentScanSession: rxMethod<void>(
          pipe(
            tap(() => patchState(store, { isLoading: true, error: null })),
            switchMap(() => scanSessionsService.getCurrentScanSession()),
            tapResponse({
              next: (scanSession) => {
                // Find the dispatch sheet name for this scanSession
                const dispatchSheetName = scanSession.dispatchSheetId
                  ? store.dispatchSheets().find(te => te.id === scanSession.dispatchSheetId)?.name ?? null
                  : null;

                patchState(store, {
                  selectedScanSession: scanSession,
                  dispatchSheetName: dispatchSheetName,
                  isLoading: false,
                });
                if (scanSession.id) {
                  loadScanSessionArticlesInternal(scanSession.id);
                }
              },
              error: (error: Error) => {
                messageService.add({ severity: 'error', summary: 'Fehler', detail: `Scan-Session konnte nicht geladen werden: ${error.message}` });
                patchState(store, {
                  isLoading: false,
                  error: error.message,
                });
              },
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

        // Start a new scan session for a dispatch sheet
        startNewScanSession: rxMethod<number | undefined>(
          pipe(
            tap(() => patchState(store, { isLoading: true, error: null })),
            switchMap((dispatchSheetId) => scanSessionsService.createScanSession(dispatchSheetId)),
            tapResponse({
              next: (scanSession) => {
                const dispatchSheetName = scanSession.dispatchSheetId
                  ? store.dispatchSheets().find(te => te.id === scanSession.dispatchSheetId)?.name ?? null
                  : "Unbekannt";

                patchState(store, {
                  selectedScanSession: scanSession,
                  dispatchSheetName: dispatchSheetName,
                  scanSessionArticles: [],
                  isLoading: false,
                });
              },
              error: (error: Error) => {
                messageService.add({ severity: 'error', summary: 'Fehler', detail: `Scan-Session konnte nicht gestartet werden: ${error.message}` });
                patchState(store, {
                  isLoading: false,
                  error: error.message,
                });
              },
            })
          )
        ),

        // Create a new dispatch sheet
        createDispatchSheet: rxMethod<string>(
          pipe(
            tap(() => patchState(store, { isLoading: true, error: null })),
            switchMap((name) => createDispatchSheetInternal(name)),
            tapResponse({
              next: () => {
                patchState(store, { isLoading: false });
              },
              error: (error: Error) => {
                messageService.add({ severity: 'error', summary: 'Fehler', detail: `Beladeliste konnte nicht erstellt werden: ${error.message}` });
                patchState(store, {
                  isLoading: false,
                  error: error.message,
                });
              },
            })
          )
        ),

        // Create a new dispatch sheet and start a scan session for it
        createDispatchSheetAndStartScanSession: rxMethod<string>(
          pipe(
            tap(() => patchState(store, { isLoading: true, error: null })),
            switchMap((name) =>
              createDispatchSheetInternal(name).pipe(
                switchMap((dispatchSheet) => {
                  // Start a scan session for the new dispatch sheet
                  if (dispatchSheet.id) {
                    return scanSessionsService.createScanSession(dispatchSheet.id).pipe(
                      tapResponse({
                        next: (scanSession) => {
                          patchState(store, {
                            selectedScanSession: scanSession,
                            dispatchSheetName: dispatchSheet.name ?? null,
                            scanSessionArticles: [],
                            isLoading: false,
                          });
                          if (scanSession.id) {
                            loadScanSessionArticlesInternal(scanSession.id);
                          }
                        },
                        error: (error: Error) => {
                          messageService.add({ severity: 'error', summary: 'Fehler', detail: `Scan-Session konnte nicht gestartet werden: ${error.message}` });
                          patchState(store, {
                            isLoading: false,
                            error: error.message,
                          });
                        },
                      })
                    );
                  } else {
                    patchState(store, {
                      isLoading: false,
                      error: 'Dispatch sheet created but no ID returned',
                    });
                    return [];
                  }
                }),
                tapResponse({
                  next: () => {},
                  error: (error: Error) => {
                    messageService.add({ severity: 'error', summary: 'Fehler', detail: `Beladeliste konnte nicht erstellt werden: ${error.message}` });
                    patchState(store, {
                      isLoading: false,
                      error: error.message,
                    });
                  },
                })
              )
            )
          )
        ),

        // Reload article units for a current scan session (e.g., after SignalR event)
        reloadScanSessionArticleUnits: () => {
          const scanSessionId = store.scanSessionId();
          if (scanSessionId) {
            loadScanSessionArticlesInternal(scanSessionId);
          }
        },

        // Setup SignalR listener for stock changes
        setupSignalRListener: () => {
          signalrService.onBarcodeScanned((msg) => {
            console.log('StockChanged event received:', msg);
            const scanSessionId = store.scanSessionId();
            if (scanSessionId) {
              loadScanSessionArticlesInternal(scanSessionId);
            }
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

