import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MesseSelection } from './messe-selection';

describe('MesseSelection', () => {
  let component: MesseSelection;
  let fixture: ComponentFixture<MesseSelection>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MesseSelection]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MesseSelection);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
