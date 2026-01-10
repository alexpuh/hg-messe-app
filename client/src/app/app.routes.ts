import { Routes } from '@angular/router';
import {Inventory} from './components/inventory/inventory';
import {RequiredStockSetup} from './components/required-stock-setup/required-stock-setup';

export const routes: Routes = [
  { path: '', redirectTo: '/inventory', pathMatch: 'full' },
  { path: 'inventory', component: Inventory },
  { path: 'config', component: RequiredStockSetup}
];
