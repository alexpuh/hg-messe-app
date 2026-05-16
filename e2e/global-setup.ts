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
  const filesToDelete = [dbPath, dbPath + '-wal', dbPath + '-shm'];

  for (const file of filesToDelete) {
    if (!fs.existsSync(file)) continue;
    // Retry up to 5 times with a short delay — the file may be briefly locked
    // by a process from a previous interrupted run.
    let lastError: Error | null = null;
    for (let attempt = 0; attempt < 5; attempt++) {
      try {
        fs.unlinkSync(file);
        lastError = null;
        break;
      } catch (err: unknown) {
        lastError = err as Error;
        await new Promise((resolve) => setTimeout(resolve, 500));
      }
    }
    if (lastError) {
      throw new Error(
        `Cannot delete ${path.basename(file)} after 5 attempts — ` +
          `it may be held open by another process.\n${lastError.message}`,
      );
    }
  }
}
