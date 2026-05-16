import * as fs from 'fs';
import * as path from 'path';

/**
 * Global setup runs once before all tests, AFTER webServer is already running.
 * The DB cleanup must happen BEFORE Playwright starts the server (done in run-e2e.ps1
 * and via `npm run clean` in `npm test`). Do NOT invoke `npx playwright test` or
 * `playwright test` directly — it skips DB cleanup and causes test failures.
 * This function only ensures the tmp/ directory exists as a safety net.
 */
export default async function globalSetup(): Promise<void> {
  const fixturesPath = path.resolve(__dirname, 'fixtures', 'articles.json');
  if (!fs.existsSync(fixturesPath)) {
    throw new Error(
      'ERROR: e2e/fixtures/articles.json not found.\n' +
        'Ensure the file is present and committed to the repository.',
    );
  }

  const tmpDir = path.resolve(__dirname, 'tmp');
  if (!fs.existsSync(tmpDir)) {
    fs.mkdirSync(tmpDir, { recursive: true });
  }
}
