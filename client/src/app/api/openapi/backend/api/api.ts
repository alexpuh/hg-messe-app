export * from './articles.openapi.service';
import { ArticlesOpenApi } from './articles.openapi.service';
export * from './eventInventories.openapi.service';
import { EventInventoriesOpenApi } from './eventInventories.openapi.service';
export * from './tradeEvents.openapi.service';
import { TradeEventsOpenApi } from './tradeEvents.openapi.service';
export const APIS = [ArticlesOpenApi, EventInventoriesOpenApi, TradeEventsOpenApi];
