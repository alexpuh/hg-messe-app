export * from './articles.openapi.service';
import { ArticlesOpenApi } from './articles.openapi.service';
export * from './barcodeScanner.openapi.service';
import { BarcodeScannerOpenApi } from './barcodeScanner.openapi.service';
export * from './dispatchSheets.openapi.service';
import { DispatchSheetsOpenApi } from './dispatchSheets.openapi.service';
export * from './scanSessions.openapi.service';
import { ScanSessionsOpenApi } from './scanSessions.openapi.service';
export const APIS = [ArticlesOpenApi, BarcodeScannerOpenApi, DispatchSheetsOpenApi, ScanSessionsOpenApi];
