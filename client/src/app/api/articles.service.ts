import { Injectable } from '@angular/core';
import {ArticlesOpenApi} from './openapi/backend';

@Injectable({
  providedIn: 'root',
})
export class ArticlesService {
  constructor(private articlesOpenApi: ArticlesOpenApi) {
  }

  public getArticles() {
    return this.articlesOpenApi.getArticles();
  }
}
