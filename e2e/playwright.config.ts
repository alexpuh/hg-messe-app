import { defineConfig } from '@playwright/test';
import * as path from 'path';

const dbPath = path.resolve(__dirname, 'tmp', 'messeapp-e2e.db');

export default defineConfig({
  testDir: './tests',

  // Serial execution to avoid session-state conflicts (all tests share one DB and one
  // "current session" concept in the debug endpoint).
  workers: 1,
  fullyParallel: false,

  // Retry failed tests once in CI to reduce flakiness from startup timing.
  retries: process.env.CI ? 1 : 0,

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
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ConnectionStrings__DefaultConnection: `DataSource=${dbPath}`,
      },
    },
    {
      command: 'npm start --prefix ../client',
      url: 'http://localhost:4200',
      reuseExistingServer: !process.env.CI,
      timeout: 180_000,
    },
  ],
});
