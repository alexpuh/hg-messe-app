export * from './articles.openapi.service';
import { ArticlesOpenApi } from './articles.openapi.service';
export * from './barcodeScanner.openapi.service';
import { BarcodeScannerOpenApi } from './barcodeScanner.openapi.service';
export * from './inventories.openapi.service';
import { InventoriesOpenApi } from './inventories.openapi.service';
export * from './tradeEvents.openapi.service';
import { TradeEventsOpenApi } from './tradeEvents.openapi.service';
export const APIS = [ArticlesOpenApi, BarcodeScannerOpenApi, InventoriesOpenApi, TradeEventsOpenApi];
