import { Pipe, PipeTransform } from '@angular/core';
import {formatDate} from "@angular/common";

@Pipe({
  standalone: true,
  name: 'germanDateTime'
})
export class GermanDateTimePipe implements PipeTransform {
  transform(value: Date | string | undefined): string | null {
    if (!value) {
      return null;
    }
    return formatDate(value, 'dd.MM.yyyy HH:mm:ss', 'de');
  }
}
