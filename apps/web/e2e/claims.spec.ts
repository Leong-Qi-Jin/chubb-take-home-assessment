import { expect, Page, test } from '@playwright/test';

const users = {
  claimant: 'alex',
  officer: 'sam',
} as const;

async function openAs(page: Page, user: string, path: string): Promise<void> {
  await page.goto('/');
  await page.evaluate(
    ([key, value]) => localStorage.setItem(key, value),
    ['claims-user', user],
  );
  await page.goto(path);
}

async function submitClaim(
  page: Page,
  type: 'Motor' | 'Property',
): Promise<string> {
  await openAs(page, users.claimant, '/my-claims/new');
  await expect(page.getByRole('heading', { name: 'New claim' })).toBeVisible();

  const unique = `${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
  await page.getByRole('radio', { name: new RegExp(`^${type}`) }).check();
  await page.getByLabel('Policy number').fill(`E2E-${unique}`);

  const yesterday = new Date();
  yesterday.setDate(yesterday.getDate() - 1);
  const date = [
    String(yesterday.getDate()).padStart(2, '0'),
    String(yesterday.getMonth() + 1).padStart(2, '0'),
    yesterday.getFullYear(),
  ].join('/');
  await page.getByLabel('Incident date').fill(date);
  await page.getByLabel('Incident time').fill('10:30 AM');
  await page.getByLabel('Incident location').fill('E2E test location');
  await page
    .getByLabel('Incident description')
    .fill(`Full-stack ${type.toLowerCase()} journey ${unique}`);

  if (type === 'Motor') {
    await page
      .getByLabel('Vehicle registration')
      .fill(`E2E${unique.slice(-6)}`);
  } else {
    await page
      .getByLabel('Property address')
      .fill(`1 Integration Test Road, ${unique}`);
  }

  const submission = page.waitForResponse(
    (response) =>
      response.request().method() === 'POST' &&
      new URL(response.url()).pathname === '/api/v1/claims',
  );
  await page.getByRole('button', { name: 'Submit claim' }).click();
  const response = await submission;
  expect(response.status()).toBe(201);

  const body = (await response.json()) as { reference: string };
  await expect(page).toHaveURL(new RegExp(`/my-claims/${body.reference}$`));
  await expect(page.getByRole('status')).toContainText('Claim submitted.');
  return body.reference;
}

async function takeClaim(page: Page, reference: string): Promise<void> {
  await openAs(page, users.officer, `/queue/${reference}`);
  await page.getByRole('button', { name: 'Take this claim' }).click();
  await expect(page.getByText('Claim assigned to you.')).toBeVisible();
}

test.describe('critical claim journeys', () => {
  test('claimant submits a claim and an officer settles it', async ({
    page,
  }) => {
    const reference = await submitClaim(page, 'Motor');
    await takeClaim(page, reference);

    await page.getByLabel('Estimated amount').fill('2500.00');
    await page.getByRole('button', { name: 'Update estimate' }).click();
    await expect(page.getByText('Estimated liability updated.')).toBeVisible();

    await page
      .getByLabel('Decision rationale')
      .fill('Repairs are covered under the policy.');
    await page.getByLabel('Settlement amount').fill('2250.00');
    await page.getByRole('button', { name: 'Settle claim' }).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog.getByRole('heading')).toHaveText('Settle this claim?');
    await dialog.getByRole('button', { name: 'Settle claim' }).click();

    await expect(
      page.getByRole('heading', { name: 'Claim settled' }),
    ).toBeVisible();
    await expect(page.getByText('SGD 2,250.00', { exact: true })).toBeVisible();

    await openAs(page, users.claimant, `/my-claims/${reference}`);
    await expect(
      page.getByRole('heading', { name: 'Claim settled' }),
    ).toBeVisible();
    await expect(
      page.getByText('Repairs are covered under the policy.'),
    ).toBeVisible();
  });

  test('officer requests information, claimant responds, and officer rejects', async ({
    page,
  }) => {
    const reference = await submitClaim(page, 'Property');
    await takeClaim(page, reference);

    const request = 'Please confirm when the damaged window was secured.';
    await page.getByLabel('Information needed').fill(request);
    await page.getByRole('button', { name: 'Request information' }).click();
    await expect(
      page.getByText('Information request sent to the claimant.'),
    ).toBeVisible();

    await openAs(page, users.claimant, `/my-claims/${reference}`);
    await expect(page.getByText(request)).toBeVisible();
    const response = 'The window was secured at 6:00 PM that evening.';
    await page.getByLabel('Your response').fill(response);
    await page.getByRole('button', { name: 'Send response' }).click();
    await expect(page.getByText('Response sent.')).toBeVisible();

    await openAs(page, users.officer, `/queue/${reference}`);
    await expect(page.getByText(response)).toBeVisible();
    const rationale = 'The reported damage is excluded by the policy.';
    await page.getByLabel('Decision rationale').fill(rationale);
    await page.getByRole('button', { name: 'Reject claim' }).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog.getByRole('heading')).toHaveText('Reject this claim?');
    await dialog.getByRole('button', { name: 'Reject claim' }).click();

    await expect(
      page.getByRole('heading', { name: 'Claim rejected' }),
    ).toBeVisible();
    await openAs(page, users.claimant, `/my-claims/${reference}`);
    await expect(
      page.getByRole('heading', { name: 'Claim rejected' }),
    ).toBeVisible();
    await expect(page.getByText(rationale)).toBeVisible();
  });
});
