import { Pipe, PipeTransform } from '@angular/core';
import {formatDate} from "@angular/common";

@Pipe({
  standalone: true,
  name: 'germanDate'
})
export class GermanDatePipe implements PipeTransform {
  transform(value: Date | string | undefined): string | null {
    if (!value) {
      return null;
    }
    return formatDate(value, 'dd.MM.yyyy', 'de');
  }
}
