export * from './articles.openapi.service';
import { ArticlesOpenApi } from './articles.openapi.service';
export * from './barcodeScanner.openapi.service';
import { BarcodeScannerOpenApi } from './barcodeScanner.openapi.service';
export * from './loadingLists.openapi.service';
import { LoadingListsOpenApi } from './loadingLists.openapi.service';
export * from './scanSessions.openapi.service';
import { ScanSessionsOpenApi } from './scanSessions.openapi.service';
export const APIS = [ArticlesOpenApi, BarcodeScannerOpenApi, LoadingListsOpenApi, ScanSessionsOpenApi];
