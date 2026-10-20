import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { AppShell } from './app-shell';

@Component({ template: '' })
class Blank {}

describe('AppShell', () => {
  async function activeNavAt(url: string): Promise<string[]> {
    localStorage.setItem('claims-user', 'alex');
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          {
            path: '',
            component: AppShell,
            children: [{ path: '**', component: Blank }],
          },
        ]),
      ],
    });
    const harness = await RouterTestingHarness.create(url);
    return Array.from(
      (harness.routeNativeElement as HTMLElement).querySelectorAll(
        'nav a.active',
      ),
      (link) => link.textContent!.trim(),
    );
  }

  afterEach(() => localStorage.clear());

  it('marks only "New claim" active on the submission page', async () => {
    expect(await activeNavAt('/my-claims/new')).toEqual(['New claim']);
  });

  it('keeps "My claims" active on a claim details page', async () => {
    expect(await activeNavAt('/my-claims/CLM-DEMO-0014')).toEqual([
      'My claims',
    ]);
  });

  it('moves focus to the page content on navigation but not on query changes', async () => {
    localStorage.setItem('claims-user', 'sam');
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          {
            path: '',
            component: AppShell,
            children: [{ path: '**', component: Blank }],
          },
        ]),
      ],
    });
    const harness = await RouterTestingHarness.create('/queue');
    const main = (harness.routeNativeElement as HTMLElement).querySelector(
      'main',
    )!;

    await harness.navigateByUrl('/queue?status=Submitted');
    expect(document.activeElement).not.toBe(main);

    await harness.navigateByUrl('/queue/CLM-DEMO-0007');
    expect(document.activeElement).toBe(main);
  });

  it('stays on the current page when switching to another user of the same role', async () => {
    localStorage.setItem('claims-user', 'sam');
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          {
            path: '',
            component: AppShell,
            children: [
              { path: 'queue', component: Blank },
              { path: 'dashboard', component: Blank },
              { path: 'my-claims', component: Blank },
            ],
          },
        ]),
      ],
    });
    const fixture = TestBed.createComponent(AppShell);
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/queue');
    fixture.detectChanges();

    const shell = fixture.componentInstance as unknown as {
      switchUser: (id: string) => void;
    };
    shell.switchUser('aisha');
    await fixture.whenStable();
    expect(router.url).toBe('/queue');

    shell.switchUser('alex');
    await fixture.whenStable();
    expect(router.url).toBe('/my-claims');
  });
});
