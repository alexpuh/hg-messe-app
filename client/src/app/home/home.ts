import {Component, inject, OnInit, signal} from '@angular/core';
import {ArticlesService} from '../api/articles.service';
import {DtoArticle} from '../api/openapi/backend';
import {Tab, TabList, TabPanel, TabPanels, Tabs} from 'primeng/tabs';
import {Inventory} from '../components/inventory/inventory';
import {RequiredStockSetup} from '../components/required-stock-setup/required-stock-setup';
import {Select, SelectChangeEvent} from 'primeng/select';
import {FormsModule} from '@angular/forms';

@Component({
  selector: 'app-home',
  imports: [
    Tabs,
    TabList,
    Tab,
    TabPanels,
    TabPanel,
    Inventory,
    RequiredStockSetup,
    Select,
    FormsModule
  ],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home implements OnInit {
  private articlesService = inject(ArticlesService)
  protected articles = signal<DtoArticle[]>([]);
  protected messeList: string[] = ['Frankfurt', 'Berlin'];

  ngOnInit(): void {
    this.articlesService.getArticles().subscribe((articles) => {
      this.articles.set(articles);
    })
  }

  protected onMesseChange($event: SelectChangeEvent) {

  }
}



