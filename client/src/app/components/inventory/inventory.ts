import { Component } from '@angular/core';
import {Select, SelectChangeEvent} from 'primeng/select';
import {Button} from 'primeng/button';
import {RouterLink} from '@angular/router';
import {GermanDatePipe} from '../../pipes/german-date.pipe';

@Component({
  selector: 'app-inventory',
  imports: [
    Select,
    Button,
    RouterLink,
    GermanDatePipe
  ],
  templateUrl: './inventory.html',
  styleUrl: './inventory.scss',
})
export class Inventory {
  protected messeList: string[] = ['Frankfurt', 'Berlin'];
  protected startedAt: Date = new Date();

  protected onMesseChange($event: SelectChangeEvent) {

  }
}
