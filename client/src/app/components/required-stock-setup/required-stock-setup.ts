import { Component } from '@angular/core';
import {Button} from 'primeng/button';
import {RouterLink} from '@angular/router';

@Component({
  selector: 'app-required-stock-setup',
  imports: [
    Button,
    RouterLink
  ],
  templateUrl: './required-stock-setup.html',
  styleUrl: './required-stock-setup.scss',
})
export class RequiredStockSetup {
  protected messeName: string = "Hannover";

}
