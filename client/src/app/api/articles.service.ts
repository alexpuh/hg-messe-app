import {inject, Injectable} from '@angular/core';
import {ArticlesOpenApi} from './openapi/backend';
import {Observable} from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ArticlesService {
  private api = inject(ArticlesOpenApi);

  uploadArticles(file: File): Observable<void> {
    const fileName = file.name;
    return this.api.uploadArticleList(fileName, file);
  }
}
