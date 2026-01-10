import { TestBed } from '@angular/core/testing';
import { EventInventoriesService } from './event-inventories.service';

describe('EventInventoriesService', () => {
  let service: EventInventoriesService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(EventInventoriesService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

