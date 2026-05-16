import * as fs from 'fs';
import * as path from 'path';

/**
 * Global setup runs once before all tests, AFTER webServer is already running.
 * The DB cleanup must happen BEFORE Playwright starts the server (done in run-e2e.ps1).
 * This function only ensures the tmp/ directory exists as a safety net.
 */
export default async function globalSetup(): Promise<void> {
  const fixturesPath = path.resolve(__dirname, 'fixtures', 'articles.json');
  if (!fs.existsSync(fixturesPath)) {
    throw new Error(
      'ERROR: e2e/fixtures/articles.json not found.\n' +
        'Create it from docs/tasks/articles.json before running E2E tests.',
    );
  }

  const tmpDir = path.resolve(__dirname, 'tmp');
  if (!fs.existsSync(tmpDir)) {
    fs.mkdirSync(tmpDir, { recursive: true });
  }
}
