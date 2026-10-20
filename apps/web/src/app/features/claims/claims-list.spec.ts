import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRoute,
  convertToParamMap,
  ParamMap,
  provideRouter,
  Router,
} from '@angular/router';
import { BehaviorSubject } from 'rxjs';

import { ClaimsList } from './claims-list';

describe('ClaimsList', () => {
  let queryParamMap: BehaviorSubject<ParamMap>;

  function setup(
    mode: 'claimant' | 'officer',
    query: Record<string, string> = {},
  ): void {
    queryParamMap = new BehaviorSubject(convertToParamMap(query));
    TestBed.configureTestingModule({
      imports: [ClaimsList],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { data: { mode }, queryParamMap: queryParamMap.value },
            queryParamMap,
          },
        },
      ],
    });
    TestBed.createComponent(ClaimsList);
  }

  async function nextRequest(): Promise<URL> {
    await new Promise((resolve) => setTimeout(resolve));
    const request = TestBed.inject(HttpTestingController).expectOne(
      (candidate) => candidate.url.startsWith('/api/v1/claims'),
    );
    request.flush({ items: [], page: 1, pageSize: 10, totalCount: 0 });
    return new URL(request.request.urlWithParams, 'http://localhost');
  }

  async function requestFor(
    mode: 'claimant' | 'officer',
    query: Record<string, string> = {},
  ): Promise<URL> {
    setup(mode, query);
    return nextRequest();
  }

  // The API rejects an empty assignedTo with 400, and claimants may not send it at all.
  it('omits the ownership filter for claimants', async () => {
    const url = await requestFor('claimant');
    expect(url.searchParams.has('assignedTo')).toBe(false);
    expect(url.searchParams.getAll('status')).toEqual([]);
  });

  it('ignores an ownership filter in a claimant URL', async () => {
    const url = await requestFor('claimant', { assignedTo: 'officer-1' });
    expect(url.searchParams.has('assignedTo')).toBe(false);
  });

  it('requests only open work, oldest first, for officers by default', async () => {
    const url = await requestFor('officer');
    expect(url.searchParams.has('assignedTo')).toBe(false);
    expect(url.searchParams.getAll('status')).toEqual([
      'Submitted',
      'UnderReview',
      'AwaitingInformation',
    ]);
    expect(url.searchParams.get('sort')).toBe('submittedAt');
    expect(url.searchParams.get('order')).toBe('asc');
  });

  it('lists recently updated claims newest first', async () => {
    const url = await requestFor('officer', { sort: 'updatedAt' });
    expect(url.searchParams.get('sort')).toBe('updatedAt');
    expect(url.searchParams.get('order')).toBe('desc');
  });

  // Dashboard totals link here with the filter in the URL, including closed statuses.
  it('applies a status filter from the URL, including closed claims', async () => {
    const url = await requestFor('officer', { status: 'Settled' });
    expect(url.searchParams.getAll('status')).toEqual(['Settled']);
  });

  it('requests settled and rejected claims for the closed-claims filter', async () => {
    const url = await requestFor('officer', { status: 'closed' });
    expect(url.searchParams.getAll('status')).toEqual(['Settled', 'Rejected']);
  });

  it('omits status when the officer asks for every claim', async () => {
    const url = await requestFor('officer', { status: 'all' });
    expect(url.searchParams.getAll('status')).toEqual([]);
  });

  it('reloads when the URL changes while the list is open', async () => {
    await requestFor('officer');
    queryParamMap.next(
      convertToParamMap({ assignedTo: 'officer-2', page: '2' }),
    );
    const url = await nextRequest();
    expect(url.searchParams.get('assignedTo')).toBe('officer-2');
    expect(url.searchParams.get('page')).toBe('2');
  });

  it("restores this officer's saved filters when the queue URL has none", () => {
    localStorage.setItem('claims-user', 'sam');
    localStorage.setItem(
      'claims-queue-filters:sam',
      JSON.stringify({ status: 'closed', type: 'Motor' }),
    );
    const navigate = vi.fn().mockResolvedValue(true);
    queryParamMap = new BehaviorSubject(convertToParamMap({}));
    TestBed.configureTestingModule({
      imports: [ClaimsList],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: { navigate } },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              data: { mode: 'officer' },
              queryParamMap: queryParamMap.value,
            },
            queryParamMap,
          },
        },
      ],
    });
    TestBed.createComponent(ClaimsList);
    expect(navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: { status: 'closed', type: 'Motor' },
      }),
    );
  });

  afterEach(() => localStorage.clear());
});
