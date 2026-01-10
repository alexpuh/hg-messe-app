import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RequiredStockSetup } from './required-stock-setup';

describe('RequiredStockSetup', () => {
  let component: RequiredStockSetup;
  let fixture: ComponentFixture<RequiredStockSetup>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RequiredStockSetup]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RequiredStockSetup);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
