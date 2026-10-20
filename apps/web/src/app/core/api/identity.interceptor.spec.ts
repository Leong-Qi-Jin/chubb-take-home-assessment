import {
  HttpClient,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { Identity } from '../identity/identity';
import { identityInterceptor } from './identity.interceptor';

describe('identityInterceptor', () => {
  let client: HttpClient;
  let http: HttpTestingController;
  let identity: Identity;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([identityInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    client = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
    identity = TestBed.inject(Identity);
  });

  afterEach(() => http.verify());

  it('sends exactly the selected claimant identity', () => {
    client.get('/api/v1/claims').subscribe();
    const request = http.expectOne('/api/v1/claims');
    expect(request.request.headers.get('X-Claimant-Email')).toBe(
      'alex.tan@example.com',
    );
    expect(request.request.headers.has('X-Staff-Id')).toBe(false);
    request.flush({});
  });

  it('switches to the officer identity header', () => {
    identity.select('sam');
    client.get('/api/v1/dashboard').subscribe();
    const request = http.expectOne('/api/v1/dashboard');
    expect(request.request.headers.get('X-Staff-Id')).toBe(
      '00000000-0000-0000-0000-000000000101',
    );
    expect(request.request.headers.has('X-Claimant-Email')).toBe(false);
    request.flush({});
  });
});
