import { test, expect, request as playwrightRequest } from '@playwright/test';
import {
  uploadArticles,
  createDispatchSheet,
  setRequiredUnits,
  createSession,
} from '../helpers/api';
import { TEST_UNIT_ID, TEST_REQUIRED_COUNT } from '../helpers/constants';

const DISPATCH_SHEET_NAME = `E2E Beladeliste Combined ${Date.now()}`;

test.describe('Combined view (/combined-view)', () => {
  test.beforeAll(async () => {
    const apiCtx = await playwrightRequest.newContext();

    await uploadArticles(apiCtx);

    const dsId = await createDispatchSheet(apiCtx, DISPATCH_SHEET_NAME);
    await setRequiredUnits(apiCtx, dsId, TEST_UNIT_ID, TEST_REQUIRED_COUNT);

    // Create a Stand session and a Lager session so the combined view has data to display.
    // The Lager session is associated with the dispatch sheet so required articles appear
    // in the combined table even without scans.
    await createSession(apiCtx, 'Inventory', 'Stand');
    await createSession(apiCtx, 'Inventory', 'Lager', dsId);

    await apiCtx.dispose();
  });

  // AC-9: Display Messeabschluss combined table
  test('AC-9: displays the combined Messeabschluss table with all expected columns', async ({
    page,
  }) => {
    await page.goto('/combined-view');

    // Wait for both selects to be interactive before interacting with them.
    await expect(page.locator('#lagerSessionSelect')).toBeVisible();
    await expect(page.locator('#standSessionSelect')).toBeVisible();

    // Explicitly select a Lager session — always the first (most recently created) option.
    // Because the DB is wiped before every run and beforeAll creates these sessions,
    // they are guaranteed to be the only options at this point.
    await page.locator('#lagerSessionSelect').click();
    await page.getByRole('option').first().click();

    // Explicitly select a Stand session.
    await page.locator('#standSessionSelect').click();
    await page.getByRole('option').first().click();

    // "Anzeigen" button should be enabled once both sessions are selected
    const anzeigenButton = page.getByRole('button', { name: 'Anzeigen' });
    await expect(anzeigenButton).toBeEnabled({ timeout: 10_000 });

    await anzeigenButton.click();

    // Combined table should appear with the expected column headers
    const table = page.locator('table.combined-table');
    await expect(table).toBeVisible({ timeout: 10_000 });

    await expect(table.locator('th', { hasText: 'Stand Ist' })).toBeVisible();
    await expect(table.locator('th', { hasText: 'Lager Ist' })).toBeVisible();
    await expect(table.locator('th', { hasText: 'Gesamt' })).toBeVisible();
    await expect(table.locator('th', { hasText: 'Soll' })).toBeVisible();
    await expect(table.locator('th', { hasText: 'Fehlt' })).toBeVisible();

    // At least one data row should be present (from dispatch sheet required units)
    await expect(table.locator('tbody tr').first()).toBeVisible();
  });
});
