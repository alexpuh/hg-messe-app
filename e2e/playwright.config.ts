import { defineConfig } from '@playwright/test';
import * as path from 'path';

const dbPath = path.resolve(__dirname, 'tmp', 'messeapp-e2e.db');

export default defineConfig({
  testDir: './tests',

  // Serial execution to avoid session-state conflicts (all tests share one DB and one
  // "current session" concept in the debug endpoint).
  workers: 1,
  fullyParallel: false,

  // No retries: tests are order-dependent and share session state.
  // Retrying an individual test in isolation would break AC-6 (which relies on the
  // session created by AC-4) and produce misleading failures.
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
      url: 'http://localhost:5227/api/BarcodeScanner/status',
      // Always start a dedicated test server. Reusing an existing dev server would point
      // at messeapp.db (the developer's primary database) instead of the isolated test DB.
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
