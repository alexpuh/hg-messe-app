import {Component, inject, OnInit, signal} from '@angular/core';
import {ArticlesService} from '../api/articles.service';
import {DtoArticle} from '../api/openapi/backend';

@Component({
  selector: 'app-home',
  imports: [],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home implements OnInit {
  private articlesService = inject(ArticlesService)
  protected articles = signal<DtoArticle[]>([]);

  ngOnInit(): void {
    this.articlesService.getArticles().subscribe((articles) => {
      this.articles.set(articles);
    })
  }

}



