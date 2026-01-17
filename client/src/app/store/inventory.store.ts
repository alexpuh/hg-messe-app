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
import {
  DtoEventInventory,
  DtoInventoryStockItem,
  DtoTradeEvent,
} from '../api/openapi/backend';
import { InventoriesService } from '../api/inventories.service';
import { TradeEventsService } from '../api/trade-events.service';
import { BarcodeScannerService, BarcodeScannerStatus } from '../api/barcode-scanner.service';
import { SignalrService } from '../api/notifications/signalr.service';

export interface InventoryState {
  selectedInventory: DtoEventInventory | null;
  stockItems: DtoInventoryStockItem[];
  tradeEvents: DtoTradeEvent[];
  tradeEventName: string | null;
  barcodeScannerStatus: BarcodeScannerStatus | null;
  isLoading: boolean;
  error: string | null;
}

const initialState: InventoryState = {
  selectedInventory: null,
  stockItems: [],
  tradeEvents: [],
  tradeEventName: null,
  barcodeScannerStatus: null,
  isLoading: false,
  error: null,
};

export const InventoryStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed((state) => ({
    hasInventory: computed(() => state.selectedInventory() !== null),
    inventoryId: computed(() => state.selectedInventory()?.id ?? null),
    isScannerConnected: computed(
      () => state.barcodeScannerStatus()?.isConnected ?? false
    ),
  })),
  withMethods(
    (
      store,
      inventoriesService = inject(InventoriesService),
      tradeEventsService = inject(TradeEventsService),
      barcodeScannerService = inject(BarcodeScannerService),
      signalrService = inject(SignalrService)
    ) => {
      // Helper function to load stock items
      const loadStockItemsInternal = rxMethod<number>(
        pipe(
          tap(() => patchState(store, { isLoading: true, error: null })),
          switchMap((inventoryId) =>
            inventoriesService.getInventoryStockItems(inventoryId).pipe(
              tapResponse({
                next: (items) => {
                  patchState(store, {
                    stockItems: items,
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

      // Helper function to create a trade event and add it to the list
      const createTradeEventInternal = (name: string) => {
        return tradeEventsService.addTradeEvent({ name }).pipe(
          tap((tradeEvent) => {
            // Add the new trade event to the list
            patchState(store, {
              tradeEvents: [...store.tradeEvents(), tradeEvent],
            });
          })
        );
      };

      return {
        // Load current inventory and its stock items
        loadCurrentInventory: rxMethod<void>(
          pipe(
            tap(() => patchState(store, { isLoading: true, error: null })),
            switchMap(() =>
              inventoriesService.getCurrentInventory().pipe(
                tapResponse({
                  next: (inventory) => {
                    // Find the trade event name for this inventory
                    const tradeEventName = inventory.tradeEventId
                      ? store.tradeEvents().find(te => te.id === inventory.tradeEventId)?.name ?? null
                      : null;

                    patchState(store, {
                      selectedInventory: inventory,
                      tradeEventName,
                      isLoading: false,
                    });
                    // Load stock items if inventory exists
                    if (inventory.id) {
                      loadStockItemsInternal(inventory.id);
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
        loadStockItems: loadStockItemsInternal,

        // Load all trade events
        loadTradeEvents: rxMethod<void>(
          pipe(
            tap(() => patchState(store, { isLoading: true, error: null })),
            switchMap(() =>
              tradeEventsService.getTradeEvents().pipe(
                tapResponse({
                  next: (events) => {
                    patchState(store, {
                      tradeEvents: events,
                      isLoading: false,
                    });
                  },
                  error: (error: Error) => {
                    console.error('Error loading trade events:', error);
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
              inventoriesService.getCurrentInventory().pipe(
                tapResponse({
                  next: (inventory) => {
                    // Find the trade event name for this inventory
                    const tradeEventName = inventory.tradeEventId
                      ? store.tradeEvents().find(te => te.id === inventory.tradeEventId)?.name ?? null
                      : null;

                    patchState(store, {
                      selectedInventory: inventory,
                      tradeEventName,
                      isLoading: false,
                    });
                    loadStockItemsInternal(inventoryId);
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
            switchMap((tradeEventId) => inventoriesService.createInventory(tradeEventId)),
            tapResponse({
              next: (inventory) => {
                const tradeEventName = inventory.tradeEventId
                  ? store.tradeEvents().find(te => te.id === inventory.tradeEventId)?.name ?? null
                  : "Unbekannt";

                patchState(store, {
                  selectedInventory: inventory,
                  tradeEventName,
                  stockItems: [],
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
              createTradeEventInternal(name).pipe(
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
              createTradeEventInternal(name).pipe(
                switchMap((tradeEvent) => {
                  // Start inventory for the new trade event
                  if (tradeEvent.id) {
                    return inventoriesService.createInventory(tradeEvent.id).pipe(
                      tapResponse({
                        next: (inventory) => {
                          patchState(store, {
                            selectedInventory: inventory,
                            tradeEventName: tradeEvent.name ?? null,
                            stockItems: [],
                            isLoading: false,
                          });
                          if (inventory.id) {
                            loadStockItemsInternal(inventory.id);
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

        // Reload stock items for current inventory (e.g., after SignalR event)
        reloadStockItems: () => {
          const inventoryId = store.inventoryId();
          if (inventoryId) {
            loadStockItemsInternal(inventoryId);
          }
        },

        // Setup SignalR listener for stock changes
        setupSignalRListener: () => {
          signalrService.onBarcodeScanned((msg) => {
            console.log('StockChanged event received:', msg);
            const inventoryId = store.inventoryId();
            if (inventoryId) {
              loadStockItemsInternal(inventoryId);
            }
          });
        },
      };
    }
  ),
  withHooks({
    onInit(store) {
      // Load initial data
      store.loadCurrentInventory();
      store.loadTradeEvents();
      store.loadBarcodeScannerStatus();

      // Setup SignalR connection and listener
      const signalrService = inject(SignalrService);
      signalrService.startConnection();
      store.setupSignalRListener();

      // Poll barcode scanner status every 5 seconds
      setInterval(() => {
        store.loadBarcodeScannerStatus();
      }, 5000);
    },
  })
);

