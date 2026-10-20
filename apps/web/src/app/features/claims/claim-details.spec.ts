import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  ActivatedRoute,
  convertToParamMap,
  ParamMap,
  provideRouter,
} from '@angular/router';
import { BehaviorSubject } from 'rxjs';

import { ClaimDetailsResponse } from '../../core/api/generated';
import { ClaimDetails } from './claim-details';

const SAM = {
  id: '00000000-0000-0000-0000-000000000101',
  displayName: 'Sam Lee',
};

function claim(
  reference: string,
  overrides: Partial<ClaimDetailsResponse> = {},
): ClaimDetailsResponse {
  return {
    reference,
    type: 'Motor',
    status: 'UnderReview',
    policyNumber: 'POL-1',
    claimant: { fullName: 'Alex Tan', email: 'alex@example.com', phone: null },
    incident: {
      occurredAt: '2026-10-01T08:00:00Z',
      location: 'Orchard Road',
      description: 'Rear-ended at a junction.',
    },
    motor: { vehicleRegistration: 'SBA1234A' },
    property: null,
    assignedOfficer: SAM,
    currency: 'SGD',
    estimatedLiability: null,
    informationRequests: [],
    decision: null,
    submittedAt: '2026-10-01T09:00:00Z',
    updatedAt: '2026-10-01T09:00:00Z',
    ...overrides,
  };
}

describe('ClaimDetails', () => {
  let fixture: ComponentFixture<ClaimDetails>;
  let http: HttpTestingController;
  let paramMap: BehaviorSubject<ParamMap>;

  const settle = () => new Promise((resolve) => setTimeout(resolve));
  const page = () => fixture.nativeElement as HTMLElement;
  const button = (label: string) =>
    Array.from(page().querySelectorAll('button')).find((candidate) =>
      candidate.textContent?.includes(label),
    )!;
  const textarea = (label: string) =>
    Array.from(page().querySelectorAll('mat-form-field'))
      .find((field) => field.textContent?.includes(label))!
      .querySelector('textarea')!;

  async function load(reference: string): Promise<void> {
    await settle();
    http
      .expectOne(`/api/v1/claims/${reference}`)
      .flush(claim(reference), { headers: { ETag: '"1"' } });
    http.expectOne(`/api/v1/claims/${reference}/history`).flush([]);
    await settle();
    await fixture.whenStable();
  }

  async function requestInformation(text: string): Promise<void> {
    const field = textarea('Information needed');
    field.value = text;
    field.dispatchEvent(new Event('input'));
    button('Request information').click();
    await settle();
  }

  beforeEach(async () => {
    // Acts as the officer who owns the claim, so the actions are available.
    localStorage.setItem('claims-user', 'sam');
    paramMap = new BehaviorSubject(
      convertToParamMap({ reference: 'CLM-DEMO-0001' }),
    );
    TestBed.configureTestingModule({
      imports: [ClaimDetails],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { data: { mode: 'officer' } }, paramMap },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ClaimDetails);
    await load('CLM-DEMO-0001');
  });

  afterEach(() => {
    http.verify();
    localStorage.removeItem('claims-user');
  });

  it('does not send whitespace-only text', async () => {
    await requestInformation('   ');
    http.expectNone('/api/v1/claims/CLM-DEMO-0001/information-requests');
    await fixture.whenStable();
    expect(page().textContent).toContain('Enter the information you need.');
  });

  it('sends the current ETag and clears the text after a successful action', async () => {
    await requestInformation('Send photos of the damage.');
    const request = http.expectOne(
      '/api/v1/claims/CLM-DEMO-0001/information-requests',
    );
    expect(request.request.headers.get('If-Match')).toBe('"1"');
    expect(request.request.body).toEqual({
      requestText: 'Send photos of the damage.',
    });
    request.flush(claim('CLM-DEMO-0001'), { headers: { ETag: '"2"' } });
    await settle();
    http.expectOne('/api/v1/claims/CLM-DEMO-0001/history').flush([]);
    await settle();
    await fixture.whenStable();
    expect(textarea('Information needed').value).toBe('');
  });

  // The action was saved, so a failed history refresh must not report it as failed.
  it('keeps the success message when the history refresh fails', async () => {
    await requestInformation('Send photos of the damage.');
    http
      .expectOne('/api/v1/claims/CLM-DEMO-0001/information-requests')
      .flush(claim('CLM-DEMO-0001'), { headers: { ETag: '"2"' } });
    await settle();
    http
      .expectOne('/api/v1/claims/CLM-DEMO-0001/history')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await settle();
    await fixture.whenStable();
    expect(page().textContent).toContain(
      'Information request sent to the claimant.',
    );
    expect(page().querySelector('app-alert[tone="danger"]')).toBeNull();
  });

  it('reloads when the route moves to another claim', async () => {
    paramMap.next(convertToParamMap({ reference: 'CLM-DEMO-0002' }));
    await load('CLM-DEMO-0002');
    expect(page().textContent).toContain('CLM-DEMO-0002');
  });
});
