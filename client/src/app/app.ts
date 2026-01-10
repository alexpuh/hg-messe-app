import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {Button} from 'primeng/button';
import {Inventory} from './components/inventory/inventory';
import {RequiredStockSetup} from './components/required-stock-setup/required-stock-setup';
import {Select, SelectChangeEvent} from 'primeng/select';
import {Tab, TabList, TabPanel, TabPanels, Tabs} from 'primeng/tabs';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Button, Inventory, RequiredStockSetup, Select, Tab, TabList, TabPanel, TabPanels, Tabs],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('client');
  protected messeList: string[] = ['Frankfurt', 'Berlin'];

  protected onMesseChange($event: SelectChangeEvent) {

  }
}
