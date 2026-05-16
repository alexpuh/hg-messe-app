import { defineConfig } from '@playwright/test';
import * as path from 'path';

const dbPath = path.resolve(__dirname, 'tmp', 'messeapp-e2e.db');

export default defineConfig({
  testDir: './tests',

  // Serial execution to avoid session-state conflicts (all tests share one DB and one
  // "current session" concept in the debug endpoint).
  workers: 1,
  fullyParallel: false,

  // No retries: even though AC-6 is now independent, AC-4/AC-7/AC-8 still create sessions
  // via the UI that alter the "current session" state. Retrying those tests would leave
  // an extra session in the DB, making subsequent tests unpredictable.
  // Re-evaluate once all tests fully manage their own session lifecycle.
  retries: 0,

  reporter: process.env.CI ? 'github' : 'list',

  use: {
    baseURL: 'http://localhost:4200',
    browserName: 'chromium',
    headless: true,
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
    // Give Angular + SignalR time to settle after navigation
    actionTimeout: 10_000,
  },

  globalSetup: './global-setup.ts',

  webServer: [
    {
      command: 'dotnet run --project ../server/messe-server/messe-server.csproj',
      // BarcodeScannerService.IsConnected() returns `serialPort is { IsOpen: true }` — a
      // pure in-memory check that never touches the serial port. Safe in CI without a scanner.
      url: 'http://localhost:5227/api/BarcodeScanner/status',
      // Always start a dedicated test server. Reusing an existing dev server would point
      // at messeapp.db (the developer's primary database) instead of the isolated test DB.
      // If port 5227 is already in use by a dev server, Playwright will fail fast with a
      // clear error — stop the dev server before running E2E tests.
      reuseExistingServer: false,
      timeout: 120_000,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ConnectionStrings__DefaultConnection: `Data Source=${dbPath}`,
        // Prevent the BarcodeScannerBackgroundService from trying to open a COM port
        // (which doesn't exist in CI or on dev machines without the physical scanner).
        BarcodeScanner__DisableBackgroundService: 'true',
      },
    },
    {
      command: 'npm start --prefix ../client',
      url: 'http://localhost:4200',
      // Allow reuse of an already-running dev client — no DB isolation concern here.
      reuseExistingServer: true,
      timeout: 180_000,
    },
  ],
});
