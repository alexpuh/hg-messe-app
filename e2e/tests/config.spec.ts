import { test, expect, request as playwrightRequest } from '@playwright/test';
import {
  uploadArticles,
  createDispatchSheet,
  setRequiredUnits,
} from '../helpers/api';
import { TEST_UNIT_ID, TEST_REQUIRED_COUNT } from '../helpers/constants';

const DISPATCH_SHEET_NAME = `E2E Beladeliste Config ${Date.now()}`;

test.describe('Config screen (/config)', () => {
  test.beforeAll(async () => {
    const apiCtx = await playwrightRequest.newContext();
    await uploadArticles(apiCtx);
    const dispatchSheetId = await createDispatchSheet(apiCtx, DISPATCH_SHEET_NAME);
    await setRequiredUnits(apiCtx, dispatchSheetId, TEST_UNIT_ID, TEST_REQUIRED_COUNT);
    await apiCtx.dispose();
  });

  // AC-2: Create a new Beladeliste
  test('AC-2: creates a new Beladeliste and shows it in the dropdown', async ({ page }) => {
    await page.goto('/config');
    // Verify the pre-created dispatch sheet is selectable — confirms the component has
    // finished loading its data before we interact with anything else.
    await page.locator('p-select').first().click();
    await expect(page.getByRole('option', { name: DISPATCH_SHEET_NAME })).toBeVisible({
      timeout: 10_000,
    });
    await page.keyboard.press('Escape');

    const newName = `Neue Beladeliste ${Date.now()}`;

    await page.getByRole('button', { name: 'Neue Beladeliste' }).click();

    // Dialog should be visible
    await expect(page.getByRole('dialog', { name: 'Neue Beladeliste erstellen' })).toBeVisible();

    await page.locator('#newTradeEventName').fill(newName);
    await page
      .getByRole('dialog', { name: 'Neue Beladeliste erstellen' })
      .getByRole('button', { name: 'Erstellen' })
      .click();

    // Dialog should close
    await expect(page.getByRole('dialog', { name: 'Neue Beladeliste erstellen' })).not.toBeVisible();

    // The new dispatch sheet should be selectable in the dropdown
    await page.locator('p-select').first().click();
    await expect(page.getByRole('option', { name: newName })).toBeVisible();
    await page.keyboard.press('Escape');
  });

  // AC-3: Set Sollbestand for an article
  test('AC-3: sets a Sollbestand for an article and displays the updated count', async ({ page }) => {
    await page.goto('/config');
    await expect(page.locator('p-select').first()).toBeVisible();
    await page.locator('p-select').first().click();
    await page.getByRole('option', { name: DISPATCH_SHEET_NAME }).click();

    // Wait for article table to load
    await expect(page.locator('p-table')).toBeVisible();

    // Find the Sollbestand edit button (size="large") — shows "+" or existing count.
    // The delete button (danger, icon-only) is also present when a count exists, so we must be specific.
    const sollCell = page.locator('p-table tbody tr').first().locator('p-button[size="large"]');
    await sollCell.click();

    // Editing input should appear
    const input = page.locator('input[type="number"]').first();
    await expect(input).toBeVisible();
    await input.fill('10');

    // Confirm save (check icon button)
    await page.locator('p-button[severity="success"]').first().click();

    // The updated count should now appear in the Sollbestand cell (scoped to the edit button)
    await expect(page.locator('p-table tbody tr').first().locator('p-button[size="large"]')).toHaveText('10');
  });
});
