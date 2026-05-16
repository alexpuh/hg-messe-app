import * as fs from 'fs';
import * as path from 'path';

/**
 * Global setup runs once before all tests.
 * - Validates that the articles fixture exists.
 * - Ensures the tmp/ directory exists and removes any leftover DB from a previous run.
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

  const dbPath = path.resolve(tmpDir, 'messeapp-e2e.db');
  if (fs.existsSync(dbPath)) {
    fs.unlinkSync(dbPath);
  }

  // Also clean up SQLite WAL/SHM files if present
  for (const suffix of ['-wal', '-shm']) {
    const sidecar = dbPath + suffix;
    if (fs.existsSync(sidecar)) {
      fs.unlinkSync(sidecar);
    }
  }
}
