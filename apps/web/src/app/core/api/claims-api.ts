import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import {
  cancelInformationRequest,
  ClaimDetailsResponse,
  ClaimHistoryEntryResponse,
  ClaimPageResponse,
  DashboardResponse,
  getClaimDetails,
  getClaimHistory,
  getDashboard,
  HttpValidationProblemDetails,
  listClaims,
  ListClaimsData,
  listCurrencies,
  ProblemDetails,
  provideInformation,
  rejectClaim,
  requestInformation,
  settleClaim,
  submitClaim,
  SubmitClaimRequest,
  takeClaim,
  updateEstimatedLiability,
} from './generated';
import { client } from './generated/client.gen';

/** Problem details plus the claim's current ETag, which the API sends with a 409 so the page can retry against the latest version. */
export type ApiProblem = Partial<
  ProblemDetails & HttpValidationProblemDetails
> & {
  etag?: string;
};

const NETWORK_PROBLEM: ApiProblem = {
  status: 0,
  title: 'Service unreachable',
  detail:
    'The claims service could not be reached. Check your connection and try again.',
};

interface ApiResult<T> {
  data?: T;
  error?: unknown;
  response?: {
    headers: { get(name: string): string | null };
    status: number;
  };
}

@Injectable({ providedIn: 'root' })
export class ClaimsApi {
  constructor() {
    client.setConfig({ baseUrl: '', httpClient: inject(HttpClient) });
  }

  async list(query: ListClaimsData['query']): Promise<ClaimPageResponse> {
    return this.data(await listClaims({ query }));
  }

  async get(
    reference: string,
  ): Promise<{ claim: ClaimDetailsResponse; etag: string }> {
    return this.claimResult(await getClaimDetails({ path: { reference } }));
  }

  async history(reference: string): Promise<ClaimHistoryEntryResponse[]> {
    return this.data(await getClaimHistory({ path: { reference } }));
  }

  async dashboard(): Promise<DashboardResponse> {
    return this.data(await getDashboard());
  }

  async currencies(): Promise<string[]> {
    return this.data(await listCurrencies());
  }

  async submit(
    body: SubmitClaimRequest,
  ): Promise<{ claim: ClaimDetailsResponse; etag: string }> {
    return this.claimResult(await submitClaim({ body }));
  }

  async take(
    reference: string,
  ): Promise<{ claim: ClaimDetailsResponse; etag: string }> {
    return this.claimResult(await takeClaim({ path: { reference } }));
  }

  async updateEstimate(
    reference: string,
    amount: string,
    etag: string,
  ): Promise<{ claim: ClaimDetailsResponse; etag: string }> {
    return this.claimResult(
      await updateEstimatedLiability({
        body: { amount },
        headers: { 'If-Match': etag },
        path: { reference },
      }),
    );
  }

  async requestInformation(
    reference: string,
    requestText: string,
    etag: string,
  ): Promise<{ claim: ClaimDetailsResponse; etag: string }> {
    return this.claimResult(
      await requestInformation({
        body: { requestText },
        headers: { 'If-Match': etag },
        path: { reference },
      }),
    );
  }

  async respond(
    reference: string,
    requestId: string,
    responseText: string,
    etag: string,
  ): Promise<{ claim: ClaimDetailsResponse; etag: string }> {
    return this.claimResult(
      await provideInformation({
        body: { responseText },
        headers: { 'If-Match': etag },
        path: { reference, requestId },
      }),
    );
  }

  async cancelRequest(
    reference: string,
    requestId: string,
    cancellationReason: string,
    etag: string,
  ): Promise<{ claim: ClaimDetailsResponse; etag: string }> {
    return this.claimResult(
      await cancelInformationRequest({
        body: { cancellationReason },
        headers: { 'If-Match': etag },
        path: { reference, requestId },
      }),
    );
  }

  async settle(
    reference: string,
    settlementAmount: string,
    rationale: string,
    etag: string,
  ): Promise<{ claim: ClaimDetailsResponse; etag: string }> {
    return this.claimResult(
      await settleClaim({
        body: { rationale, settlementAmount },
        headers: { 'If-Match': etag },
        path: { reference },
      }),
    );
  }

  async reject(
    reference: string,
    rationale: string,
    etag: string,
  ): Promise<{ claim: ClaimDetailsResponse; etag: string }> {
    return this.claimResult(
      await rejectClaim({
        body: { rationale },
        headers: { 'If-Match': etag },
        path: { reference },
      }),
    );
  }

  /** The user-facing message for a failed request: the API's detail, or a generic fallback. */
  problemMessage(error: unknown, fallback = 'Something went wrong.'): string {
    return this.problem(error).detail ?? fallback;
  }

  problem(error: unknown): ApiProblem {
    // Network failures are turned into NETWORK_PROBLEM in data(), so anything else here is the API's problem body.
    if (error && typeof error === 'object') return error as ApiProblem;
    return { ...NETWORK_PROBLEM };
  }

  private claimResult(result: ApiResult<ClaimDetailsResponse>): {
    claim: ClaimDetailsResponse;
    etag: string;
  } {
    return {
      claim: this.data(result),
      // Without an ETag, If-Match is empty and matches no version, so a later update fails closed instead of skipping the concurrency check.
      etag: result.response?.headers.get('ETag') ?? '',
    };
  }

  private data<T>(result: ApiResult<T>): T {
    if (result.error !== undefined || result.data === undefined) {
      // Angular reports a request that never reached the server as status 0.
      if (!result.response || result.response.status === 0)
        throw { ...NETWORK_PROBLEM };
      const problem = this.problem(result.error);
      problem.status ??= result.response.status;
      problem.etag = result.response.headers.get('ETag') ?? undefined;
      throw problem;
    }
    return result.data;
  }
}
