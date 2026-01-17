import { TestBed } from '@angular/core/testing';
import { TradeEventsService } from './trade-events.service';

describe('TradeEventsService', () => {
  let service: TradeEventsService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(TradeEventsService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});


