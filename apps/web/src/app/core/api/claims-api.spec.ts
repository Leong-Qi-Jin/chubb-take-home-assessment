import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ClaimsApi } from './claims-api';

describe('ClaimsApi', () => {
  let api: ClaimsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(ClaimsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('repeats status filters for the open work queue', async () => {
    const response = api.list({
      status: ['Submitted', 'UnderReview'],
      page: 1,
    });
    await new Promise((resolve) => setTimeout(resolve));
    const request = http.expectOne((candidate) =>
      candidate.url.startsWith('/api/v1/claims?'),
    );
    request.flush({ items: [], page: 1, pageSize: 20, totalCount: 0 });
    await expect(response).resolves.toMatchObject({ totalCount: 0 });
  });

  it('sends optimistic concurrency headers for commands', async () => {
    const response = api.updateEstimate('CLM-DEMO-0007', '2500.00', '"4"');
    await new Promise((resolve) => setTimeout(resolve));
    const request = http.expectOne(
      '/api/v1/claims/CLM-DEMO-0007/estimated-liability',
    );
    expect(request.request.headers.get('If-Match')).toBe('"4"');
    expect(request.request.method).toBe('PUT');
    request.flush({ reference: 'CLM-DEMO-0007' }, { headers: { ETag: '"5"' } });
    await expect(response).resolves.toMatchObject({ etag: '"5"' });
  });

  it('exposes the current claim and ETag when a command conflicts', async () => {
    const response = api.updateEstimate('CLM-DEMO-0007', '2500.00', '"4"');
    await new Promise((resolve) => setTimeout(resolve));
    http.expectOne('/api/v1/claims/CLM-DEMO-0007/estimated-liability').flush(
      {
        status: 409,
        code: 'ClaimModified',
        detail: 'The claim was changed by someone else.',
        claim: { reference: 'CLM-DEMO-0007' },
      },
      { status: 409, statusText: 'Conflict', headers: { ETag: '"6"' } },
    );
    await expect(response).rejects.toMatchObject({
      status: 409,
      etag: '"6"',
      claim: { reference: 'CLM-DEMO-0007' },
    });
  });

  it('reports an unreachable service instead of a generic failure', async () => {
    const response = api.dashboard();
    await new Promise((resolve) => setTimeout(resolve));
    http
      .expectOne('/api/v1/dashboard')
      .error(new ProgressEvent('error'), { status: 0 });
    await expect(response).rejects.toMatchObject({
      status: 0,
      detail: expect.stringContaining('could not be reached'),
    });
  });
});
