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
import { LoadingListsService } from '../api/loading-lists.service';
import { BarcodeScannerService, BarcodeScannerStatus } from '../api/barcode-scanner.service';
import { SignalrService } from '../api/notifications/signalr.service';
import { MessageService } from 'primeng/api';
import {DtoLoadingList, DtoScanSession, DtoScanSessionArticle} from '../api/openapi/backend';

export interface ScanSessionState {
  selectedScanSession: DtoScanSession | null;
  scanSessionArticles: DtoScanSessionArticle[];
  tradeEvents: DtoLoadingList[];
  tradeEventName: string | null;
  barcodeScannerStatus: BarcodeScannerStatus | null;
  isLoading: boolean;
  error: string | null;
}

const initialState: ScanSessionState = {
  selectedScanSession: null,
  scanSessionArticles: [],
  tradeEvents: [],
  tradeEventName: null,
  barcodeScannerStatus: null,
  isLoading: false,
  error: null,
};

export const ScanSessionStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed((state) => ({
    hasInventory: computed(() => state.selectedScanSession() !== null),
    inventoryId: computed(() => state.selectedScanSession()?.id ?? null),
    isScannerConnected: computed(
      () => state.barcodeScannerStatus()?.isConnected ?? false
    ),
  })),
  withMethods(
    (
      store,
      scanSessionsService = inject(ScanSessionsService),
      loadingListsService = inject(LoadingListsService),
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
                  console.error('Error loading stock items:', error);
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
            console.error('Error loading barcode scanner status:', error);
            patchState(store, {
              barcodeScannerStatus: {
                isConnected: false,
                status: 'Error loading status',
              },
            });
          },
        });
      };

      // Helper function to create a trade event and add it to the list
      const createLoadingListInternal = (name: string) => {
        return loadingListsService.addLoadingList({ name }).pipe(
          tap((tradeEvent) => {
            // Add the new trade event to the list
            patchState(store, {
              tradeEvents: [...store.tradeEvents(), tradeEvent],
            });
          })
        );
      };

      return {
        // Load current scan session and its stock items
        readCurrentScanSession: rxMethod<void>(
          pipe(
            tap(() => patchState(store, { isLoading: true, error: null })),
            switchMap(() =>
              scanSessionsService.getCurrentScanSession().pipe(
                tapResponse({
                  next: (scanSession) => {
                    // Find the trade event name for this scanSession
                    const tradeEventName = scanSession.loadingListId
                      ? store.tradeEvents().find(te => te.id === scanSession.loadingListId)?.name ?? null
                      : null;

                    patchState(store, {
                      selectedScanSession: scanSession,
                      tradeEventName,
                      isLoading: false,
                    });
                    // Load stock items if scanSession exists
                    if (scanSession.id) {
                      loadScanSessionArticlesInternal(scanSession.id);
                    }
                  },
                  error: (error: Error) => {
                    console.error('Error loading inventory:', error);
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

        // Load stock items for a specific inventory
        readScanSessionArticles: loadScanSessionArticlesInternal,

        // Load all loading lists
        readLoadingLists: rxMethod<void>(
          pipe(
            tap(() => patchState(store, { isLoading: true, error: null })),
            switchMap(() =>
              loadingListsService.getLoadingLists().pipe(
                tapResponse({
                  next: (events) => {
                    patchState(store, {
                      tradeEvents: events,
                      isLoading: false,
                    });
                  },
                  error: (error: Error) => {
                    console.error('Error reading loading lists:', error);
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

        // Load barcode scanner status
        loadBarcodeScannerStatus: rxMethod<void>(
          pipe(
            switchMap(() =>
              barcodeScannerService.getStatus().pipe(
                tapResponse({
                  next: (status) => {
                    patchState(store, { barcodeScannerStatus: status });
                  },
                  error: (error: Error) => {
                    console.error('Error loading barcode scanner status:', error);
                    patchState(store, {
                      barcodeScannerStatus: {
                        isConnected: false,
                        status: 'Error loading status',
                      },
                    });
                  },
                })
              )
            )
          )
        ),

        // Select an inventory by ID and reload its data
        selectInventory: rxMethod<number>(
          pipe(
            tap(() => patchState(store, { isLoading: true, error: null })),
            switchMap((inventoryId) =>
              scanSessionsService.getScanSession(inventoryId).pipe(
                tapResponse({
                  next: (scanSession) => {
                    // Find the trade event name for this scanSession
                    const tradeEventName = scanSession.loadingListId
                      ? store.tradeEvents().find(te => te.id === scanSession.loadingListId)?.name ?? null
                      : null;

                    patchState(store, {
                      selectedScanSession: scanSession,
                      tradeEventName,
                      isLoading: false,
                    });
                    loadScanSessionArticlesInternal(inventoryId);
                  },
                  error: (error: Error) => {
                    console.error('Error selecting inventory:', error);
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

        // Start a new inventory for a trade event
        startNewInventory: rxMethod<number | undefined>(
          pipe(
            tap(() => patchState(store, { isLoading: true, error: null })),
            switchMap((tradeEventId) => scanSessionsService.createScanSession(tradeEventId)),
            tapResponse({
              next: (scanSession) => {
                const loadingListName = scanSession.loadingListId
                  ? store.tradeEvents().find(te => te.id === scanSession.loadingListId)?.name ?? null
                  : "Unbekannt";

                patchState(store, {
                  selectedScanSession: scanSession,
                  tradeEventName: loadingListName,
                  scanSessionArticles: [],
                  isLoading: false,
                });
              },
              error: (error: Error) => {
                console.error('Error starting new inventory:', error);
                patchState(store, {
                  isLoading: false,
                  error: error.message,
                });
              },
            })
          )
        ),

        // Create a new trade event
        createTradeEvent: rxMethod<string>(
          pipe(
            tap(() => patchState(store, { isLoading: true, error: null })),
            switchMap((name) =>
              createLoadingListInternal(name).pipe(
                tapResponse({
                  next: () => {
                    patchState(store, { isLoading: false });
                  },
                  error: (error: Error) => {
                    console.error('Error creating trade event:', error);
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

        // Create a new trade event and start inventory for it
        createTradeEventAndStartInventory: rxMethod<string>(
          pipe(
            tap(() => patchState(store, { isLoading: true, error: null })),
            switchMap((name) =>
              createLoadingListInternal(name).pipe(
                switchMap((tradeEvent) => {
                  // Start inventory for the new trade event
                  if (tradeEvent.id) {
                    return scanSessionsService.createScanSession(tradeEvent.id).pipe(
                      tapResponse({
                        next: (scanSession) => {
                          patchState(store, {
                            selectedScanSession: scanSession,
                            tradeEventName: tradeEvent.name ?? null,
                            scanSessionArticles: [],
                            isLoading: false,
                          });
                          if (scanSession.id) {
                            loadScanSessionArticlesInternal(scanSession.id);
                          }
                        },
                        error: (error: Error) => {
                          console.error('Error starting inventory:', error);
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
                      error: 'Trade event created but no ID returned',
                    });
                    return [];
                  }
                }),
                tapResponse({
                  next: () => {},
                  error: (error: Error) => {
                    console.error('Error creating trade event:', error);
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

        // Reload stock items for a current scan session (e.g., after SignalR event)
        reloadStockItems: () => {
          const inventoryId = store.inventoryId();
          if (inventoryId) {
            loadScanSessionArticlesInternal(inventoryId);
          }
        },

        // Setup SignalR listener for stock changes
        setupSignalRListener: () => {
          signalrService.onBarcodeScanned((msg) => {
            console.log('StockChanged event received:', msg);
            const inventoryId = store.inventoryId();
            if (inventoryId) {
              loadScanSessionArticlesInternal(inventoryId);
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
      store.readCurrentScanSession();
      store.readLoadingLists();
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

