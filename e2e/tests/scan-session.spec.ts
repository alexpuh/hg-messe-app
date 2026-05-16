import { test, expect, request as playwrightRequest } from '@playwright/test';
import {
  uploadArticles,
  createDispatchSheet,
  setRequiredUnits,
  createSession,
  simulateScan,
} from '../helpers/api';
import {
  TEST_EAN_UNIT,
  TEST_UNIT_ID,
  TEST_REQUIRED_COUNT,
} from '../helpers/constants';

const DISPATCH_SHEET_NAME = `E2E Beladeliste Session ${Date.now()}`;

test.describe('Scan session screen (/scan-session)', () => {
  // Hoisted so AC-6 can create its own Beladung session without depending on AC-4.
  let dispatchSheetId: number;

  test.beforeAll(async () => {
    const apiCtx = await playwrightRequest.newContext();
    await uploadArticles(apiCtx);
    dispatchSheetId = await createDispatchSheet(apiCtx, DISPATCH_SHEET_NAME);
    await setRequiredUnits(apiCtx, dispatchSheetId, TEST_UNIT_ID, TEST_REQUIRED_COUNT);
    await apiCtx.dispose();
  });

  // AC-5: Start Beladung without selecting Beladeliste — Starten button must be disabled
  test('AC-5: Starten button is disabled when no Beladeliste is selected', async ({ page }) => {
    await page.goto('/scan-session');
    await expect(page.getByRole('button', { name: 'Neue Beladung starten' })).toBeVisible();

    await page.getByRole('button', { name: 'Neue Beladung starten' }).click();
    await expect(page.getByRole('dialog', { name: 'Neue Beladung starten' })).toBeVisible();

    // Starten button must be disabled — no dispatch sheet selected
    const startButton = page.getByRole('dialog').getByRole('button', { name: 'Starten' });
    await expect(startButton).toBeDisabled();

    await page.getByRole('dialog').getByRole('button', { name: 'Abbrechen' }).click();
  });

  // AC-4: Start Beladung with a Beladeliste — session starts and Soll column is visible
  test('AC-4: starts a Beladung session and shows the Soll column', async ({ page }) => {
    await page.goto('/scan-session');
    await expect(page.getByRole('button', { name: 'Neue Beladung starten' })).toBeVisible();

    await page.getByRole('button', { name: 'Neue Beladung starten' }).click();
    await expect(page.getByRole('dialog', { name: 'Neue Beladung starten' })).toBeVisible();

    // Select the dispatch sheet (p-select appends to body)
    await page.locator('#dispatchSheetSelect').click();
    await expect(page.getByRole('option', { name: DISPATCH_SHEET_NAME })).toBeVisible();
    await page.getByRole('option', { name: DISPATCH_SHEET_NAME }).click();

    const startButton = page.getByRole('dialog').getByRole('button', { name: 'Starten' });
    await expect(startButton).toBeEnabled();
    await startButton.click();

    // Dialog should close
    await expect(page.getByRole('dialog', { name: 'Neue Beladung starten' })).not.toBeVisible();

    // Session started — page shows dispatch sheet name and Soll indicators
    await expect(page.locator('h1')).toContainText('Beladung');

    // Required articles appear with a Soll count (req-exists or req-not-exists span)
    await expect(page.locator('.req-exists, .req-not-exists').first()).toBeVisible();
  });

  // AC-6: Scan a barcode via the Debug endpoint — article count increments
  test('AC-6: scans a barcode via the Debug endpoint and the article count increments to 1', async ({
    page,
    request,
  }) => {
    // Create a fresh Beladung session so this test is independent of AC-4's session state.
    // The new session becomes the most recently updated and is auto-selected by the UI.
    await createSession(request, 'ProcessDispatchList', 'Lager', dispatchSheetId);

    await page.goto('/scan-session');
    // Wait for the page heading to confirm the active session is loaded.
    await expect(page.locator('h1')).toBeVisible();
    // Verify we are in a Beladung session — guards against scanning into a different session.
    await expect(page.locator('h1')).toContainText('Beladung');

    await simulateScan(request, TEST_EAN_UNIT);

    // The article list should update via SignalR — article count becomes 1.
    // Scoped to the specific article row by EAN to avoid relying on DOM order.
    const scannedRow = page.locator('.stock-item', {
      has: page.locator('.artEan', { hasText: TEST_EAN_UNIT }),
    });
    await expect(scannedRow.locator('.ist-count')).toHaveText('1', { timeout: 10_000 });
  });

  // AC-7: Bestandsaufnahme Stand — no Soll column
  test('AC-7: starts a Bestandsaufnahme Stand session without a Soll column', async ({ page }) => {
    await page.goto('/scan-session');
    await expect(page.getByRole('button', { name: 'Bestandsaufnahme starten' })).toBeVisible();

    await page.getByRole('button', { name: 'Bestandsaufnahme starten' }).click();
    await expect(page.getByRole('dialog', { name: 'Bestandsaufnahme starten' })).toBeVisible();

    // Select "Stand" radio button
    await page.locator('label[for="ortStand"]').click();

    const startButton = page.getByRole('dialog').getByRole('button', { name: 'Starten' });
    await expect(startButton).toBeEnabled();
    await startButton.click();

    await expect(page.getByRole('dialog', { name: 'Bestandsaufnahme starten' })).not.toBeVisible();

    // scanMode shows "Messestand" (Inventory + Stand)
    await expect(page.locator('h1')).toContainText('Messestand');

    // No Soll column elements should be present
    await expect(page.locator('.req-exists')).toHaveCount(0);
    await expect(page.locator('.req-not-exists')).toHaveCount(0);
  });

  // AC-8: Bestandsaufnahme Lager — Soll column visible
  test('AC-8: starts a Bestandsaufnahme Lager session with Ist, Soll, and Fehlt indicators', async ({
    page,
  }) => {
    await page.goto('/scan-session');
    await expect(page.getByRole('button', { name: 'Bestandsaufnahme starten' })).toBeVisible();

    await page.getByRole('button', { name: 'Bestandsaufnahme starten' }).click();
    await expect(page.getByRole('dialog', { name: 'Bestandsaufnahme starten' })).toBeVisible();

    // Select "Lager" radio button
    await page.locator('label[for="ortLager"]').click();

    // Dispatch sheet selector appears for Lager
    await page.locator('#bestandsDispatchSheetSelect').click();
    await expect(page.getByRole('option', { name: DISPATCH_SHEET_NAME })).toBeVisible();
    await page.getByRole('option', { name: DISPATCH_SHEET_NAME }).click();

    const startButton = page.getByRole('dialog').getByRole('button', { name: 'Starten' });
    await expect(startButton).toBeEnabled();
    await startButton.click();

    await expect(page.getByRole('dialog', { name: 'Bestandsaufnahme starten' })).not.toBeVisible();

    // scanMode shows "Bestandsaufnahme Lager"
    await expect(page.locator('h1')).toContainText('Bestandsaufnahme Lager');

    // Soll column is visible (required articles from dispatch sheet appear)
    await expect(page.locator('.req-exists').first()).toBeVisible({ timeout: 10_000 });
  });
});
