import { Routes } from '@angular/router';
import {ScanSession} from './components/scan-session/scan-session.component';
import {RequiredStockSetup} from './components/required-stock-setup/required-stock-setup';

export const routes: Routes = [
  { path: '', redirectTo: '/scan-session', pathMatch: 'full' },
  { path: 'scan-session', component: ScanSession },
  { path: 'config', component: RequiredStockSetup}
];
